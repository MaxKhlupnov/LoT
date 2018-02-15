
namespace HomeOS.Hub.Platform
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using HomeOS.Hub.Common;
    using System.Xml;
    using Ionic.Zip;
    using HomeOS.Hub.Platform.Views;
    using HomeOS.Hub.Platform.DeviceScout;

    public class Configuration
    {
        //private Dictionary<string, Role> roleDb = new Dictionary<string, Role>();
        //public Dictionary<string, HomeStoreApp> moduleDb = new Dictionary<string,HomeStoreApp>();
        //private Dictionary<string, HomeStoreDevice> deviceDb = new Dictionary<string, HomeStoreDevice>();

        public Dictionary<string, ModuleInfo> allModules = new Dictionary<string, ModuleInfo>();

        private int nextUserOrGroupId = 1;  //0 is reserved for user system
        private Dictionary<string, UserInfo> allUsers = new Dictionary<string, UserInfo>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, UserGroupInfo> allGroups = new Dictionary<string, UserGroupInfo>(StringComparer.OrdinalIgnoreCase); //includes all users
        private UserGroupInfo rootGroup;

        private int nextLocId = 0;
        private Dictionary<string, Location> allLocations = new Dictionary<string, Location>();
        public Location RootLocation { get; private set; }

        //configured ports
        private Dictionary<VPortInfo, PortInfo> configuredPorts = new Dictionary<VPortInfo, PortInfo>();
        private Dictionary<string, VPortInfo> configuredPortNames = new Dictionary<string, VPortInfo>();
        private Dictionary<VRole,bool> configuredRolesInHome = new Dictionary<VRole,bool>();

        //unconfigured Ports
        private Dictionary<VPortInfo, PortInfo> unconfiguredPorts = new Dictionary<VPortInfo, PortInfo>();

        private List<AccessRule> allPolicies = new List<AccessRule>();

        private Dictionary<string, Device> allDevices = new Dictionary<string, Device>();

        //this is where we store information on all scouts
        private Dictionary<string, ScoutInfo> allScouts = new Dictionary<string, ScoutInfo>();

        VLogger logger;

        XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();

        string directory;

        string LocationsFile { get { return directory + "\\" + Constants.LocationsFileName; } }
        string DevicesFile { get { return directory + "\\" + Constants.DevicesFileName; } }
        string ModulesFile { get { return directory + "\\" + Constants.ModulesFileName; } }
        string ServicesFile { get { return directory + "\\" + Constants.ServicesFileName; } }
        string UsersFile { get { return directory + "\\" + Constants.UsersFileName; } }
        string RulesFile { get { return directory + "\\" + Constants.RulesFileName; } }
        string ScoutsFile { get { return directory + "\\" + Constants.ScoutsFileName; } }
        string SettingsFile { get { return directory + "\\" + Constants.SettingsFileName; } }
        string PrivateSettingsFile { get { return directory + "\\" + Constants.PrivateSettingsFileName; } }

        public Configuration(string directory)
        {
            this.directory = directory;
        }

        public void ParseSettings()
        {
            xmlReaderSettings.IgnoreComments = true;
            ReadSettings();
        }

        public void SetLogger(VLogger logger)
        {
            this.logger = logger;
        }

        public void ReadConfiguration()
        {
            xmlReaderSettings.IgnoreComments = true;

            ReadPrivateSettings();
            ReadLocationTree();
            ReadModuleList();
            ReadServicesList();
            ReadUserTree(); 
            ReadAccessRules();
            ReadDeviceList();
            ReadScoutsList();
        }

        //we use this method to save xmlDoc to minimize the chances that bad configs will be left on disk
        private void SaferSave(XmlDocument xmlDoc, string fileName)
        {
            string tmpFile = fileName + ".tmp";

            xmlDoc.Save(tmpFile);

            if (System.IO.File.Exists(fileName))
                System.IO.File.Delete(fileName);

            System.IO.File.Move(tmpFile, fileName);
        }


        #region read and write device list
        private void ReadDeviceList()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(DevicesFile, xmlReaderSettings);
            DateTime lastSeenDateTime;

            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Devices"))
                throw new Exception(DevicesFile + " doesn;t start with Devices");

            foreach (XmlElement xmlDevice in root.ChildNodes)
            {
                if (!xmlDevice.Name.Equals("Device"))
                    throw new Exception("child is not a Device in " + DevicesFile);

                string friendlyName = xmlDevice.GetAttribute("FriendlyName");
                string uniqueName = xmlDevice.GetAttribute("UniqueName");
                string ipAddress = xmlDevice.GetAttribute("IPAddress");
                string lastSeenStr = xmlDevice.GetAttribute("LastSeen");
                string driverBinaryName = xmlDevice.GetAttribute("DriverBinaryName");

                //read the devicedetails now
                string configuredStr = xmlDevice.GetAttribute("Configured");
                string driverModuleFriendlyName = xmlDevice.GetAttribute("DriverModuleFriendlyName");

                //read the deviceDriverParams
                List<string> deviceDriverParams = new List<string>();

                XmlElement xmlDriverParams = (XmlElement) xmlDevice.ChildNodes.Item(0);

                if (!xmlDriverParams.Name.Equals("DriverParams"))
                    throw new Exception("child of Device is not DriverParams. it is " + xmlDriverParams.Name);

                int count = int.Parse(xmlDriverParams.GetAttribute("Count"));

                for (int index=0; index < count; index++) 
                {
                    deviceDriverParams.Add(xmlDriverParams.GetAttribute("Param"+index));
                }

                //sanity check and construct the device object

                if (!DateTime.TryParse(lastSeenStr, out lastSeenDateTime))
                {
                    // TODO: what should be the last seen time if there isn't one saved in the Devices configuration
                }

                Device device = new Device(friendlyName, uniqueName, ipAddress, lastSeenDateTime, driverBinaryName);

                bool configured = false;
                if (bool.TryParse(configuredStr, out configured))
                {
                    device.Details.Configured = configured;
                }

                device.Details.DriverFriendlyName = driverModuleFriendlyName;
                device.Details.DriverParams = deviceDriverParams;

                allDevices.Add(uniqueName, device);
            }

            xmlReader.Close();
        }

        //we assume that allDevices is locked when this function is called
        public void WriteDeviceList()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement root = xmlDoc.CreateElement("Devices");
            xmlDoc.AppendChild(root);

            foreach (Device device in allDevices.Values)
            {
                XmlElement xmlDevice = xmlDoc.CreateElement("Device");

                xmlDevice.SetAttribute("FriendlyName", device.FriendlyName);
                xmlDevice.SetAttribute("UniqueName", device.UniqueName);
                xmlDevice.SetAttribute("IPAddress", device.DeviceIpAddress);
                //xmlDevice.SetAttribute("LastSeen", device.LastSeen.ToString()); 
                // commenting out, because it should not be written to disk.  
                // otherwise, config version(hash) keeps changing every minute; updates fail 
                //-ray
                xmlDevice.SetAttribute("DriverBinaryName", device.DriverBinaryName);

                //write the device details now
                xmlDevice.SetAttribute("Configured", device.Details.Configured.ToString());
                xmlDevice.SetAttribute("DriverModuleFriendlyName", device.Details.DriverFriendlyName);

                XmlElement driverParams = xmlDoc.CreateElement("DriverParams");
                
                driverParams.SetAttribute("Count", device.Details.DriverParams.Count.ToString());

                for (int index = 0; index < device.Details.DriverParams.Count; index++)
                {
                    driverParams.SetAttribute("Param" + index, device.Details.DriverParams[index]);
                }
                xmlDevice.AppendChild(driverParams);

                root.AppendChild(xmlDevice);
            }

            SaferSave(xmlDoc, DevicesFile);
        }
        #endregion

        #region read and write scout list
        private void ReadScoutsList()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(ScoutsFile, xmlReaderSettings);

            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Scouts"))
                throw new Exception(ScoutsFile + " doesn't start with Scouts");

            foreach (XmlElement xmlDevice in root.ChildNodes)
            {
                if (!xmlDevice.Name.Equals("Scout"))
                    throw new Exception("child is not a Scout in " + ScoutsFile);

                string scoutName = xmlDevice.GetAttribute("Name");
                string driverBinaryName = xmlDevice.GetAttribute("DllName");
                string version = xmlDevice.GetAttribute("Version");
                driverBinaryName = driverBinaryName.Replace(".dll", "");
                

                ScoutInfo sInfo = new ScoutInfo(scoutName, driverBinaryName);

                if (!String.IsNullOrEmpty(version))
                    sInfo.SetDesiredVersion(version);
                else
                    sInfo.SetDesiredVersion(Constants.UnknownHomeOSUpdateVersionValue);

                allScouts.Add(scoutName, sInfo);
            }

            xmlReader.Close();
        }

        public void WriteScoutsList()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement root = xmlDoc.CreateElement("Scouts");
            xmlDoc.AppendChild(root);

            foreach (var scout in allScouts.Values)
            {
                XmlElement xmlScout = xmlDoc.CreateElement("Scout");

                xmlScout.SetAttribute("Name", scout.Name);
                xmlScout.SetAttribute("DllName", scout.DllName);

                if (!String.IsNullOrEmpty(scout.DesiredVersion))  
                    xmlScout.SetAttribute("Version", scout.DesiredVersion);

                root.AppendChild(xmlScout);
            }

            SaferSave(xmlDoc, ScoutsFile);
        }

        public bool AddScout(ScoutInfo sInfo, bool writeToDisk = true)
        {
            lock (allScouts)
            {
                if (allScouts.ContainsKey(sInfo.Name))
                    return false;

                allScouts.Add(sInfo.Name, sInfo);

                if (writeToDisk)
                    WriteScoutsList();

                return true;
            }
        }

        public bool RemoveScout(string scoutName, bool writeToDisk = true)
        {
            lock (allScouts)
            {
                if (!allScouts.ContainsKey(scoutName))
                    return false;

                allScouts.Remove(scoutName);

                if (writeToDisk)
                    WriteScoutsList();

                return true;
            }
        }

        #endregion

        #region read and write settings
        private void ReadSettings()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(SettingsFile, xmlReaderSettings);

            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Settings"))
                throw new Exception(SettingsFile + " doesn't start with Settings");

            foreach (XmlElement xmlParam in root.ChildNodes)
            {
                if (!xmlParam.Name.Equals("Param"))
                    throw new Exception("child is not a Param in " + SettingsFile);

                string name = xmlParam.GetAttribute("Name");
                string value = xmlParam.GetAttribute("Value");

                Settings.SetParameter(name, value);
            }

            xmlReader.Close();
        }

        private void WriteSettings()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement root = xmlDoc.CreateElement("Settings");
            xmlDoc.AppendChild(root);

            lock (Settings.SettingsTable)
            {
                foreach (string paramName in Settings.SettingsTable.Keys)
                {
                    XmlElement xmlDevice = xmlDoc.CreateElement("Param");

                    xmlDevice.SetAttribute("Name", paramName);
                    xmlDevice.SetAttribute("Value", Settings.SettingsTable[paramName].Value.ToString());

                    root.AppendChild(xmlDevice);
                }
            }

            PersistentWrite(xmlDoc, SettingsFile);
        }
        #endregion

        #region read and write private settings
        private void ReadPrivateSettings()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(PrivateSettingsFile, xmlReaderSettings);

            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("PrivateSettings"))
                throw new Exception(SettingsFile + " doesn't start with Settings");

            foreach (XmlElement xmlParam in root.ChildNodes)
            {
                if (!xmlParam.Name.Equals("Param"))
                    throw new Exception("child is not a Param in " + SettingsFile);

                string name = xmlParam.GetAttribute("Name");
                string value = xmlParam.GetAttribute("Value");

                Settings.SetPrivateParameter(name, value);
            }

            xmlReader.Close();
        }

        private void WritePrivateSettings()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement root = xmlDoc.CreateElement("PrivateSettings");
            xmlDoc.AppendChild(root);

            lock (Settings.PrivateSettingsTable)
            {
                foreach (string paramName in Settings.PrivateSettingsTable.Keys)
                {
                    XmlElement xmlDevice = xmlDoc.CreateElement("Param");

                    xmlDevice.SetAttribute("Name", paramName);
                    xmlDevice.SetAttribute("Value", Settings.PrivateSettingsTable[paramName].Value.ToString());

                    root.AppendChild(xmlDevice);
                }
            }

            PersistentWrite(xmlDoc, PrivateSettingsFile);
        }
        #endregion

        // this function exists to make the write go through in case it fails due to the 
        // file not being released after an earlier write
        private void PersistentWrite(XmlDocument xmlDoc, string fileName)
        {
            int numRemainingTries = 3;

            while (numRemainingTries > 0)
            {
                try
                {
                    //xmlDoc.Save(fileName);

                    SaferSave(xmlDoc, fileName);

                    //we succeeded, so return now
                    return;
                }
                catch (System.IO.IOException ioEx)
                {
                    numRemainingTries--;

                    logger.Log("Writing to {0} failed with {1}. NumRemainingTries = {2}", fileName, ioEx.Message, numRemainingTries.ToString());

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        #region read and write user tree
        private void ReadUserTree()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(this.UsersFile, xmlReaderSettings);

            xmlDoc.Load(xmlReader);

            XmlElement rootXml = xmlDoc.FirstChild as XmlElement;

            //names are case insensitive
            string name = rootXml.GetAttribute("Name").ToLower();

            rootGroup = new UserGroupInfo(nextUserOrGroupId, name); 
            
            allGroups.Add(name, rootGroup);

            nextUserOrGroupId++;

            ReadUserSubTree(rootXml, rootGroup);

            xmlReader.Close();
        }

        private void ReadUserSubTree(XmlElement xmlParent, UserGroupInfo parent)
        {
            foreach (XmlElement xmlChild in xmlParent.ChildNodes)
            {

                UserGroupInfo child;

                //names are case insensitive
                string name = xmlChild.GetAttribute("Name").ToLower();

                if (allGroups.ContainsKey(name))
                    throw new Exception("duplicate usergroup name " + name);

                switch (xmlChild.Name)
                {
                    case "Group":
                        {
                            child = new UserGroupInfo(nextUserOrGroupId, name);
                        }
                        break;
                    case "User":
                        {
                            string password = xmlChild.GetAttribute("Password");
                            string liveId = xmlChild.GetAttribute("LiveId");
                            string LiveIdUniqueUserToken = xmlChild.GetAttribute("LiveIdUniqueUserToken");
                            DateTime activeFrom = DateTime.Parse(xmlChild.GetAttribute("ActiveFrom"));
                            DateTime activeUntil = DateTime.Parse(xmlChild.GetAttribute("ActiveUntil"));

                            child = new UserInfo(nextUserOrGroupId, name, password, activeFrom, activeUntil, liveId, LiveIdUniqueUserToken);

                            if (xmlChild.ChildNodes.Count != 0)
                                throw new Exception("User " + name + " has children");
                        }
                        break;
                    default:
                        throw new Exception("bad node name in users file " + xmlChild.Name);
                }

                AddUserGroup(child, parent, false);
                nextUserOrGroupId++;

                ReadUserSubTree(xmlChild, child);

            }
        }

        private void AddUserGroup(UserGroupInfo groupToAdd, UserGroupInfo parent, bool writeToDisk = true)
        {
            //we lock allGroups even when allUsers is being accessed
            lock (allGroups)
            {
                //add to all groups
                allGroups.Add(groupToAdd.Name, groupToAdd);

                //add to all users if this is a user
                if (groupToAdd is UserInfo)
                    allUsers.Add(groupToAdd.Name, (UserInfo)groupToAdd);

                //do the linking up and down
                groupToAdd.SetParent(parent);
                parent.AddChild(groupToAdd);

                if (writeToDisk)
                    WriteUserTree();
            }
        }

        private void WriteUserTree()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement rootXml = xmlDoc.CreateElement("Group");
            xmlDoc.AppendChild(rootXml);

            rootXml.SetAttribute("Name", rootGroup.Name);

            WriteUserSubTree(rootXml, rootGroup, xmlDoc);

            SaferSave(xmlDoc, this.UsersFile);

        }

        private void WriteUserSubTree(XmlElement xmlParent, UserGroupInfo parent, XmlDocument xmlDoc)
        {
            foreach (UserGroupInfo userGroup in parent.Children)
            {
                XmlElement xmlChild;

                if (userGroup is UserInfo)
                {
                    xmlChild = xmlDoc.CreateElement("User");

                    UserInfo userInfo = (UserInfo)userGroup;
                    xmlChild.SetAttribute("Password", userInfo.Password);
                    xmlChild.SetAttribute("ActiveFrom", userInfo.ActiveFrom.ToString());
                    xmlChild.SetAttribute("ActiveUntil", userInfo.ActiveUntil.ToString());
                    xmlChild.SetAttribute("LiveId", userInfo.LiveId);
                    xmlChild.SetAttribute("LiveIdUniqueUserToken", userInfo.LiveIdUniqueUserToken);
                }
                else
                {
                    xmlChild = xmlDoc.CreateElement("Group");
                }

                xmlChild.SetAttribute("Name", userGroup.Name);

                xmlParent.AppendChild(xmlChild);

                WriteUserSubTree(xmlChild, userGroup, xmlDoc);
            }
        }
        #endregion

        #region read and write locations
        private void ReadLocationTree()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(LocationsFile, xmlReaderSettings);

            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Location"))
                throw new Exception("modules file does not start with location. bad file: " + LocationsFile);

            string name = root.GetAttribute("Name");
            int id = ++nextLocId;

            //read the root location
            RootLocation = new Location(id, name);
            allLocations.Add(name, RootLocation);

            //read the rest of tree
            ReadLocationSubTree(root, RootLocation);

            xmlReader.Close();
        }

        private void ReadLocationSubTree(XmlElement xmlParent, Location parent)
        {
            foreach (XmlElement xmlChild in xmlParent.ChildNodes)
            {
                if (!xmlChild.Name.Equals("Location"))
                    throw new Exception("Location file has node " + xmlChild.Name);

                string name = xmlChild.GetAttribute("Name");
                int id = ++nextLocId;

                Location child = new Location(id, name);

                allLocations.Add(name, child);

                child.SetParent(parent);
                parent.AddChildLocation(child);

                //read the rest of the tree
                ReadLocationSubTree(xmlChild, child);
             }
        }
        
        public void AddConfiguredPort(PortInfo portInfo, bool writeToDisk=true)
        {

            //we always lock configuredPorts even if unconfiguredPorts is being touched

            lock (configuredPorts)
            {
                //remove from uncofigured list first if it exists there

                if (unconfiguredPorts.ContainsKey(portInfo))
                    unconfiguredPorts.Remove(portInfo);

                configuredPorts.Add(portInfo, portInfo);
                configuredPortNames.Add(portInfo.GetFriendlyName(), portInfo);

                foreach (VRole role in portInfo.GetRoles())
                {
                    if (!configuredRolesInHome.ContainsKey(role))
                        configuredRolesInHome[role] = true;
                }

                if (writeToDisk) 
                    WriteServicesList();
            }

        }

        public void AddUnconfiguredPort(PortInfo portInfo, bool writeToDisk = true)
        {
            lock (configuredPorts)
            {
                lock (unconfiguredPorts)
                {
                    if (configuredPorts.ContainsKey(portInfo))
                        throw new Exception("adding as unconfigured a port that exists in configured list: " + portInfo.ToString());

                    if (!unconfiguredPorts.ContainsKey(portInfo))
                        unconfiguredPorts.Add(portInfo, portInfo);

                    if (writeToDisk)
                        WriteServicesList();
                }
            }
        }

        #region methods to remove ports from config at runtime -rayman
        private bool RemoveConfiguredPort(PortInfo portInfo,  bool writeToDisk = true)
        {
            
            //we always lock configuredPorts even if unconfiguredPorts is being touched
            lock (unconfiguredPorts)
            {

                lock (configuredPorts)
                {
                    bool result = configuredPorts.Remove(portInfo);
                    configuredPortNames.Remove(portInfo.GetFriendlyName());
                    
                    //portInfo.SetLocation(parentLocation);
                    //parentLocation.AddChildPort(portInfo);

                    allLocations[portInfo.GetLocation().Name()].RemoveChildPort(portInfo);
                    
                    
                    // if "configured roles in home" list contains any roles from this port
                    // rebuild the configured roles in home list from the "new" configured ports list (i.e., after removing this port)
                    configuredRolesInHome.Clear();
                    foreach (PortInfo port in configuredPorts.Values) // rebuilding the configuredRolesInHome list again
                    {
                        foreach (VRole role in port.GetRoles())
                        {
                            if (!configuredRolesInHome.ContainsKey(role)) // avoiding duplicates
                                configuredRolesInHome[role] = true;
                        }
                    }

                    if (writeToDisk)
                        WriteServicesList();

                    return result;

                }
            }

        }

        private bool RemoveUnconfiguredPort(PortInfo portInfo, bool writeToDisk = true)
        {
            lock (configuredPorts)
            {
                lock (unconfiguredPorts)
                {
                    bool result = unconfiguredPorts.Remove(portInfo);

                    if (writeToDisk)
                        WriteServicesList();

                    return result;
                }
            }
        }


        public bool RemovePort(PortInfo port)
        {
            lock (configuredPorts)
            {
                lock (unconfiguredPorts)
                {
                    if (unconfiguredPorts.ContainsKey(port))
                        return RemoveUnconfiguredPort(port);
                    else if (configuredPorts.ContainsKey(port))
                        return RemoveConfiguredPort(port);
                    else
                        return false;
                }
            }
        }


        #endregion 

        private void WriteLocationTree()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement root = xmlDoc.CreateElement("Location");
            xmlDoc.AppendChild(root);

            root.SetAttribute("Name", RootLocation.Name());

            WriteLocationSubTree(root, RootLocation, xmlDoc);

            SaferSave(xmlDoc, LocationsFile);
        }


        private void WriteLocationSubTree(XmlElement xmlParent, Location parent, XmlDocument xmlDoc)
        {
            foreach (Location childLocation in parent.ChildLocations)
            {

                XmlElement xmlChild = xmlDoc.CreateElement("Location");

                xmlChild.SetAttribute("Name", childLocation.Name());

                xmlParent.AppendChild(xmlChild);

                WriteLocationSubTree(xmlChild, childLocation, xmlDoc);
            }
        }

        public bool AddLocation(string locationToAdd, string parentLocation)
        {
            lock (allLocations)
            {
                if (allLocations.ContainsKey(locationToAdd))
                {
                    return false;
                }
                else if (!allLocations.ContainsKey(parentLocation))
                {
                    return false;
                }
                else
                {
                    Location location = new Location(locationToAdd);

                    location.SetParent(allLocations[parentLocation]);
                    allLocations[parentLocation].AddChildLocation(location);

                    allLocations.Add(locationToAdd, location);

                    WriteLocationTree();

                    return true;
                }
            }
        }

        public Location GetLocation(string locationString)
        {
            lock (allLocations)
            {
                if (allLocations.ContainsKey(locationString))
                    return allLocations[locationString];
                else
                    return null;
            }
        }

        public List<string> GetAllLocations()
        {
            lock (allLocations)
            {
                return System.Linq.Enumerable.ToList<string>(allLocations.Keys);
            }
        }

        #endregion

        #region read and write module list
        private void ReadModuleList()
        {
            string fileName = ModulesFile;

            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(fileName, xmlReaderSettings);
            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Modules"))
                throw new Exception(fileName + " doesn't start with Modules");

            foreach (XmlElement xmlModule in root.ChildNodes)
            {
                if (!xmlModule.Name.Equals("Module"))
                    throw new Exception("child is not a Module in " + fileName);

                string name = xmlModule.GetAttribute("FriendlyName");
                string appName = xmlModule.GetAttribute("AppName");
                string binaryName = xmlModule.GetAttribute("BinaryName");
                string workingDir = xmlModule.GetAttribute("WorkingDir");
                string autoStartStr = xmlModule.GetAttribute("AutoStart");
                string backgroundStr = xmlModule.GetAttribute("Background");

                string version = xmlModule.GetAttribute("Version");

                string argStr = xmlModule.GetAttribute("ModuleArgStr");

                if (!argStr.Equals(""))
                    throw new Exception("module arguments are being supplied in old-fashioned way");

                //string[] words = argStr.Split(" ");

                if (workingDir.Equals(""))
                    workingDir = null;

                bool autoStart = (autoStartStr.Equals("1")) ? true : false;

                bool background = (backgroundStr.Equals("1")) ? true : false;

                string[] words = ReadModuleArguments(xmlModule);

                ModuleInfo moduleInfo = new ModuleInfo(name, appName, binaryName, workingDir, autoStart, words);
                moduleInfo.Background = background;

                // now lets set the version. if the version is  missing in the xml file set it as UnknownHomeOSUpdateVersionValue(0.0.0.0)
                if(string.IsNullOrWhiteSpace(version))
                    moduleInfo.SetDesiredVersion(Constants.UnknownHomeOSUpdateVersionValue);
                else
                    moduleInfo.SetDesiredVersion(version);
                   
                //now let's attach the manifest
                Manifest manifest = ReadManifest(xmlModule);
                moduleInfo.SetManifest(manifest);

                AddModule(moduleInfo, false);

            }

            xmlReader.Close();
        }

        private string[] ReadModuleArguments(XmlElement xmlModule)
        {
            string[] args = null;

            foreach (XmlElement xmlChild in xmlModule.ChildNodes)
            {
                if (xmlChild.Name.Equals("Args"))
                {
                    int count = int.Parse(xmlChild.GetAttribute("Count"));

                    args = new string[count];

                    for (int argNum = 1; argNum <= count; argNum++)
                    {
                        args[argNum - 1] = xmlChild.GetAttribute("val" + argNum);
                    }
                }
            }

            return args;
        }

        private Manifest ReadManifest(XmlElement xmlModule)
        {
            Manifest manifest = new Manifest();

            foreach (XmlElement xmlChild in xmlModule.ChildNodes)
            {
                if (xmlChild.Name.Equals("RoleList"))
                {
                    RoleList roleList = new RoleList();

                    foreach (XmlElement xmlRole in xmlChild.ChildNodes)
                    {
                        if (!xmlRole.Name.Equals("Role"))
                            throw new Exception("child of RoleList shouldn't be " + xmlRole.Name);

                        string roleName = xmlRole.GetAttribute("Name");

                        roleList.AddRole(new Role(roleName.ToLower()));
                    }

                    string optional = xmlChild.GetAttribute("Optional");
                    roleList.Optional = (optional.ToLower().Equals("true"));
                    manifest.AddRoleList(roleList);
                }
            }

            return manifest;
        }

        public void AddModule(ModuleInfo moduleInfo, bool writeConfigToDisk=true)
        {
            lock (allModules)
            {
                if (!allModules.ContainsKey(moduleInfo.FriendlyName())) 
                {
                    allModules.Add(moduleInfo.FriendlyName(), moduleInfo);
                }
                else 
                { 
                    logger.Log("Warning: Attempt to add an already added module " + moduleInfo.FriendlyName());
                }

                if (writeConfigToDisk)
                    WriteModuleList();
            }
        }

        public void AddModuleIfMissing(ModuleInfo moduleInfo)
        {
            lock (allModules)
            {
                if (!allModules.ContainsKey(moduleInfo.FriendlyName()))
                    AddModule(moduleInfo);
            }
        }

        public bool RemoveModule(string moduleFriendlyName)
        {
            lock (allModules)
            {
                if (!allModules.ContainsKey(moduleFriendlyName))
                {
                    logger.Log("Warning: Attempt to remove a missing module " + moduleFriendlyName);
                    return false;
                }

                bool result = allModules.Remove(moduleFriendlyName);
                
                WriteModuleList();

                return result;
            }
        }

        public void WriteModuleList()
        {
            string fileName = ModulesFile;

            XmlDocument xmlDoc = new XmlDocument();

            XmlElement root = xmlDoc.CreateElement("Modules");
            xmlDoc.AppendChild(root);

            foreach (ModuleInfo moduleInfo in allModules.Values)
            {
                XmlElement xmlModule = xmlDoc.CreateElement("Module");

                xmlModule.SetAttribute("FriendlyName", moduleInfo.FriendlyName());
                xmlModule.SetAttribute("AppName", moduleInfo.AppName());
                xmlModule.SetAttribute("BinaryName", moduleInfo.BinaryName());
                if (moduleInfo.WorkingDir() != null)
                    xmlModule.SetAttribute("WorkingDir", moduleInfo.WorkingDir());
                xmlModule.SetAttribute("AutoStart", (moduleInfo.AutoStart) ? "1" : "0");
                xmlModule.SetAttribute("Background", (moduleInfo.Background) ? "1" : "0");

                if (!string.IsNullOrWhiteSpace(moduleInfo.GetDesiredVersion()) && !moduleInfo.GetDesiredVersion().Equals(Constants.UnknownHomeOSUpdateVersionValue))
                    xmlModule.SetAttribute("Version", moduleInfo.GetDesiredVersion());

                int argCount = moduleInfo.Args().Length;

                if (argCount > 0)
                {
                    XmlElement xmlArgs = xmlDoc.CreateElement("Args");

                    xmlArgs.SetAttribute("Count", argCount.ToString());

                    for (int argIndex = 1; argIndex <= argCount; argIndex++)
                    {
                        xmlArgs.SetAttribute("val" + argIndex, moduleInfo.Args()[argIndex - 1]);
                    }

                    xmlModule.AppendChild(xmlArgs);
                }

                foreach (RoleList roleList in moduleInfo.GetManifest().GetRoleLists())
                {
                    XmlElement xmlRoleList = xmlDoc.CreateElement("RoleList");

                    xmlRoleList.SetAttribute("Optional", roleList.Optional.ToString());

                    foreach (Role role in roleList.GetRoles())
                    {
                        XmlElement xmlRole = xmlDoc.CreateElement("Role");
                        xmlRole.SetAttribute("Name", role.Name());
                        xmlRoleList.AppendChild(xmlRole);
                    }

                    xmlModule.AppendChild(xmlRoleList);
                }

                root.AppendChild(xmlModule);
            }

            SaferSave(xmlDoc, fileName);
        }
        #endregion

        #region read and write services list
        private void ReadServicesList()
        {
            string fileName = this.ServicesFile;

            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(fileName, xmlReaderSettings);
            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Services"))
                throw new Exception(fileName + " doesn't start with Services");

            foreach (XmlElement xmlChild in root.ChildNodes)
            {
                if (!xmlChild.Name.Equals("Service"))
                    throw new Exception("child is not a Service in " + fileName);

                string configuredString = xmlChild.GetAttribute("Configured").ToLower();
                bool configuredPort = (configuredString.Equals("") || configuredString.Equals("yes"));

                string moduleFacingName = xmlChild.GetAttribute("ModuleFacingName");
                string moduleName = xmlChild.GetAttribute("Module");

                if (!allModules.ContainsKey(moduleName))
                    throw new Exception("Unknown module " + moduleName + " for service " + moduleFacingName);

                PortInfo child = new PortInfo(moduleFacingName, allModules[moduleName]);

                //mark as high security if the security tag is missing or is marked as low
                string security = xmlChild.GetAttribute("Security");
                child.SetSecurity((!security.Equals("") && !security.ToLower().Equals("low")) ? true : false);

                //read in the roles
                IList<VRole> roles = new List<VRole>();

                foreach (XmlElement xmlRole in xmlChild.ChildNodes)
                {
                    if (!xmlRole.Name.Equals("Role"))
                        throw new Exception("child of Service is not Role. it is " + xmlRole.Name);

                    string roleName = xmlRole.GetAttribute("Name");

                    //if (!roleDb.ContainsKey(roleName.ToLower()))
                    //    throw new Exception("unknown role name " + roleName);

                    //roles.Add(roleDb[roleName.ToLower()]);

                    roles.Add(new Role(roleName.ToLower()));
                }

                child.SetRoles(roles);

                string friendlyName = xmlChild.GetAttribute("FriendlyName");
                child.SetFriendlyName(friendlyName);

                //read and verify location
                string locationStr = xmlChild.GetAttribute("Location");
                Location location = GetLocation(locationStr);
                if (location == null)
                   throw new Exception("Unknown location for service: " + locationStr);

                child.SetLocation(location);
                location.AddChildPort(child);

                if (configuredPort)
                {
                    AddConfiguredPort(child, false);
                }
                else
                {
                    AddUnconfiguredPort(child, false);
                }
            }

            xmlReader.Close();
        }

        public void WriteServicesList()
        {
            string fileName = this.ServicesFile;

            XmlDocument xmlDoc = new XmlDocument();

            XmlElement root = xmlDoc.CreateElement("Services");
            xmlDoc.AppendChild(root);

            foreach (PortInfo portInfo in configuredPorts.Keys)
            {
                XmlElement xmlService = xmlDoc.CreateElement("Service");

                xmlService.SetAttribute("FriendlyName", portInfo.GetFriendlyName());
                xmlService.SetAttribute("Location", portInfo.GetLocation().Name());

                if (portInfo.IsSecure())
                    xmlService.SetAttribute("Security", "High");

                xmlService.SetAttribute("Module", portInfo.ModuleFriendlyName());
                xmlService.SetAttribute("ModuleFacingName", portInfo.ModuleFacingName());

                //now add roles
                foreach (VRole role in portInfo.GetRoles())
                {
                    XmlElement xmlRole = xmlDoc.CreateElement("Role");

                    xmlRole.SetAttribute("Name", role.Name());

                    xmlService.AppendChild(xmlRole);
                }
                
                root.AppendChild(xmlService);
            }

            foreach (PortInfo portInfo in unconfiguredPorts.Keys)
            {
                XmlElement xmlService = xmlDoc.CreateElement("Service");

                xmlService.SetAttribute("FriendlyName", portInfo.GetFriendlyName());
                xmlService.SetAttribute("Module", portInfo.ModuleFriendlyName());
                xmlService.SetAttribute("ModuleFacingName", portInfo.ModuleFacingName());
                xmlService.SetAttribute("Location", portInfo.GetLocation().Name());
                xmlService.SetAttribute("Configured", "no");

                //now add roles
                foreach (VRole role in portInfo.GetRoles())
                {
                    XmlElement xmlRole = xmlDoc.CreateElement("Role");

                    xmlRole.SetAttribute("Name", role.Name());

                    xmlService.AppendChild(xmlRole);
                }

                root.AppendChild(xmlService);
            }

            SaferSave(xmlDoc, fileName);
        }
#endregion

        #region read and write access rules
        private void ReadAccessRules()
        {
            string fileName = this.RulesFile;

            XmlDocument xmlDoc = new XmlDocument();

            XmlReader xmlReader = XmlReader.Create(fileName, xmlReaderSettings);
            xmlDoc.Load(xmlReader);

            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Rules"))
                throw new Exception("rules file " + fileName + " does not begin with <Rules>");

            foreach (XmlElement xmlRule in root.ChildNodes)
            {
                if (!xmlRule.Name.Equals("Rule"))
                    throw new Exception("expected Rule. Got " + xmlRule.Name);

                foreach (XmlElement xmlUser in xmlRule.ChildNodes)
                {
                    if (!xmlUser.Name.Equals("User") && !xmlUser.Name.Equals("Group"))
                        throw new Exception("expected User. Got " + xmlUser.Name);

                    AccessRule accessRule = new AccessRule();

                    accessRule.RuleName = xmlRule.GetAttribute("Name");
                    
                    accessRule.ModuleName = xmlRule.GetAttribute("Module");

                    if (!allModules.ContainsKey(accessRule.ModuleName)
                        && !accessRule.ModuleName.Equals(Constants.GuiServiceSuffixWeb)
                        && !accessRule.ModuleName.Equals(Constants.GuiServiceSuffixWebSec)
                        && !accessRule.ModuleName.Equals(Constants.ScoutsSuffixWeb)
                        )
                        throw new Exception("unknown module in rules: " + accessRule.ModuleName);

                    accessRule.UserGroup = xmlUser.GetAttribute("Name").ToLower();
                    if (!allGroups.ContainsKey(accessRule.UserGroup))
                        throw new Exception("unknown user/group in rules: " + accessRule.UserGroup);

                    accessRule.AccessMode = (AccessMode)Enum.Parse(typeof(AccessMode), xmlUser.GetAttribute("Type"), true);

                    List<string> deviceList = new List<string>();
                    List<TimeOfWeek> timeList = new List<TimeOfWeek>();

                    foreach (XmlElement xmlChild in xmlUser.ChildNodes)
                    {
                        switch (xmlChild.Name)
                        {
                            case "Service":
                                {
                                    //it is a device
                                    string serviceName = xmlChild.GetAttribute("FriendlyName");

                                    if (!configuredPortNames.ContainsKey(serviceName)&& !serviceName.Equals("*") )
                                        throw new Exception("unknown service name in rules: " + serviceName);

                                    deviceList.Add(serviceName);
                                }
                                break;
                            case "Time":
                                {
                                    //it is time
                                    int dayOfWeek = int.Parse(xmlChild.GetAttribute("DayOfWeek"));

                                    string startMins = xmlChild.GetAttribute("StartMins");
                                    string endMins = xmlChild.GetAttribute("EndMins");

                                    int startMinsInt = (startMins.Equals("")) ? 0 : int.Parse(startMins);
                                    int endMinsInt = (endMins.Equals("")) ? 2400 : int.Parse(endMins);

                                    TimeOfWeek timeOfWeek = new TimeOfWeek(dayOfWeek, startMinsInt, endMinsInt);
                                    if (!timeOfWeek.Valid())
                                        throw new Exception("invalid time spec for rule " + accessRule.RuleName);

                                    timeList.Add(timeOfWeek);
                                }
                                break;
                            default:
                                throw new Exception("expected Device or Time. Got " + xmlChild.Name);
                        }
                    }

                    //assume always if no time was specified
                    if (timeList.Count == 0)
                        timeList.Add(new TimeOfWeek(-1, 0, 2400));

                    // assume access-rule applies to all ports of the module if no service specified
                    if(deviceList.Count==0)
                        deviceList.Add("*");

                    accessRule.DeviceList = deviceList;
                    accessRule.TimeList = timeList;

                    accessRule.Priority = 0;

                    AddAccessRule(accessRule, false);
                }
            }

            xmlReader.Close();
        }

        public void AddAccessRule(AccessRule rule, bool writeToDisk = true)
        {
            lock (allPolicies)
            {
                allPolicies.Add(rule);

                if (writeToDisk)
                    WriteAccessRules();

            }

        }

        public bool RemoveAccessRule(string appFriendlyName, string deviceFriendlyName)
        {
            lock (allPolicies)
            {
                List<AccessRule> matchingRules = GetAppDeviceRules(appFriendlyName, deviceFriendlyName);

                if (matchingRules.Count == 0) 
                    return false;

                foreach (var rule in matchingRules)
                {
                    rule.DeviceList.Remove(deviceFriendlyName);

                     if (rule.DeviceList.Count == 0)
                         allPolicies.Remove(rule);                     
                }

                WriteAccessRules();
            }

            return true;
        }

        public void RemoveAccessRulesForDevice(string deviceFriendlyName)
        {
            lock (allPolicies)
            {
                var rulesToRemove = new List<AccessRule>();

                foreach (var rule in allPolicies)
                {
                    if (rule.DeviceList.Contains(deviceFriendlyName))
                    {
                        rule.DeviceList.Remove(deviceFriendlyName);

                        if (rule.DeviceList.Count == 0)
                            rulesToRemove.Add(rule);                     
                    }
                }

                foreach (var rule in rulesToRemove)
                {
                    allPolicies.Remove(rule);
                }

                WriteAccessRules();
            }
        }

        public void RemoveAccessRulesForModule(string moduleFriendlyName)
        {
            lock (allPolicies)
            {
                var rulesToRemove = new List<AccessRule>();

                foreach (var rule in allPolicies)
                {
                    if (rule.ModuleName.Equals(moduleFriendlyName))
                    {
                            rulesToRemove.Add(rule);
                    }
                }

                foreach (var rule in rulesToRemove)
                {
                    allPolicies.Remove(rule);
                }

                WriteAccessRules();
            }
        }

        public void RemoveAccessRulesForUser(string userName)
        {
            lock (allPolicies)
            {
                var rulesToRemove = new List<AccessRule>();

                foreach (var rule in allPolicies)
                {
                    if (rule.UserGroup.Equals(userName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        rulesToRemove.Add(rule);
                    }
                }

                foreach (var rule in rulesToRemove)
                {
                    allPolicies.Remove(rule);
                }

                WriteAccessRules();
            }
        }

        public List<AccessRule> GetAppDeviceRules(string appFriendlyName, string deviceFriendlyName)
        {
            List<AccessRule> retList = new List<AccessRule>();

            lock (allPolicies)
            {
                foreach (var rule in allPolicies)
                {
                    if (rule.ModuleName.Equals(appFriendlyName))
                    {
                        if (rule.DeviceList.Contains(deviceFriendlyName))
                        {
                            retList.Add(rule);
                        }
                    }
                }
            }

            return retList;
        }


        private void WriteAccessRules()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement root = xmlDoc.CreateElement("Rules");
            xmlDoc.AppendChild(root);

            foreach (AccessRule rule in allPolicies)
            {
                XmlElement xmlRule = xmlDoc.CreateElement("Rule");
                root.AppendChild(xmlRule);

                xmlRule.SetAttribute("Name", rule.RuleName);
                xmlRule.SetAttribute("Module", rule.ModuleName);

                XmlElement xmlUser = xmlDoc.CreateElement("User");
                xmlRule.AppendChild(xmlUser);

                xmlUser.SetAttribute("Name", rule.UserGroup);
                xmlUser.SetAttribute("Type", rule.AccessMode.ToString());
                xmlUser.SetAttribute("Priority", rule.Priority.ToString());

                foreach (string device in rule.DeviceList)
                {

                    XmlElement xmlDevice = xmlDoc.CreateElement("Service");
                    xmlUser.AppendChild(xmlDevice);

                    xmlDevice.SetAttribute("FriendlyName", device);
                }

                foreach (TimeOfWeek timeOfWeek in rule.TimeList)
                {

                    XmlElement xmlTime = xmlDoc.CreateElement("Time");
                    xmlUser.AppendChild(xmlTime);

                    xmlTime.SetAttribute("DayOfWeek", timeOfWeek.DayOfWeek.ToString());
                    xmlTime.SetAttribute("StartMins", timeOfWeek.StartMins.ToString());
                    xmlTime.SetAttribute("EndMins", timeOfWeek.EndMins.ToString());
                }

            }

            SaferSave(xmlDoc, this.RulesFile);
        }
        #endregion

        public Tuple<UserInfo, string> AddLiveIdUser(string userName, string parentGroup, string liveId, string liveIdToken)
        {
            lock (allGroups)
            {
                if (allGroups.ContainsKey(userName.ToLower()))
                    return new Tuple<UserInfo, string> (null, "Attempt to add a user with duplicate username");

                if (!allGroups.ContainsKey(parentGroup.ToLower()))
                    return new Tuple<UserInfo, string> (null, "Parent group does not exist");

                //check for the uniqueness of LiveId and LiveIdUniqueUserToken 
                foreach (var userGroupInfo in allGroups.Values)
                {
                    var userInfo = userGroupInfo as UserInfo;

                    if (userInfo == null) continue;

                    if (userInfo.LiveIdUniqueUserToken.Equals(liveIdToken, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return new Tuple<UserInfo, string>(null, "Duplicate liveIdUniqueUserToken");
                    }
                     if (userInfo.LiveId.Equals(liveId, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return new Tuple<UserInfo, string>(null, "Duplicate liveId");
                    }
                }

                UserGroupInfo parent = allGroups[parentGroup];

                UserInfo user = new UserInfo(nextUserOrGroupId, userName, "", DateTime.MinValue, DateTime.MaxValue, liveId, liveIdToken);

                AddUserGroup(user, parent);

                nextUserOrGroupId++;

                return new Tuple<UserInfo,string> (user, "");
            }
        }

        public Tuple<UserInfo, string> RemoveLiveIdUser(string userName)
        {
            RemoveAccessRulesForUser(userName);

            lock (allGroups)
            {
                if (!allUsers.ContainsKey(userName.ToLower()))
                    return new Tuple<UserInfo, string>(null, "User not found");

                UserInfo user = allUsers[userName.ToLower()];

                UserGroupInfo parent = user.Parent;

                parent.RemoveChild(user);

                //remove from the groups 
                allGroups.Remove(userName.ToLower());

                //remove from users
                allUsers.Remove(userName.ToLower());

                WriteUserTree();

                return new Tuple<UserInfo, string>(user, ""); 
            }
        }


        public bool IsCompatableWithHome(HomeStoreApp app)
        {
            return app.Manifest.IsCompatibleWithHome(System.Linq.Enumerable.ToList<VRole>(configuredRolesInHome.Keys));
        }
         
        public List<ModuleInfo> GetCompatibleModules(VRole role)
        {
            var roleCompatibleApps = new List<ModuleInfo>();

            lock (allModules)
            {
                foreach (var mInfo in allModules.Values)
                {
                    if (mInfo.GetManifest().IsCompatibleWithRole(role))
                        roleCompatibleApps.Add(mInfo);
                }
            }
            return roleCompatibleApps;
        }

        public List<ModuleInfo> GetCompatibleModules(PortInfo portInfo)
        {
            var retList = new List<ModuleInfo>();

            foreach (VRole role in portInfo.GetRoles())
            {
                var roleCompatibleModules = GetCompatibleModules(role);

                foreach (var mInfo in roleCompatibleModules)
                {
                    if (!retList.Contains(mInfo))
                        retList.Add(mInfo);
                }
            }

            return retList;
        }

        public List<ModuleInfo> GetCompatibleModules(string deviceFriendlyName)
        {
            PortInfo pInfoForDevice = null;

            lock (configuredPorts)
            {
                foreach (var pInfo in configuredPorts.Values)
                    if (pInfo.GetFriendlyName().Equals(deviceFriendlyName))
                        pInfoForDevice = pInfo;
            }

            if (pInfoForDevice == null)
                return null;

            return GetCompatibleModules(pInfoForDevice);
        }


        public List<PortInfo> GetCompatiblePorts(Manifest manifest)
        {
            var roleCompatiblePorts = new List<PortInfo>();

            lock (configuredPorts)
            {
                foreach (PortInfo pInfo in configuredPorts.Values)
                {
                    foreach (VRole role in pInfo.GetRoles())
                    {
                        if (manifest.IsCompatibleWithRole(role))
                        {
                            //add to the list if it is not already there
                            if (!roleCompatiblePorts.Contains(pInfo))
                                roleCompatiblePorts.Add(pInfo);

                            continue;
                        }
                    }
                }
            }

            return roleCompatiblePorts;
        }

        public List<PortInfo> GetCompatiblePorts(string appFriendlyName)
        {
            lock (allModules)
            {
                if (allModules.ContainsKey(appFriendlyName))
                {
                    var module = allModules[appFriendlyName];
                    return GetCompatiblePorts(module.GetManifest());
                }
                else
                {
                    logger.Log("appFriendlyName {0} not found in allModules while searching for GetCompatibleDevicesForApp", appFriendlyName);
                    return null;
                }

            }
        }

        public bool CanAppAccessDevice(string appFriendlyName, string deviceFriendlyName)
        {
            lock (allPolicies)
            {
                var matchingRules = GetAppDeviceRules(appFriendlyName, deviceFriendlyName);
                return (matchingRules.Count > 0);
            }
        }

        public ObservableCollection<AccessRule> GetRules(string userOrGroup, string app, string device, string location, AccessMode accessType, 
                                                int startTime, int endTime, int day)
        {
            var retRules = new ObservableCollection<AccessRule>();

            foreach (AccessRule rule in allPolicies)
            {
                //check user group membership
                UserGroupInfo queryGroup = allGroups[userOrGroup];
                UserGroupInfo ruleGroup = allGroups[rule.UserGroup];

                if (!queryGroup.Equals(ruleGroup) && 
                    !queryGroup.IsDescendant(ruleGroup) &&
                    !queryGroup.IsAncestor(ruleGroup))
                    continue;


                //check if the app matches
                if (!app.Equals("All") && 
                    !app.Equals(rule.ModuleName))
                    continue;

                //check if device is in the list membership
                if (!device.Equals("") &&
                    !rule.DeviceList.Contains(device))
                    continue;

                //check for location matches
                if (device.Equals("") && !location.Equals(""))
                {
                    bool locationMatches = false;
                    Location queryLocation = allLocations[location];

                    foreach (string deviceInRule in rule.DeviceList)
                    {
                        VPortInfo portInfo = configuredPortNames[deviceInRule];

                        if (queryLocation.ContainsPort(portInfo))
                            locationMatches = true;
                    }

                    if (!locationMatches)
                        continue;
                }

                //check for access type matches
                if (accessType != AccessMode.All &&
                    accessType != rule.AccessMode)
                    continue;

                //check for day of week matches
                TimeOfWeek queryTimeOfWeek = new TimeOfWeek(day, startTime, endTime);

                bool dayOfWeekMatches = false;

                foreach (TimeOfWeek ruleTimeOfWeek in rule.TimeList) 
                {
                    if (ruleTimeOfWeek.Overlaps(queryTimeOfWeek))
                    {
                        dayOfWeekMatches = true;
                        break;
                    }
                }

                if (!dayOfWeekMatches)
                    continue;

                retRules.Add(rule);
            }

            return retRules;
        }

        public void UpdateDeviceDetails(string deviceUniqueName, bool configured, string driverFriendlyName)
        {
            lock (allDevices)
            {
                if (allDevices.ContainsKey(deviceUniqueName))
                {
                    allDevices[deviceUniqueName].Details.Configured = configured;
                    allDevices[deviceUniqueName].Details.DriverFriendlyName = driverFriendlyName;
                }
                else
                {
                    logger.Log("Error: cannot update device details. device {0} not found", deviceUniqueName);
                }

                WriteDeviceList();
            }
        }

        public void ProcessNewDiscoveryResults(List<Device> deviceList)
        {
            lock (allDevices)
            {
                foreach (Device newDevice in deviceList)
                {
                    if (allDevices.ContainsKey(newDevice.UniqueName))
                    {
                        newDevice.Details = allDevices[newDevice.UniqueName].Details;

                        allDevices[newDevice.UniqueName] = newDevice;
                    }
                    else
                    {
                        newDevice.Details.Configured = false;
                        newDevice.Details.DriverFriendlyName = "";

                        allDevices.Add(newDevice.UniqueName, newDevice);

                        logger.Log("Discovered new device: {0} {1}", newDevice.UniqueName, newDevice.DeviceIpAddress);
                    }
                }

                WriteDeviceList();
            }
        }

        public bool RemoveDevice(string uniqueDeviceId)
        {
            lock (allDevices)
            {
                bool result = allDevices.Remove(uniqueDeviceId);

                if (!result)
                    logger.Log("Warning: got a request to remove a non-existent device " + uniqueDeviceId);
                else
                    WriteDeviceList();

                return result;
            }
        }


        public void SetDeviceDriverParams(Device targetDevice, List<string> paramList)
        {
            lock (allDevices)
            {
                foreach (Device device in allDevices.Values)
                {
                    if (device.UniqueName.Equals(targetDevice.UniqueName))
                    {
                        //make sure that you update device, not the targetDevice
                        device.Details.DriverParams = paramList;

                        WriteDeviceList();

                        return;
                    }
                }

                //no matching device was found :(

                throw new Exception(String.Format("Matching device not found in config.allDevice. DeviceToUpdate = {0}", targetDevice.UniqueName)); 
            }
        }

        public List<string> GetDeviceDriverParams(Device targetDevice)
        {
            lock (allDevices)
            {
                foreach (Device device in allDevices.Values)
                {
                    if (device.UniqueName.Equals(targetDevice.UniqueName))
                    {
                        //make sure you fetch from device, not targetDevice
                        return device.Details.DriverParams;
                   }
                }

                //no matching device was found :(

                throw new Exception(String.Format("Matching device not found in allDevice. DeviceToUpdate = {0}", targetDevice.UniqueName));
            }
        }

        public ObservableCollection<Device> GetUnconfiguredDevices()
        {
            ObservableCollection<Device> deviceList = new ObservableCollection<Device>();

            lock (allDevices)
            {
                foreach (Device device in allDevices.Values)
                {

                    if (!device.Details.Configured)
                        deviceList.Add(device);
                }
            }

            return deviceList;
        }

        public List<PortInfo> GetConfiguredPorts()
        {
            List<PortInfo> portList = new List<PortInfo>();

            lock (configuredPorts)
            {
                foreach (PortInfo pInfo in configuredPorts.Values)
                {
                    portList.Add(pInfo);
                }
            }

            return portList;
        }

        public bool IsConfiguredDevice(string uniqueName)
        {
            lock (allDevices)
            {
                if (allDevices.ContainsKey(uniqueName))
                {
                    Device device = allDevices[uniqueName];

                    if (device.Details.Configured)
                        return true;
                }

                return false;
            }

        }

        /// <summary>
        ///returns the device object given the unique name of the device
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        public Device GetDevice(string uniqueName)
        {
            lock (allDevices)
            {
                if (allDevices.ContainsKey(uniqueName))
                    return allDevices[uniqueName];

                return null;
            }
        }

        /// <summary>
        ///returns the device object given the unique name of the device
        /// </summary>
        /// <param name="moduleFriendlyName"></param>
        /// <returns></returns>
        public List<Device> GetDevicesForModule(string moduleFriendlyName)
        {
            var retList = new List<Device>();

            lock (allDevices)
            {
                foreach (Device device in allDevices.Values)
                {
                    if (device.Details.DriverFriendlyName.Equals(moduleFriendlyName))
                        retList.Add(device);
                }
            }

            return retList;
        }


        public PortInfo GetMatchingPortInfo(VPortInfo targetPortInfo)
        {
            lock (configuredPorts)
            {
                if (configuredPorts.ContainsKey(targetPortInfo))
                    return configuredPorts[targetPortInfo];

                if (unconfiguredPorts.ContainsKey(targetPortInfo))
                    return unconfiguredPorts[targetPortInfo];

                return null;
            }
        }

        public void UpdateRoleList(VPortInfo storedPortInfo)
        {
            lock (configuredPorts)
            {
                //if this is a configured port, rebuild configuredRolesInHome
                if (configuredPorts.ContainsKey(storedPortInfo))
                {
                    configuredRolesInHome.Clear();

                    foreach (PortInfo port in configuredPorts.Values) // rebuilding the configuredRolesInHome list again
                    {
                        foreach (VRole role in port.GetRoles())
                        {
                            if (!configuredRolesInHome.ContainsKey(role)) // avoiding duplicates
                                configuredRolesInHome[role] = true;
                        }
                    }

                }

                //write to disk
                WriteServicesList();
            }
        }

        public string GetConfSetting(string paramName)
        {
            lock (Settings.SettingsTable)
            {
                try
                {
                    return Settings.GetParameter(paramName).ToString();
                }
                catch (Exception)
                {
                    logger.Log("Exception while getting configuration param: {0}", paramName);
                    return null;
                }
            }
        }

        public void UpdateConfSetting(string paramName, object paramValue)
        {
            Settings.SetParameter(paramName, paramValue);
            lock (Settings.SettingsTable)
            {
                WriteSettings();
            }
        }

        public string GetPrivateConfSetting(string paramName)
        {
            lock (Settings.PrivateSettingsTable)
            {
                try
                {
                    return Settings.GetPrivateParameter(paramName).ToString();
                }
                catch (Exception)
                {
                    logger.Log("Exception while getting private configuration param: {0}", paramName);
                    return null;
                }
            }
        }

        public void UpdatePrivateConfSetting(string paramName, object paramValue)
        {
            Settings.SetPrivateParameter(paramName, paramValue);
            lock (Settings.PrivateSettingsTable)
            {
                WritePrivateSettings();
            }
        }


        //get an unconfigured service (port) that corresponds to a device with the input uniqueDeviceId
        //the assumption is that the module facing name of the port contains the device 
        public List<PortInfo> GetUnconfiguredPorts(string uniqueDeviceIdOrModuleFacingName)
        {
            var retList = new List<PortInfo>();

            //we always lock configuredPorts for unconfiguredPorts
            lock (configuredPorts)
            {
                foreach (PortInfo pInfo in unconfiguredPorts.Values)
                {
                    if (pInfo.ModuleFacingName().Contains(uniqueDeviceIdOrModuleFacingName))
                        retList.Add(pInfo);
                }
            }

            return retList;
        }

        public List<PortInfo> GetUnconfiguredPorts()
        {
            //we lock configuredPorts for unconfiguredPorts as well
            lock (configuredPorts)
            {
                return unconfiguredPorts.Values.ToList();
            }
        }


        //get a configured service (port) that corresponds to a device with the input uniqueDeviceId
        //the assumption is that the module facing name of the port contains the device 
        public List<PortInfo> GetConfiguredPortsUsingDeviceId(string uniqueDeviceId)
        {
            var retList = new List<PortInfo>();

            lock (configuredPorts)
            {
                foreach (PortInfo pInfo in configuredPorts.Values)
                {
                    if (pInfo.ModuleFacingName().Contains(uniqueDeviceId))
                        retList.Add(pInfo);
                }
            }

            return retList;
        }

        public PortInfo GetConfiguredPortUsingFriendlyName(string friendlyName)
        {
            lock (configuredPorts)
            {
                //since we enforce uniqueness on friendlyname, there should be only one
                foreach (PortInfo pInfo in configuredPorts.Values)
                {
                    if (pInfo.GetFriendlyName().Equals(friendlyName))
                        return pInfo;
                }
            }

            return null;
        }

        public List<PortInfo> GetPortsUsingModuleFriendlyName(string moduleFriendlyName)
        {
            var retList = new List<PortInfo>();

            //we lock configuredPorts for unconfiguredPorts as well
            lock (configuredPorts)
            {
                foreach (PortInfo pInfo in configuredPorts.Values)
                {
                    if (pInfo.ModuleFriendlyName().Equals(moduleFriendlyName))
                        retList.Add(pInfo);
                }

                foreach (PortInfo pInfo in unconfiguredPorts.Values)
                {
                    if (pInfo.ModuleFriendlyName().Equals(moduleFriendlyName))
                        retList.Add(pInfo);
                }
            }

            return retList;
        }

        public PortInfo GetConfiguredPortUsingModuleFacingName(string moduleName, string moduleFacingName)
        {
            lock (configuredPorts)
            {
                //since we enforce uniqueness on friendlyname, there should be only one
                foreach (PortInfo pInfo in configuredPorts.Values)
                {
                    if (pInfo.ModuleFriendlyName().Equals(moduleName) && 
                        pInfo.ModuleFacingName().Equals(moduleFacingName))
                        return pInfo;
                }
            }

            return null;
        }

        public ModuleInfo GetModule(string friendlyName)
        {
            lock (allModules)
            {
                if (allModules.ContainsKey(friendlyName))
                    return allModules[friendlyName];

                return null;
            }
        }

        public List<PortInfo> GetPorts(string uniqueDeviceId)
        {
            var retList = GetUnconfiguredPorts(uniqueDeviceId);
            retList.AddRange(GetConfiguredPortsUsingDeviceId(uniqueDeviceId));
            return retList;
        }

        public List<ModuleInfo> GetAllForegroundModules()
        {
            List<ModuleInfo> mInfoList = new List<ModuleInfo>();

            lock (allModules)
            {
                foreach (var mInfo in allModules.Values) 
                {
                    if (!mInfo.Background)
                        mInfoList.Add(mInfo);
                }
            }

            return mInfoList;
        }

        internal bool IsAppInstalled(string appName)
        {
            lock (allModules)
            {
                foreach (var mInfo in allModules.Values)
                {
                    if (mInfo.AppName().Equals(appName))
                        return true;
                }
            }

            return false;
        }

        public List<UserInfo> GetAllUsers()
        {
            lock (allUsers)
            {
                return allUsers.Values.ToList();
            }
        }

        public List<UserGroupInfo> GetAllGroups()
        {
            lock (allGroups)
            {
                return allGroups.Values.ToList();
            }
        }

        /// <summary>
        /// Checks if the username and password are valid
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public DateTime ValidUserUntil(string username, string password)
        {

            username = username.ToLower();

            lock (allUsers)
            {
                if (allUsers.ContainsKey(username) &&
                    allUsers[username].Password.Equals(password) &&
                    DateTime.Now > allUsers[username].ActiveFrom)
                {
                    return allUsers[username].ActiveUntil;
                }
            }

            return DateTime.MinValue;
        }

        public DateTime ValidLiveIdUntil(string LiveIdUniqueUserToken)
        {
            lock (allUsers)
            {
                foreach (UserInfo user in allUsers.Values)
                {
                    if (user.LiveIdUniqueUserToken.Equals(LiveIdUniqueUserToken))
                        if (DateTime.Now > user.ActiveFrom)
                            return user.ActiveUntil;

                }
            }

            return DateTime.MinValue;
        }

        public List<VRole> GetAllRolesInHome()
        {
            lock (configuredRolesInHome)
            {
                return configuredRolesInHome.Keys.ToList();
            }
        }

        internal string GetDeviceIpAddress(string deviceId)
        {
            lock (allDevices)
            {
                if (allDevices.ContainsKey(deviceId))
                    return allDevices[deviceId].DeviceIpAddress;
            }

            return null;
        }

        internal List<ScoutInfo> GetAllScouts()
        {
            lock (allScouts)
            {
                return allScouts.Values.ToList();
            }
        }

        internal List<AccessRule> GetAllPolicies()
        {
            lock (allPolicies)
            {
                List<AccessRule> newList = new List<AccessRule>();
                newList.AddRange(allPolicies);
                return newList;
            }
        }
    }
}
