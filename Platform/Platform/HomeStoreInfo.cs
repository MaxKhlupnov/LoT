using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Xml;

namespace HomeOS.Hub.Platform
{
    public class HomeStoreDb
    {
        public Dictionary<string, Role> roleDb = new Dictionary<string, Role>();
        public Dictionary<string, HomeStoreApp> moduleDb = new Dictionary<string, HomeStoreApp>();
        public Dictionary<string, HomeStoreDevice> deviceDb = new Dictionary<string, HomeStoreDevice>();
        public Dictionary<string, HomeStoreScout> scoutDb = new Dictionary<string, HomeStoreScout>();

        VLogger logger;

        XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();

        public HomeStoreDb(VLogger logger)
        {
            this.logger = logger;

            xmlReaderSettings.IgnoreComments = true;
        }

        public void Populate()
        {
            try
            {
                ReadRoleDb(new Uri(Settings.HomeStoreBase + "\\" + Constants.RoleDbFileName));
            }
            catch (Exception exception)
            {
                logger.Log("Exception while reading role db: {0}", exception.ToString());
            }

            try
            {
                ReadHomeStoreApps(new Uri(Settings.HomeStoreBase + "\\" + Constants.ModuleDbFileName));
            }
            catch (Exception exception)
            {
                logger.Log("Exception while reading module db: {0}", exception.ToString());
            }

            try
            {
                ReadHomeStoreDevices(new Uri(Settings.HomeStoreBase + "\\" + Constants.DeviceDbFileName));
            }
            catch (Exception exception)
            {
                logger.Log("Exception while reading device db: {0}", exception.ToString());
            }

            try
            {
                ReadHomeStoreScouts(new Uri(Settings.HomeStoreBase + "\\" + Constants.ScoutDbFileName));
            }
            catch (Exception exception)
            {
                logger.Log("Exception while reading scout db: {0}", exception.ToString());
            }

        }

        public void ReadRoleDb(Uri fileName)
        {
            System.Net.WebRequest req = System.Net.WebRequest.Create(fileName);
            System.Net.WebResponse resp;
            try
            {
                resp = req.GetResponse();
            }
            catch (System.Net.WebException)
            {
                logger.Log("error reading RoleDb from {0}", fileName.ToString());
                return;
            }

            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(resp.GetResponseStream(), xmlReaderSettings);

            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Roles"))
                throw new Exception(fileName + " doesn't start with Roles");

            foreach (XmlElement xmlChild in root.ChildNodes)
            {
                if (!xmlChild.Name.Equals("Role"))
                    throw new Exception("child is not a Role in " + fileName);

                //int id = int.Parse(xmlChild.GetAttribute("Id"));
                string name = xmlChild.GetAttribute("Name");

                roleDb.Add(name.ToLower(), new Role(name.ToLower()));
            }

            xmlReader.Close();
        }

        //homestore functions
        public void ReadHomeStoreApps(Uri fileName)
        {
            System.Net.WebRequest req = System.Net.WebRequest.Create(fileName);
            System.Net.WebResponse resp;
            try
            {
                resp = req.GetResponse();
            }
            catch (System.Net.WebException)
            {
                logger.Log("error reading HomeStoreApps from {0}", fileName.ToString());
                return;
            }

            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(resp.GetResponseStream(), xmlReaderSettings);
            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Modules"))
                throw new Exception(fileName + " doesn't start with Modules");

            foreach (XmlElement xmlModule in root.ChildNodes)
            {
                try
                {
                    HomeStoreApp homeStoreApp = ReadHomeStoreAppFromXml(fileName, xmlModule);
                    AddAppToDB(homeStoreApp);
                }
                catch (Exception ex)
                {
                    logger.Log("Error reading/adding a home store app: " + ex.ToString());
                }
            }

            xmlReader.Close();
        }

        public void ReadHomeStoreScouts(Uri fileName)
        {
            System.Net.WebRequest req = System.Net.WebRequest.Create(fileName);
            System.Net.WebResponse resp;
            try
            {
                resp = req.GetResponse();
            }
            catch (System.Net.WebException)
            {
                logger.Log("error reading HomeStoreApps from {0}", fileName.ToString());
                return;
            }

            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(resp.GetResponseStream(), xmlReaderSettings);
            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Scouts"))
                throw new Exception(fileName + " doesn't start with Scouts");

            foreach (XmlElement xmlModule in root.ChildNodes)
            {
                try
                {
                    HomeStoreScout homeStoreScout = ReadHomeStoreScoutFromXml(fileName, xmlModule);
                    AddScoutToDB(homeStoreScout);
                }
                catch (Exception ex)
                {
                    logger.Log("Error reading/adding a home store app: " + ex.ToString());
                }
            }

            xmlReader.Close();
        }

        public void ReadHomeStoreDevices(Uri fileName)
        {
            System.Net.WebRequest req = System.Net.WebRequest.Create(fileName);
            System.Net.WebResponse resp;
            try
            {
                resp = req.GetResponse();
            }
            catch (System.Net.WebException)
            {
                logger.Log("error reading HomeStoreDevices from {0}", fileName.ToString());
                return;
            }

            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(resp.GetResponseStream(), xmlReaderSettings);

            xmlDoc.Load(xmlReader);
            XmlElement root = xmlDoc.FirstChild as XmlElement;

            if (!root.Name.Equals("Devices"))
                throw new Exception(fileName + " doesn't start with Devicess");

            foreach (XmlElement xmlModule in root.ChildNodes)
            {
                HomeStoreDevice homeStoreDev = ReadHomeStoreDeviceFromXml(fileName, xmlModule);
                AddDeviceToDB(homeStoreDev);
            }

            xmlReader.Close();
        }

        private void AddAppToDB(HomeStoreApp app)
        {
            moduleDb.Add(app.AppName, app);
        }

        private void AddScoutToDB(HomeStoreScout scout)
        {
            scoutDb.Add(scout.Name, scout);
        }

        private void AddDeviceToDB(HomeStoreDevice dev)
        {
            deviceDb.Add(dev.DeviceName, dev);
        }

        /// <summary>
        /// Takes an xml node which describes a homestore app and returns
        /// a HomeStoreApp structure
        /// </summary>
        /// <param name="xmlModule">the xml subtree of the app</param>
        /// <returns>The relevant HomeStoreApp structure</returns>
        private HomeStoreApp ReadHomeStoreAppFromXml(Uri baseUri, XmlElement xmlModule)
        {
            if (!xmlModule.Name.Equals("Module"))
                throw new Exception("child is not a Module in " + xmlModule);

            HomeStoreApp homeStoreApp = new HomeStoreApp();

            homeStoreApp.AppName = xmlModule.GetAttribute("AppName");
            homeStoreApp.BinaryName = xmlModule.GetAttribute("BinaryName");
            homeStoreApp.Description = xmlModule.GetAttribute("Description");
            homeStoreApp.Rating = int.Parse(xmlModule.GetAttribute("Rating"));

            #region we don't have binary url anymore
            //try
            //{
            //    string binaryUrlString = xmlModule.GetAttribute("BinaryUrl");
            //    if (!String.IsNullOrWhiteSpace(binaryUrlString))
            //    {
            //        //this function does the right thing if binaryUrlString is already absolute
            //        homeStoreApp.BinaryUrl = new Uri(baseUri, binaryUrlString).ToString();
            //    }
            //    else
            //    {
            //        homeStoreApp.BinaryUrl = null;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    logger.Log("exception in parsing BinaryUrl for {0}: {1}", homeStoreApp.AppName, ex.ToString());
            //    homeStoreApp.BinaryUrl = null;
            //}
            #endregion

            homeStoreApp.IconUrl = null;
            try
            {
                string iconUrlString = xmlModule.GetAttribute("IconUrl");
                if (!String.IsNullOrWhiteSpace(iconUrlString))
                {
                    //this function does the right thing if iconUrlString is already absolute
                    homeStoreApp.IconUrl = new Uri(baseUri, iconUrlString).ToString();
                }
            }
            catch (Exception ex)
            {
                logger.Log("exception in parsing IconUrl for {0}: {1}", homeStoreApp.AppName, ex.ToString());
            }

            homeStoreApp.Version = null;
            try
            {
                string versionString = xmlModule.GetAttribute("Version");
                if (!String.IsNullOrWhiteSpace(versionString))
                {
                    homeStoreApp.Version = versionString;
                }
            }
            catch (Exception ex)
            {
                logger.Log("exception in parsing Version for {0}: {1}", homeStoreApp.AppName, ex.ToString());
            }

            homeStoreApp.Manifest = ReadManifest(xmlModule, roleDb);

            //this field is filled in later
            homeStoreApp.CompatibleWithHome = false;

            return homeStoreApp;
        }

        /// <summary>
        /// Takes an xml node which describes a homestore scout and returns
        /// a HomeStoreScout structure
        /// </summary>
        /// <param name="xmlScout">the xml subtree of the app</param>
        /// <returns>The relevant HomeStoreScout structure</returns>
        private HomeStoreScout ReadHomeStoreScoutFromXml(Uri baseUri, XmlElement xmlScout)
        {
            if (!xmlScout.Name.Equals("Scout"))
                throw new Exception("child is not a Scout in " + xmlScout);

            HomeStoreScout homeStoreScout = new HomeStoreScout();

            homeStoreScout.Name = xmlScout.GetAttribute("Name");
            homeStoreScout.DllName = xmlScout.GetAttribute("DllName").Replace(".dll", "");
            homeStoreScout.Description = xmlScout.GetAttribute("Description");
            homeStoreScout.Rating = int.Parse(xmlScout.GetAttribute("Rating"));

            homeStoreScout.IconUrl = null;
            try
            {
                string iconUrlString = xmlScout.GetAttribute("IconUrl");
                if (!String.IsNullOrWhiteSpace(iconUrlString))
                {
                    //this function does the right thing if iconUrlString is already absolute
                    homeStoreScout.IconUrl = new Uri(baseUri, iconUrlString).ToString();
                }
            }
            catch (Exception ex)
            {
                logger.Log("exception in parsing IconUrl for {0}: {1}", homeStoreScout.Name, ex.ToString());
            }

            homeStoreScout.Version = Constants.UnknownHomeOSUpdateVersionValue;
            try
            {
                string versionString = xmlScout.GetAttribute("Version");
                if (!String.IsNullOrWhiteSpace(versionString))
                {
                    homeStoreScout.Version = versionString;
                }
            }
            catch (Exception ex)
            {
                logger.Log("exception in parsing Version for {0}: {1}", homeStoreScout.Name, ex.ToString());
            }

            return homeStoreScout;
        }

        /// <summary>
        /// Takes an xml node which describes a homestore app and returns
        /// a HomeStoreApp structure
        /// </summary>
        /// <param name="xmlDevice">the xml subtree of the app</param>
        /// <returns>The relevant HomeStoreApp structure</returns>
        public static HomeStoreDevice ReadHomeStoreDeviceFromXml(Uri baseUri, XmlElement xmlDevice)
        {
            if (!xmlDevice.Name.Equals("Device"))
                throw new Exception("child is not a Device in " + xmlDevice);

            HomeStoreDevice homeStoreDev = new HomeStoreDevice();

            homeStoreDev.DeviceName = xmlDevice.GetAttribute("DeviceName");
            homeStoreDev.ManufacturerName = xmlDevice.GetAttribute("ManufacturerName");
            homeStoreDev.Description = xmlDevice.GetAttribute("Description");
            homeStoreDev.Rating = int.Parse(xmlDevice.GetAttribute("Rating"));
            homeStoreDev.Model = xmlDevice.GetAttribute("Model");
            try
            {
                string iconUrlString = xmlDevice.GetAttribute("IconUrl");
                if (!iconUrlString.Equals(""))
                {
                    homeStoreDev.IconUrl = new Uri(baseUri, xmlDevice.GetAttribute("IconUrl")).ToString();
                }
                else
                {
                    homeStoreDev.IconUrl = null;
                }
            }
            catch (Exception)
            {
                homeStoreDev.IconUrl = null;
            }

            homeStoreDev.Roles = new List<string>();
            homeStoreDev.ValidDrivers = new List<string>();

            foreach (XmlElement child in xmlDevice.ChildNodes)
            {
                if (child.Name.Equals("RoleList"))
                {
                    foreach (XmlElement role in child)
                    {
                        if (role.Name.Equals("Role"))
                        {
                            homeStoreDev.Roles.Add(role.GetAttribute("Name"));
                        }
                    }
                }
                else if (child.Name.Equals("DriverList"))
                {
                    foreach (XmlElement driver in child)
                    {
                        if (driver.Name.Equals("Driver"))
                        {
                            homeStoreDev.ValidDrivers.Add(driver.GetAttribute("Name"));
                        }
                    }
                }
            }

            return homeStoreDev;
        }

        private Manifest ReadManifest(XmlElement xmlModule, IDictionary<string, Role> roleDb)
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

                        if (!roleDb.ContainsKey(roleName.ToLower()))
                            throw new Exception("unknown role name: " + roleName);

                        roleList.AddRole(roleDb[roleName.ToLower()]);
                    }

                    string optional = xmlChild.GetAttribute("Optional");
                    roleList.Optional = (optional.ToLower().Equals("true"));
                    manifest.AddRoleList(roleList);
                }
            }

            return manifest;
        }
    }


    public class HomeStoreInfo
    {
        DateTime lastRefreshed;

        VLogger logger;

        HomeStoreDb storeDb;

        public HomeStoreInfo(VLogger logger)
        {
            this.logger = logger;

            RefreshDbs(null, null);

            if (Settings.HomeStoreRefreshIntervalsMins <= 0 ||
                Settings.HomeStoreRefreshIntervalsMins >= Int32.MaxValue / (60000))  //60000 = 60 * 1000 (secs) * (ms)
            {
                logger.Log("Illegal refresh value {0}. Must be >= 0 and < {1}", Settings.HomeStoreRefreshIntervalsMins.ToString(), (Int32.MaxValue / (60000)).ToString());
            }
            else
            {
                System.Timers.Timer refreshTimer = new System.Timers.Timer(Settings.HomeStoreRefreshIntervalsMins * 60000);
                //System.Timers.Timer refreshTimer = new System.Timers.Timer(10*1000);
                refreshTimer.Enabled = true;
                refreshTimer.Elapsed += RefreshDbs;
            }
        }

        void RefreshDbs(object sender, System.Timers.ElapsedEventArgs e)
        {
            //refresh Dbs in the background
            SafeThread refreshThread = new SafeThread(delegate()
            {
                logger.Log("Homestore refresh was triggered");
                
                //refresh into this temporary db
                HomeStoreDb tmpStoreDb = new HomeStoreDb(logger);
                tmpStoreDb.Populate();

                //do a switch now
                lock (this)
                {
                    storeDb = tmpStoreDb;
                }

                lastRefreshed = DateTime.Now;
            }, "homestore refresh", logger);

            refreshThread.Start();
        }

        public HomeStoreApp GetHomeStoreAppByName(string appName)
        {
            lock (this)
            {
                foreach (string name in storeDb.moduleDb.Keys)
                {
                    if (name.Equals(appName))
                        return storeDb.moduleDb[name];
                }
            }

            return null;
        }

        public List<HomeStoreApp> GetAllModules()
        {
            lock (this)
            {
                return storeDb.moduleDb.Values.ToList();
            }
        }

        public List<HomeStoreScout> GetAllScouts()
        {
            lock (this)
            {
                return storeDb.scoutDb.Values.ToList();
            }
        }

        public HomeStoreScout GetScout(string scoutName)
        {
            lock (this)
            {
                if (storeDb.scoutDb.ContainsKey(scoutName))
                    return storeDb.scoutDb[scoutName];

                return null;
            }
        }

    }
}
