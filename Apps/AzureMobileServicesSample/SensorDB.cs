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
using Microsoft.WindowsAzure.MobileServices;

namespace HomeOS.Hub.Apps.AzureMobileServicesSample
{

    /// <summary>
    /// This sample app demonstrates how to use to one of the features in Azure Mobile Services to store sensor data in Azure Sql
    /// To learn more about Azure mobile services please see http://azure.microsoft.com/en-us/documentation/services/mobile-services/
    /// </summary>

    public class sensorapp //this should be the name of the table you created in Mobile Services
    {

        public string id { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "sensor_role")]
        public string sensor_role { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "sensor_data")]
        public float sensor_data { get; set; }
    }

    [System.AddIn.AddIn("HomeOS.Hub.Apps.AzureMobileServicesSample")]
    public class AzureMobileServicesSample : ModuleBase
    {
        
        //creating a mobile service client so we can connect to a sql azure db
        public static MobileServiceClient MobileService = new MobileServiceClient("https://yourmobileservice.azure-mobile.net/", "[your api key here]");
        
        //you can get the host name and key info in the azure management portal: 
        //https://manage.windowsazure.com/microsoft.onmicrosoft.com#Workspaces/MobileServicesExtension/apps/lotsensors/quickstart 
        //note if the hostname and key info are incorrect exceptions are thrown when you try to write data to db
 
 

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
           

            SensorDBService sensorService = new SensorDBService(logger, this);
            serviceHost = new SafeServiceHost(logger, typeof(ISensorContract), sensorService, this, Constants.AjaxSuffix, moduleInfo.BaseURL());
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

            //#########test to see if insert (to db) operation works###############
            //WriteToDB("_test_", 0);
            //#####################################################################
 
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

        async void WriteToDB(string tag, float data)
        {

            try
            {
                sensorapp sensorItem = new sensorapp { sensor_role = tag, sensor_data = data };
                IMobileServiceTable<sensorapp> table = MobileService.GetTable<sensorapp>();
                await table.InsertAsync(sensorItem);
                await table.UpdateAsync(sensorItem);

            }
            catch (Exception e)
            {
                logger.Log("[Database operation has failed] " + e.GetType().ToString() + " -> " + e.InnerException.ToString());
                logger.Log("[Logging data offline]"+ tag + "," + data );
            }

            //To Do: In the offline disconnected scenario, there's a way to write to a local sql db first and then sync to the online db 
            //destails on how to implement an offline sync here: 
            //http://azure.microsoft.com/en-us/documentation/articles/mobile-services-windows-store-dotnet-get-started-offline-data/

        }


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

                     //log to the azure sql db via azure mobile services
                    WriteToDB(sensorTag, Convert.ToSingle(sensorData));

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
        }

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
    }
}
 