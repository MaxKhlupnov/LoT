using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common.Bolt.DataStore;
using System.IO;
using System.Timers;
using System.Net.Mail;

namespace HomeOS.Hub.Apps.Sensor
{

    /// <summary>
    /// Logging module that logs data reported by sensors to UI and to data store
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Apps.Sensor")]
    public class Sensor : ModuleBase
    {
        //list of accessible sensor ports in the system
        List<VPort> accessibleSensorPorts;

        private SafeServiceHost serviceHost;

        private WebFileServer appServer;

        Queue<string> receivedMessageList;

        //Variables used to write to local file
        IStream datastream;
        string localDataDirectoryPath = ""; //Used to write data to local file - if directory is a OneDrive directory than it will be sync'd by OneDrive which is handy
        Boolean writingToLocalFile = false;

        int gNumStringstoShow = 100;


        //Variables used for monitoring
        public class MonitorDataVal
        {
            public DateTime? timeSampled;
            public string dataVal;
            public bool isMonitored;
            public int? maxMinutesBetweenSamples;
            public double? maxValue;
            public double? minValue;

            public MonitorDataVal(string dVal)
            {
                timeSampled = DateTime.Now;
                dataVal = dVal;
                isMonitored = false;
                maxMinutesBetweenSamples = null;
                maxValue = null;
                minValue = null;
            }
            public MonitorDataVal(bool isMon, int maxMin){
                isMonitored = isMon;
                maxMinutesBetweenSamples = maxMin;
                timeSampled = null;
                dataVal = "";
            }
            public void UpdateDataVal(string dVal)
            {
                timeSampled = DateTime.Now;
                dataVal = dVal;
            }
            public void UpdateMonitorVals(bool isM, int minBetween, double? maxV, double? minV)
            {
                isMonitored = isM;
                maxMinutesBetweenSamples = minBetween;
                maxValue = maxV;
                minValue = minV;
            }
 

        }
        Dictionary<string, MonitorDataVal> monitorDataValues = new Dictionary<string, MonitorDataVal>();
        Timer mTimer;
        string gAlertEmailAddress = "";
        string gHome_Id;
        private string gMonitoringMemoryFile;


        public override void Start()
        {
            //Using "Sensor:" to indicate clearly in the log where this line came from.
            logger.Log("Sensor:Started: {0} ", ToString());

            // remoteSync flag can be set to true, if the Platform Settings has the Cloud storage
            // information i.e., DataStoreAccountName, DataStoreAccountKey values
            datastream = base.CreateValueDataStream<StrKey, StrValue>("data", true, 10 * 60);

            SensorService sensorService = new SensorService(logger, this);
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
            mTimer = new Timer(60000); //1 minute
            mTimer.Elapsed += new ElapsedEventHandler(doDataMonitoring);

            gAlertEmailAddress = GetPrivateConfSetting("NotificationEmail");
            gHome_Id = GetConfSetting("HomeId");

            //location to save information
            Directory.CreateDirectory(moduleInfo.WorkingDir());

            gMonitoringMemoryFile = moduleInfo.WorkingDir() + "\\" + "monitoringMemory.txt";

            CheckForPreviousMonitoringValues();

        }


        public override void Stop()
        {
            logger.Log("Sensor:clean up");
            if (datastream != null)
                datastream.Close();

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


        public void WriteToStream(string tag, string data)
        {

            StrKey key = new StrKey(tag);
            if (datastream != null)
            {
                datastream.Append(key, new StrValue(data));
                }
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

                try
                {
                    //Write to the stream
                    WriteToStream(sensorTag, sensorData);
                }
                catch (Exception exp)
                {
                    logger.Log("Sensor:{0}: WriteToStream failed. Exception caught.");
                    logger.Log(exp.ToString());
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

            //record latest data in our monitoring dictionary
            if (monitorDataValues.ContainsKey(sensorTag))
            {
                (monitorDataValues[sensorTag]).UpdateDataVal(sensorData);
            }
            else
            {
                monitorDataValues.Add(sensorTag, new MonitorDataVal(sensorData));
            }
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

        #region monitoring

        public List<string> GetMonitoringInfo()
        {
            List<string> retVal = new List<string>();

            retVal.Add(""); //signal no error

            foreach (KeyValuePair<string, MonitorDataVal> sensorData in monitorDataValues)
            {
                //each entry is 5 elements: name of sensor, isMonitored, maxMinutesBetweenSamples, maxValue, minValue
                retVal.Add(sensorData.Key);
                retVal.Add(sensorData.Value.isMonitored.ToString());
                if (!sensorData.Value.isMonitored)
                {
                    //all other values are junk  if not monitoring
                    retVal.Add("");  //maxMinutes
                    retVal.Add(""); //maxValue
                    retVal.Add(""); //minValue
                }
                else
                { //some of the values are valid
                    if (sensorData.Value.maxMinutesBetweenSamples.HasValue)
                        retVal.Add(sensorData.Value.maxMinutesBetweenSamples.ToString());
                    else
                        retVal.Add("");
                    if (sensorData.Value.maxValue.HasValue)
                        retVal.Add(sensorData.Value.maxValue.ToString());
                    else
                        retVal.Add("");
                    if (sensorData.Value.minValue.HasValue)
                        retVal.Add(sensorData.Value.minValue.ToString());
                    else
                        retVal.Add("");
                }
            }
            return retVal;
        }

        public List<string> SetMonitoringInfo(string sensorTag, bool isMonitoring, int maxMinutesBetweenUpdate, int? maxValue, int? minValue)
        {
            List<string> retVal = new List<string>();
            retVal.Add("");
            string monitoringInfo;

            //Setup the monitoring information - if there is an error return error string


            if (monitorDataValues.ContainsKey(sensorTag))
            {
                (monitorDataValues[sensorTag]).UpdateMonitorVals(isMonitoring, maxMinutesBetweenUpdate, maxValue, minValue);
                if (isMonitoring)
                {
                    monitoringInfo = sensorTag + "]" + maxMinutesBetweenUpdate;
                    SaveMonitoringInfo(monitoringInfo);
                }
            }

            //enable the timer
            if ((!mTimer.Enabled) && isMonitoring)
                mTimer.Enabled = true;

            return retVal;

        }

        public List<string> StopMonitoring()
        {
            List<string> retVal = new List<string>();
            //stop the Timer          
            mTimer.Enabled = false;

            //turn off all monitoring
            foreach (KeyValuePair<string, MonitorDataVal> sensorData in monitorDataValues)
            {
                sensorData.Value.isMonitored = false;
            }

            //TODO: delete anything that has been saved.
            lock (this)
            {
                if (File.Exists(gMonitoringMemoryFile))
                    File.Delete(gMonitoringMemoryFile);
            }

            retVal.Add("");

            return retVal;
        }

        private void SaveMonitoringInfo(string monitoringInfo)
        {
            lock (this)
            {
                //Make sure monitoring file exists
                if (!(File.Exists(gMonitoringMemoryFile)))
                {
                    System.IO.FileStream f = File.Create(gMonitoringMemoryFile);
                    f.Close();
                }
            
                TextWriter tw = new StreamWriter(gMonitoringMemoryFile, true);
                tw.WriteLine(monitoringInfo);
                tw.Close();
            }
        }


        private void CheckForPreviousMonitoringValues()
        {
            //Read the monitoring values in from memory
            
            lock (this)
            {
                if (File.Exists(gMonitoringMemoryFile))
                {
                    string[] monitorData;
                    TextReader tw = new StreamReader(gMonitoringMemoryFile);
                    string currLine = tw.ReadLine();
                    while (currLine !=null) {
                        //put the line into the monitoring dictionary
                        logger.Log("Sensor: found monitoring info:" + currLine);
                       
                        monitorData = currLine.Split(']');
                        if (monitorData.Length == 2) //we expect 2 objects
                        {
                            //record latest data in our monitoring dictionary
                            if (monitorDataValues.ContainsKey(monitorData[0]))
                            {
                                (monitorDataValues[monitorData[0]]).UpdateDataVal(monitorData[1]);
                            }
                            else
                            {
                                try
                                {
                                    int i = Convert.ToInt32(monitorData[1]);
                                    monitorDataValues.Add(monitorData[0], new MonitorDataVal(true, i));
                                }
                                catch (Exception e)
                                {
                                    logger.Log("Sensor: Badly formatted monitoring information");

                                }
                            }
                        }
                        currLine = tw.ReadLine();
                    }

                     tw.Close();
                    
                    //start the monitoring timer
                     mTimer.Enabled = true;
                }       
            }  
        }


        private void doDataMonitoring(object sender, ElapsedEventArgs e)
        {

            logger.Log("Sensor:Do Monitoring called");
            List<Attachment> attachmentList = new List<Attachment>(); //necessary for sending email. Leave empty for now.
            string emailSubject;
            string emailMsg;

            //called regularly, loop through monitoring information, issue alerts
            foreach (KeyValuePair<string, MonitorDataVal> sensorData in monitorDataValues)
            {
                if (sensorData.Value.isMonitored  && sensorData.Value.timeSampled.HasValue)
                {

                    if (sensorData.Value.maxMinutesBetweenSamples.HasValue)
                    {
                        logger.Log(sensorData.Key.ToString() + " Sampled: " + sensorData.Value.timeSampled + " Max Minutes: " + sensorData.Value.maxMinutesBetweenSamples);
                        if ((DateTime.Now.Subtract((DateTime)sensorData.Value.timeSampled)).Minutes > sensorData.Value.maxMinutesBetweenSamples)
                        {
                            emailSubject = gHome_Id + ":" + sensorData.Key.ToString() + "stale data";
                            emailMsg = "On Hub: " + gHome_Id + "," + sensorData.Key.ToString() + " no data for more than " + sensorData.Value.maxMinutesBetweenSamples.ToString() + " minutes.";
                            logger.Log("Sensor:" + sensorData.Key.ToString() + " is stale, sending email alert to" + gAlertEmailAddress);
                            Tuple<bool, string> result = base.SendEmail(gAlertEmailAddress, emailSubject, emailMsg, attachmentList);

                            if (result.Item1)
                            {
                                logger.Log("Sensor: Data Monitoring notification succeeded");
                            }
                            else
                            {
                                logger.Log("Sensor: Email notification failed with error:{0}", result.Item2);
                            }
                        }

                    }
                }

            }

        }
    }
        #endregion
}
