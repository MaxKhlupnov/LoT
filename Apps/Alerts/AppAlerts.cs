using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn;
using System.Runtime.Serialization;
using System.Net.Mail;
using System.IO;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform;
using HomeOS.Hub.Common.Bolt.DataStore;

namespace HomeOS.Hub.Apps.Alerts
{
    public enum AlertMode { none, emailonly, smsonly, both };

    public class AlertSettings
    {
        public AlertMode Mode { get; set; }
        public int StartHourMin { get; set; }
        public int EndHourMin { get; set; }

        public int SuppressSeconds { get; set; }

        public string UserName { get; set; }

        public override string ToString()
        {
            return Mode + " " + StartHourMin + "-" + EndHourMin + " " + SuppressSeconds;
        }
        
        public string emailAddress { get; set; }
    }

    //[DataContract]
    public class Alert
    {
        //[DataMember]
        public DateTime TimeTriggered;

        //[DataMember]
        public string SensorFriendlyName;

        //[DataMember]
        public string SensorLocation;

        //[DataMember]
        public Byte Value;
        
        //[DataMember]
        public bool Acknowledged;

        public bool Equals(Alert other)
        {
            return (SensorFriendlyName.Equals(other.SensorFriendlyName) &&
                    SensorLocation.Equals(other.SensorLocation) &&
                    Value == other.Value);
        }

        public override string ToString()
        {
            return String.Format("{0}: {1} [{2}] triggered [new value = {3}]", TimeTriggered.ToString(), SensorFriendlyName, SensorLocation, Value);
        }

        public string FriendlyToString()
        {
            return String.Format("{0} {1}: {2} triggered, Location: {3}", TimeTriggered.ToShortDateString(), TimeTriggered.ToShortTimeString(), SensorFriendlyName, SensorLocation);

        }
    }

    
    [AddIn("HomeOS.Hub.Apps.Alerts")]
    public class DoorNotifier : Common.ModuleBase
    {        
        Dictionary<VPort, VCapability> cameraPorts = new Dictionary<VPort,VCapability>();

        Dictionary<VPort, VCapability> registeredSensors = new Dictionary<VPort, VCapability>();
        
        AlertSettings settings;

        private SafeServiceHost serviceHost;

        const int MaxAlertHistory = 1000;
        SortedList<DateTime, Alert> alertHistory = new SortedList<DateTime, Alert>();

        WebFileServer webUiServer;
        
        //DataStream for writing the alert pictures and text.
        IStream picStream, textStream;
        private Object picStreamLock = new Object();
        private Object textStreamLock = new Object();

        ////Email address to receive the alert pictures.
        //string emailAdrs;

        public override void Start()
        {
            logger.Log("Started: {0}", ToString());

            try
            {
                settings = new AlertSettings();


                settings.Mode = (moduleInfo.Args().Length > 0) ?
                                    (AlertMode)Enum.Parse(typeof(AlertMode), moduleInfo.Args()[0], true) :
                                    AlertMode.emailonly;

                settings.StartHourMin = (moduleInfo.Args().Length > 1) ? int.Parse(moduleInfo.Args()[1]) : 0;

                settings.EndHourMin = (moduleInfo.Args().Length > 2) ? int.Parse(moduleInfo.Args()[2]) : 2400;

                settings.SuppressSeconds = (moduleInfo.Args().Length > 3) ? int.Parse(moduleInfo.Args()[3]) : 5;  //AJB shorten suppression

                settings.UserName = (moduleInfo.Args().Length > 4) ? moduleInfo.Args()[4] : "user";

                settings.emailAddress = GetPrivateConfSetting("NotificationEmail");

            }
            catch (Exception exception)
            {
                logger.Log("{0}: error parsing arguments: {1}", exception.ToString(), String.Join(" ", moduleInfo.Args()));
            }

            picStream = base.CreateFileDataStream<StrKey, ByteValue>("H2OAlertsPics", true, 0);
            textStream = base.CreateValueDataStream<StrKey, StrValue>("H2OAlertsText", true, 0);

            DoorNotifierSvc service = new DoorNotifierSvc(logger, this);

            //serviceHost = DoorNotifierSvc.CreateServiceHost(
            //    service,
            //    new Uri(moduleInfo.BaseURL()+"/webapp"));

            serviceHost = DoorNotifierSvc.CreateServiceHost(logger, this, service, moduleInfo.BaseURL() + "/webapp");

            serviceHost.Open();

            webUiServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);

            logger.Log("{0}: service is open for business at {1}", ToString(), moduleInfo.BaseURL());

            //no services are exported by this application

            //..... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();

            if (allPortsList != null)
            {
                foreach (VPort port in allPortsList)
                {
                    PortRegistered(port);
                }
            }

            //insert a fake notification for testing
            Alert newAlert = new Alert()
            {
                TimeTriggered = DateTime.Now,
                SensorFriendlyName = "fake sensor",
                SensorLocation = "fake location",
                Value = 1,
                Acknowledged = false,
            };

            InsertAlert(newAlert);
        }

        private bool IsAlertTime()
        {
            DateTime currentTime = DateTime.Now;

            int currentHourMin = currentTime.Hour * 100 + currentTime.Minute;

            if (settings.EndHourMin > settings.StartHourMin)
            {
                return settings.StartHourMin <= currentHourMin && currentHourMin <= settings.EndHourMin;
            }
            else
            {
                if (currentHourMin <= 2400)
                    return (settings.StartHourMin <= currentHourMin);
                else
                    return (currentHourMin <= settings.EndHourMin);
            }
        }

        private bool SuppressAlert(Alert newAlert)
        {
            lock (alertHistory)
            {
                int count = alertHistory.Count;

                DateTime currTime = DateTime.Now;

                for (int index = count - 1; index >= 0; index--)
                {
                    KeyValuePair<DateTime,Alert> kvPair = alertHistory.ElementAt(index);

                    TimeSpan timeDiff = currTime - kvPair.Key;

                    //if we've gone far back into history, lets stop
                    if (timeDiff.TotalSeconds > settings.SuppressSeconds)
                    {
                        return false;
                    }

                    //we found an equivalent notification
                    if (kvPair.Value.Equals(newAlert))
                        return true;
                }
            }

            return false;
        }

        private void InsertAlert(Alert alert)
        {
            lock (alertHistory)
            {
                if (alertHistory.Count == MaxAlertHistory)
                    alertHistory.RemoveAt(0);

                //take care of duplicates
                while (alertHistory.ContainsKey(alert.TimeTriggered))
                    alert.TimeTriggered += TimeSpan.FromMilliseconds(1);

                alertHistory.Add(alert.TimeTriggered, alert);
            }
        }

        public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        {
            if (retVals.Count >= 1)
            {
                byte val = (byte) (int) retVals[0].Value();

                //hack for techfest since we are using a multi-level switch as a doorbell
                //if (RoleSwitchMultiLevel.RoleName.Equals(roleName, StringComparison.CurrentCultureIgnoreCase))
                //    val = 0;

                Alert newAlert = new Alert() { TimeTriggered = DateTime.Now, 
                                                                 SensorFriendlyName = senderPort.GetInfo().GetFriendlyName(),  
                                                                 SensorLocation = senderPort.GetInfo().GetLocation().Name(),
                                                                 Value = val,
                                                                 Acknowledged = false, };

                bool notify = //settings.Mode != AlertMode.none && 
                              IsAlertTime() &&
                              !SuppressAlert(newAlert) &&
                              ((RoleSwitchMultiLevel.RoleName.Equals(roleName, StringComparison.CurrentCultureIgnoreCase) && (val == 99 || val == 0)) ||
                               (RoleSensor.RoleName.Equals(roleName, StringComparison.CurrentCultureIgnoreCase) && val == 255));

                logger.Log("{0}: got notified by {1} [{2}] val = {3} notify = {4}\n",
                           this.ToString(), newAlert.SensorFriendlyName, roleName, val.ToString(), notify.ToString());

                if (notify)
                {
                    InsertAlert(newAlert);
                    GenerateMessage(newAlert);  
                }
            }
            else
            {
                logger.Log("{0}: got unexpected retvals [{1}] from {2}", ToString(), retVals.Count.ToString(), senderPort.ToString());
            }
        }

        private void GenerateMessage(Alert newAlert)
        {
            List<Attachment> attachmentList = new List<Attachment>();

            string linkMessage = String.Format("Go to Lab of Things Alerts application to see list of alerts.");

            string subject = "Alert";
            string message = String.Format("Dear {0} - \n\n{1}.\n\n{2}\n\nCheers.\n",
                                            settings.UserName,
                                            newAlert.FriendlyToString(),
                                            linkMessage
                                            );
            string alertTxtName = "Alert-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            AddTxtDataStream(alertTxtName,newAlert.ToString());

            foreach (VPort cameraPort in cameraPorts.Keys)
            {
                VCapability capability = cameraPorts[cameraPort];

                IList<VParamType> retVals = cameraPort.Invoke(RoleCamera.RoleName, RoleCamera.OpGetImageName, new List<VParamType>(),
                                                                   ControlPort, capability, ControlPortCapability);

                if (retVals[0].Maintype() != (int)ParamType.SimpleType.error)
                {
                    string cameraFriendlyName = cameraPort.GetInfo().GetFriendlyName();
                    logger.Log("{0} got image from {1}", this.ToString(), cameraFriendlyName);

                    if (retVals.Count >= 1 && retVals[0].Value() != null)
                    {
                        byte[] imageBytes = (byte[])retVals[0].Value();
                        string mimeType = "image/jpeg";

                        Attachment attachment = new Attachment(new MemoryStream(imageBytes), cameraFriendlyName + "." + "jpg", mimeType);
                        attachmentList.Add(attachment);

                        string alertImgName = "WaterAlertImg-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                        AddPicDataStream(alertImgName,imageBytes);
                    }
                    else
                    {
                        logger.Log("{0} got null image", this.ToString());
                    }
                }
            }

            if (settings.Mode == AlertMode.emailonly || settings.Mode == AlertMode.both)
                SendEmail(subject, message, attachmentList);

            if (settings.Mode == AlertMode.smsonly || settings.Mode == AlertMode.both)
                SendSms(subject, message);
        }

        private void AddTxtDataStream(string key, string message)
        {
            StrKey strKey = new StrKey(key);
            StrValue strVal = new StrValue(message);
            try
            {
                lock (textStreamLock)
                {
                    textStream.Append(strKey, strVal);
                    logger.Log("WaterAlert message has been written to {0}.", textStream.Get(strKey).ToString());
                }
            }
            catch (Exception e)
            {
                logger.Log("Error while writing text to file stream: {0}", e.ToString());
            }
        }

        private void AddPicDataStream(string key, byte[] imageBytes)
        {
            StrKey strKey = new StrKey(key);
            ByteValue byteVal = new ByteValue(imageBytes);
            try
            {
                lock (picStreamLock)
                {
                    picStream.Append(strKey, byteVal);
                }
            }
            catch (Exception e)
            {
                logger.Log("Error while writing images to dir stream: {0}", e.ToString());
            }
        }

        internal void UpdateEmail(string email)
        {
            settings.emailAddress = email;
        }

        void SendEmail(string subject, string message, List<Attachment> attachmentList)
        {           
            Tuple<bool,string> result = base.SendEmail(settings.emailAddress, subject, message, attachmentList);

            if (result.Item1)
            {
                logger.Log("Email notification succeeded");
            }
            else
            {
                logger.Log("Email notification failed with error:{0}", result.Item2);
            }
        }


        void SendSms(string subject, string message)
        {
            Tuple<bool, string> result = base.SendEmail(settings.emailAddress, subject, message, null);

            if (result.Item1)
            {
                logger.Log("SMS notification succeeded");
            }
            else
            {
                logger.Log("SMS notification failed with error:{0}", result.Item2);
            }
        }

             
        public override void Stop()
        {
            if (serviceHost != null)
                serviceHost.Abort();

            try
            {
                textStream.Close();
                picStream.Close();
            }
            catch (Exception e) {
                logger.Log("{0}: error: {1}", e.ToString());
            }
        }

        public void WindowClosed()
        {
        }

        public void UpdateSettings(AlertSettings settings)
        {
            this.settings = settings;

        }

        public override void PortRegistered(VPort port)
        {
            lock (this)
            {
                if (Role.ContainsRole(port, RoleCamera.RoleName))
                {
                    VCapability capability = GetCapability(port, Constants.UserSystem);

                    if (cameraPorts.ContainsKey(port))
                        cameraPorts[port] = capability;
                    else
                        cameraPorts.Add(port, capability);
                    
                    logger.Log("{0} added camera port {1}", this.ToString(), port.ToString());
                }

                //lets not monitor switches
                //if (Role.ContainsRole(port, RoleSwitchMultiLevel.RoleName))
                //{
                //    if (port.GetInfo().GetFriendlyName().Equals(switchFriendlyName, StringComparison.CurrentCultureIgnoreCase))
                //    {
                //        switchPort = port;
                //        switchPortCapability = GetCapability(port, Constants.UserSystem);

                //        if (switchPortCapability != null)
                //        {
                //            switchPort.Subscribe(RoleSwitchMultiLevel.RoleName, RoleSwitchMultiLevel.OpGetName,
                //                this.ControlPort, switchPortCapability, this.ControlPortCapability);
                //        }
                //    }
                //}

                if (Role.ContainsRole(port, RoleSensor.RoleName))
                {
                    //if (port.GetInfo().GetFriendlyName().Equals(sensorFriendlyName, StringComparison.CurrentCultureIgnoreCase))
                    //{
                    //    sensorPort = port;
                    VCapability capability = GetCapability(port, Constants.UserSystem);

                    if (registeredSensors.ContainsKey(port))
                        registeredSensors[port] = capability;
                    else
                        registeredSensors.Add(port, capability);

                    if (capability != null)
                    {
                        port.Subscribe(RoleSensor.RoleName, RoleSensor.OpGetName,
                               this.ControlPort, capability, this.ControlPortCapability);
                    }
                    //}
                }

            }
        }

        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (Role.ContainsRole(port, RoleCamera.RoleName))
                {
                    if (cameraPorts.ContainsKey(port))
                    {
                        cameraPorts.Remove(port);
                        logger.Log("{0} removed camera port {1}", this.ToString(), port.ToString());

                    }
                }


                if (Role.ContainsRole(port, RoleSensor.RoleName))
                {
                    if (registeredSensors.ContainsKey(port))
                    {
                        registeredSensors.Remove(port);
                        logger.Log("{0} removed sensor port {1}", this.ToString(), port.ToString());
                    }
                }
            }
        }

        internal AlertSettings GetSettings()
        {
            return settings;
        }


        internal List<Alert> GetMostRecentAlerts(int numAlerts)
        {
            int endIndex = alertHistory.Count - 1;

            int startIndex = alertHistory.Count - numAlerts;

            return GetAlertsWithIndices(startIndex, endIndex);
        }

        internal List<Alert> GetOlderAlerts(DateTime dateTime, int numAlerts)
        {
            int index = alertHistory.IndexOfKey(dateTime);
            if (index < 0)
                return new List<Alert>();

            return GetAlertsWithIndices(index - numAlerts, index - 1);
        }

        internal List<Alert> GetNewerAlerts(DateTime dateTime, int numAlerts)
        {
            int index = alertHistory.IndexOfKey(dateTime);
            if (index < 0)
                return new List<Alert>();

            return GetAlertsWithIndices(index + 1, index + numAlerts);
        }

        //the notifications are returned in the opposite order
        //the newer ones have lower indices
        private List<Alert> GetAlertsWithIndices(int startIndex, int endIndex)
        {
            List<Alert> retList = new List<Alert>();

            lock (this)
            {
                for (int index = endIndex; index >= startIndex; index--)
                {
                    if (index >= 0 && index < alertHistory.Count)
                        retList.Add(alertHistory.ElementAt(index).Value);
                }
            }

            return retList;

        }

        internal void SetAcknowledgment(DateTime time, bool acknowledged)
        {
            lock (this)
            {
                //check if we still have this notification
                if (alertHistory.ContainsKey(time))
                {
                    alertHistory[time].Acknowledged = acknowledged;
                }
            }
        }

        internal List<string> GetMonitoredDevices()
        {
            var retList = new List<string>();

            lock (this)
            {
                foreach (var sensor in registeredSensors.Keys)
                {
                    retList.Add(sensor.GetInfo().GetFriendlyName());
                }

            }

            return retList;
        }

        internal string GetEmail()
        {
            return settings.emailAddress;
        }
    }
}