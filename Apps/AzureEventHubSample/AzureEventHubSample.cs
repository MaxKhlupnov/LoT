using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.IO;
using System.Timers;
using System.Net.Mail;
using System.Configuration;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using HomeOS.Hub.Apps.DigitalMedia;


namespace HomeOS.Hub.Apps.AzureEventHubSample
{



    /// <summary>
    /// This sample app demonstrates how to use Azure Event Hub and Stream Analytics features to stream data
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Apps.AzureEventHubSample")]
    public class SensorApp : ModuleBase
    {

        //####################EVENT HUB related code####################################

        static string connectionString = GetServiceBusConnectionString();
        NamespaceManager namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
        static string eventHubName = ConfigurationManager.AppSettings["EventHubName"];

        Sender sender = new Sender(eventHubName);

        //###############################################################################
        

        //list of accessible sensor ports in the system
        List<VPort> accessibleSensorPorts;

        private SafeServiceHost serviceHost;

        private WebFileServer appServer;

        Queue<string> receivedMessageList;


        string localDataDirectoryPath = ""; //Used to write data to local file - if directory is a OneDrive directory than it will be sync'd by OneDrive which is handy
        Boolean writingToLocalFile = false;

        int gNumStringstoShow = 100;    
  
        string gHome_Id;
       


        public override void Start()
        {
            //Using "Sensor:" to indicate clearly in the log where this line came from.
            logger.Log("Sensor:Started: {0} ", ToString());


            SensorAppService sensorService = new SensorAppService(logger, this);
            serviceHost = new SafeServiceHost(logger, typeof(IDigitalMediaContract), sensorService, this, Constants.AjaxSuffix, moduleInfo.BaseURL());
            serviceHost.Open();

            appServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);


            //........... instantiate the list of other ports that we are interested in
            accessibleSensorPorts = new List<VPort>();

            //..... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();

            if (allPortsList != null)
                ProcessAllPortsList(allPortsList);

            this.receivedMessageList = new Queue<string>();
             

            //gAlertEmailAddress = GetPrivateConfSetting("NotificationEmail");
            gHome_Id = GetConfSetting("HomeId");

            //location to save information
            Directory.CreateDirectory(moduleInfo.WorkingDir());
                  
 
        }


        public override void Stop()
        {
            logger.Log("Sensor:clean up");

            if (serviceHost != null)
                serviceHost.Close();

            if (appServer != null)
                appServer.Dispose();
        }

        #region Writing application data to local file system

        public void ConfigureWritingToLocalFile(Boolean wToFile, string path)
        {

            //turn on and off writing to local file.
            this.writingToLocalFile = wToFile;

            if (this.writingToLocalFile)
            {
                //make sure the file path is setup and data directory exists
                Boolean success = SetupLocalDataDir(path);
                if (!success) //if the directory isn't setup correctly then don't write to file (even if user said we should)
                {
                    this.writingToLocalFile = false;
                    logger.Log("Sensor: Problem with directory: {0}, can't write to local file {0} ", path);
                }
            }
        }


        private Boolean SetupLocalDataDir(string path)
        {

            DirectoryInfo di_home;
            string study_id = GetConfSetting("StudyId");

            //TODO: Make sure that path ends in "\";
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            this.localDataDirectoryPath = path + study_id + "\\" + gHome_Id + "\\";

            try
            {
                if (!Directory.Exists(localDataDirectoryPath))
                {
                    di_home = Directory.CreateDirectory(localDataDirectoryPath);
                }
                return true;  //directory exists
            }
            catch (Exception e)
            {
                logger.Log("Sensor: Can't create local data storage directory: {0}. Exception: {1}", localDataDirectoryPath, e.ToString());
                return false;
            }

        }

        private string GetFileHourExtension()
        {
            var now = DateTime.Now;
            return string.Format("{0:D4}-{1:D2}-{2:D2}-{3:D2}", now.Year, now.Month, now.Day, now.Hour);
        }

        public void WriteToLocalFile(string data)
        {
            string fname = this.localDataDirectoryPath + "sensor_" + GetFileHourExtension() + ".txt";
            try
            {
                if (!File.Exists(fname))
                {
                    System.IO.FileStream f = File.Create(fname);
                    f.Close();
                }

                TextWriter tw = new StreamWriter(fname, true);
                tw.WriteLine(data);
                tw.Close();
            }
            catch (Exception e)
            {

                logger.Log("something wrong with writing to file" + e.ToString());
            }

        }

        public string GetLocalDirectory()
        {

            return this.localDataDirectoryPath;
        }
        public bool GetIsSyncToLocal()
        {
            return this.writingToLocalFile;
        }

        #endregion

  

        public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        {
            string message;
            string sensorData;
            string sensorTag = senderPort.GetInfo().GetFriendlyName() + roleName;

            lock (this)
            {
                if (roleName.Contains(RoleSensor.RoleName) && opName.Equals(RoleSensor.OpGetName))
                {
                    byte rcvdNum = (byte)(int)retVals[0].Value();
                    sensorData = rcvdNum.ToString();

                }
                else if (roleName.Contains(RoleSensorMultiLevel.RoleName) && opName.Equals(RoleSensorMultiLevel.OpGetName))
                {
                    double rcvdNum = (double)retVals[0].Value();
                    sensorData = rcvdNum.ToString();
                }
                else
                {
                    sensorData = String.Format("Sensor: Invalid role->op {0}->{1} from {2}", roleName, opName, sensorTag);
                }

                ///////////////Write to Azure Event Hub 
                bool bIsEventSent = sender.SendEvents(gHome_Id, DateTime.Now, senderPort.GetInfo().GetFriendlyName(), roleName, sensorData);

                if (false == bIsEventSent)
                {
                    logger.Log("*****Sending data to event hub failed****");
                    logger.Log("[Log Data Offline]{0}|{1}|{2}|{3}|{4}", gHome_Id, DateTime.Now.ToString(), gHome_Id, DateTime.Now.ToString(), senderPort.GetInfo().GetFriendlyName(), roleName, sensorData, roleName, sensorData);

                }
                 
               }

            //Create local list of alerts for display
            message = String.Format("{0}\t{1}\t{2}", DateTime.Now, sensorTag, sensorData);
            this.receivedMessageList.Enqueue(message);
            if (this.receivedMessageList.Count > gNumStringstoShow)
                this.receivedMessageList.Dequeue();
            logger.Log("Sensor\t{0}", message);

            //if applicable write the information to a local file.
            if (this.writingToLocalFile)
                WriteToLocalFile(message);

 
        }


        private void ProcessAllPortsList(IList<VPort> portList)
        {
            foreach (VPort port in portList)
            {
                PortRegistered(port);
            }
        }


        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        public override void PortRegistered(VPort port)
        {

            logger.Log("{0} got registeration notification for {1}", ToString(), port.ToString());

            lock (this)
            {
                if (!accessibleSensorPorts.Contains(port) &&
                    Role.ContainsRole(port, RoleSignalDigital.RoleName) &&
                    GetCapabilityFromPlatform(port) != null)
                {
                    accessibleSensorPorts.Add(port);

                    logger.Log("{0} added port {1}", this.ToString(), port.ToString());

                    this.Subscribe(port, RoleSignalDigital.Instance, RoleSignalDigital.OpSetDigitalName);
                    this.Subscribe(port, RoleSignalDigital.Instance, RoleSignalDigital.OnConnectEvent);
                    this.Subscribe(port, RoleSignalDigital.Instance, RoleSignalDigital.OnDigitalEvent);
                    this.Subscribe(port, RoleSignalDigital.Instance, RoleSignalDigital.OnDisconnectEvent);
                    this.Subscribe(port, RoleSignalDigital.Instance, RoleSignalDigital.OnErrorEvent);


                }
            }
        }

 /*       /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        public override void PortRegistered(VPort port)
        {

            logger.Log("Sensor:{0} got registeration notification for {1}", ToString(), port.ToString());

            lock (this)
            {
                if (!accessibleSensorPorts.Contains(port) &&
                    GetCapabilityFromPlatform(port) != null &&
                    (Role.ContainsRole(port, RoleSensor.RoleName) || Role.ContainsRole(port, RoleSensorMultiLevel.RoleName)))
                {
                    accessibleSensorPorts.Add(port);

                    logger.Log("Sensor:{0} added port {1}", this.ToString(), port.ToString());

                    if (Role.ContainsRole(port, RoleSensor.RoleName))
                    {
                        if (Subscribe(port, RoleSensor.Instance, RoleSensor.OpGetName))
                            logger.Log("Sensor:{0} subscribed to sensor port {1}", this.ToString(), port.ToString());
                        else
                            logger.Log("Sensor:{0} failed to subscribe to sensor  port {1}", this.ToString(), port.ToString());
                    }

                    if (Role.ContainsRole(port, RoleSensorMultiLevel.RoleName))
                    {
                        if (Subscribe(port, RoleSensorMultiLevel.Instance, RoleSensorMultiLevel.OpGetName))
                            logger.Log("Sensor:{0} subscribed to multi-level sensor port {1}", this.ToString(), port.ToString());
                        else
                            logger.Log("Sensor: {0} failed to subscribe to multi-level port {1}", this.ToString(), port.ToString());
                    }

                }
            }
        }*/

        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (accessibleSensorPorts.Contains(port))
                {
                    accessibleSensorPorts.Remove(port);
                    logger.Log("Sensor:{0} deregistered port {1}", this.ToString(), port.GetInfo().ModuleFacingName());
                }
            }
        }

        public List<string> GetReceivedMessages()
        {

            List<string> retList;
            //Length will always be below the max.
            retList = this.receivedMessageList.ToList<string>();
            //newest displayed at the top
            retList.Reverse();
            return retList;
        }

/*        public List<string> GetReceivedMessages()
        {
            List<string> retList = new List<string>(this.receivedMessageList);
            retList.Reverse();
            return retList;
        }*/

        private static string GetServiceBusConnectionString()
        {
            string connectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Did not find Service Bus connections string in appsettings (app.config)");
                return string.Empty;
            }
            ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder(connectionString);
            builder.TransportType = TransportType.Amqp;
            return builder.ToString();
        }

  


        public List<string> SetDigitalSignal(int slot, int joint, string value)
        {
            List<string> retList = new List<string>(accessibleSensorPorts.Capacity);
            foreach (VPort port in accessibleSensorPorts)
            {
                retList.Add(SetDigitalSignal(port, slot, joint, value));
            }
            return retList;
        }

        public string SetDigitalSignal(VPort port, int slot, int join, string value)
        {
            try
            {
                DateTime requestTime = DateTime.Now;

                ParamType[] args = new ParamType[] { new ParamType(slot), new ParamType(join), new ParamType(value) };

                var retVals = Invoke(port, RoleSignalDigital.Instance, RoleSignalDigital.OpSetDigitalName, args);

                double diffMs = (DateTime.Now - requestTime).TotalMilliseconds;


                if (retVals[0].Maintype() != (int)ParamType.SimpleType.error)
                {

                    bool rcvdNum = (bool)retVals[0].Value();

                    logger.Log(String.Format("set digital success to {0} after {1} ms. sent = slot:{2} join:{3} value:{4} rcvd = {5}", port, diffMs, slot, join, value, rcvdNum));
                }
                else
                {
                    logger.Log(String.Format("set digital failure to {0} after {1} ms. sent = slot:{2} join:{3} value:{4} error = {5}", port, diffMs, slot, join, value, retVals[0].Value()));
                }

                return retVals[0].Value().ToString();
            }
            catch (Exception e)
            {
                string ErrorMessage = String.Format("Error while calling echo request: {0}", e.ToString());
                logger.Log(ErrorMessage);
                return ErrorMessage;
            }
        }
    }
}
 