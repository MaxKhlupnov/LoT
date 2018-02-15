
namespace HomeOS.Hub.Platform
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Diagnostics;
    using HomeOS.Hub.Platform.Views;
    using HomeOS.Shared;

    /// <summary>
    /// HeartbeatService periodically grabs some essential health stats for platform and the running
    /// modules, and posts to the cloud.
    /// </summary>
    public  class HeartbeatService : IDisposable
    {
        protected VLogger logger;
        protected Uri uri;
        protected UInt32 heartbeatIntervalMins; // in minutes
        protected TimerCallback tcb;
        protected Timer timer;
        protected PerformanceCounter perfCountPercentProcTime;
        protected PerformanceCounter perfCountWorkingSet;
        protected UInt32 sequenceNumber;
        protected bool disposed = false;
        protected Platform platform;

        public HeartbeatService(Platform platform, VLogger log)
        {
            this.platform = platform;
            this.logger = log;
            this.tcb = SendHeartbeat;
            this.sequenceNumber = 0;
            try
            {
                this.uri = new Uri("https://" + GetHeartbeatServiceHostString() + ":" + Constants.HeartbeatServiceSecurePort + "/" +
                                   Constants.HeartbeatServiceWcfListenerEndPointUrlSuffix);
                this.heartbeatIntervalMins = Settings.HeartbeatIntervalMins;
                if (this.heartbeatIntervalMins < Constants.MinHeartbeatIntervalInMins)
                    this.heartbeatIntervalMins = Constants.MinHeartbeatIntervalInMins;
                if (this.heartbeatIntervalMins > Constants.MaxHeartbeatIntervalInMins)
                    this.heartbeatIntervalMins = Constants.MaxHeartbeatIntervalInMins;

                this.perfCountPercentProcTime = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName, true);
                this.perfCountWorkingSet = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName, true);
            }
            catch (Exception e)
            {
                logger.Log("Platform failed failed to construct heartbeat service , Exception={0}", e.Message);
            }

        }

        private string GetHeartbeatServiceHostString()
        {
            string retString;
            switch(HomeOS.Hub.Platform.Settings.HeartbeatServiceMode)
            {
                case "Production":
                    retString = Settings.HeartbeatServiceHost;
                    break;
                case "Off":
                default:
                    retString = "";
                    break;
            }
            return retString;
        }

        public void Start()
        {
            try
            {
                this.timer = new Timer(tcb, null, 500, this.heartbeatIntervalMins * 60 * 1000);
            }
            catch(Exception e)
            {
                logger.Log("Platform failed failed to start heartbeat service, Exception={0}", e.Message); 
            }
        }

        public void Stop()
        {
            Dispose(true);
        }

        void webClient_UploadHeartbeatInfoCompleted(object sender, UploadStringCompletedEventArgs e)        
        {
            if (e.Error != null)
            {
                logger.Log("Heartbeat for {0} failed with Server error : {1}", (String)e.UserState, e.Error.Message);
            }
            else
            {
                logger.Log("Heartbeat for {0} posted to Server successfully", (String)e.UserState);
            }
        }

        public HeartbeatInfo GetPlatformHeartBeatInfo()
        {
            HeartbeatInfo hbi = new HeartbeatInfo();

            hbi.HomeId = Settings.HomeId;
            hbi.OrgId = Settings.OrgId;
            hbi.StudyId = Settings.StudyId;
            hbi.HubTimestamp = DateTime.UtcNow.ToString();
            hbi.PhysicalMemoryBytes = this.perfCountWorkingSet.NextValue(); 
            hbi.TotalCpuPercentage = this.perfCountPercentProcTime.NextValue();
            hbi.ModuleMonitorInfoList = this.platform.GetModuleMonitorInfoList();
            hbi.ScoutInfoList = this.platform.GetScoutInfoList();
            hbi.HeartbeatIntervalMins = this.heartbeatIntervalMins;
            hbi.SequenceNumber = this.sequenceNumber++;
            hbi.HardwareId = HomeOS.Hub.Common.Utils.HardwareId;
            hbi.PlatformVersion = this.platform.GetPlatformVersion();

            return hbi;
        }

        private void SendHeartbeat(Object stateInfo)
        {
            try
            {
                HeartbeatInfo heartbeatInfo = GetPlatformHeartBeatInfo();
                if (null != heartbeatInfo)
                {
                    string jsonString = heartbeatInfo.SerializeToJsonStream();
                    logger.Log("Sending heartbeat: {0}", jsonString);
                    WebClient webClient = new WebClient();
                    webClient.UploadStringCompleted += webClient_UploadHeartbeatInfoCompleted;
                    webClient.Headers["Content-type"] = "application/json";
                    webClient.Encoding = Encoding.UTF8;
                    webClient.UseDefaultCredentials = true;
                    webClient.UploadStringAsync(new Uri(this.uri.OriginalString + "/SetHeartbeatInfo"), "POST", jsonString, jsonString);
                }
            }
            catch (Exception e)
            {
                logger.Log("Failed to send heartbeat, exception={0}", e.Message);
            }
        }
     
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if(timer!=null)
                        this.timer.Dispose();
                    if (perfCountPercentProcTime != null)
                        this.perfCountPercentProcTime.Dispose();
                    if (perfCountWorkingSet != null)
                        this.perfCountWorkingSet.Dispose();
                }

                this.disposed = true;
            }
        }

        void webClient_CanIClaimHomeIdCompleted(object sender, UploadStringCompletedEventArgs arg)
        {
            Action<bool> callback = ((ClaimHomeIdCallbackTokenObject)arg.UserState).Callback;
            if (arg.Error != null)
            {
                logger.Log("Checking for unique Home ID for {0} failed with Server error : {1}", ((ClaimHomeIdCallbackTokenObject)arg.UserState).JsonSerialized, arg.Error.Message);
                callback(false);
            }
            else
            {
                logger.Log("Successfully checked for unique Home ID for {0}", ((ClaimHomeIdCallbackTokenObject)arg.UserState).JsonSerialized);

                if (arg.Result != null)
                {
                    callback(arg.Result == "true");
                }
                else
                {
                    callback(false);
                }
            }
        }

        public class ClaimHomeIdCallbackTokenObject
        {
            public Action<bool> Callback { get; set; }
            public string JsonSerialized { get; set; }
        }

        internal void CanIClaimHomeId(string hardwareId, string homeId, Action<bool> callback)
        {
            try
            {
                ClaimHomeIdInfo claimHomeIdInfo = new ClaimHomeIdInfo() { HardwareId = hardwareId, HomeId = homeId };
                if (null != claimHomeIdInfo)
                {
                    string jsonString = claimHomeIdInfo.SerializeToJsonStream();
                    logger.Log("Checking for unique Home ID: {0}", jsonString);
                    WebClient webClient = new WebClient();
                    webClient.UploadStringCompleted += webClient_CanIClaimHomeIdCompleted;
                    webClient.Headers["Content-type"] = "application/json";
                    webClient.Encoding = Encoding.UTF8;
                    webClient.UploadStringAsync(new Uri(this.uri.OriginalString + "/CanClaimHomeId"), "POST", jsonString,
                                new ClaimHomeIdCallbackTokenObject() { Callback = callback, JsonSerialized = jsonString });
                }
            }
            catch (Exception e)
            {
                logger.Log("Exception thrown while checking for unique Home ID, exception={0}", e.Message);
            }
        }
    }

}
