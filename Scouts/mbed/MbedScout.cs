using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Management;

namespace HomeOS.Hub.Scouts.Mbed
{
    /// <summary>
    /// Scout for mbed devices. For further info visit http://www.sysnet.org.pk/w/SoftUPS
    /// </summary>
    
    public class MbedScout : IScout
    {
        private string baseUrl;
        private ScoutViewOfPlatform platform;
        private VLogger logger;

        private MbedScoutService scoutService;
        private WebFileServer appServer;
        private bool disposed = false;

        /// <summary>
        /// mbed serial name
        /// </summary>
        private const string MBED = "mbed";

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new MbedScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("MbedScout initialized");
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
                    scoutService.Dispose();
                    appServer.Dispose();
                }

                disposed = true;
            }
        }
        internal string GetInstructions()
        {
            return "Placeholder for instructions to help discover mbed devices";
        }
        public List<HomeOS.Hub.Common.Device> GetDevices()
        {
            List<Device> retList = new List<Device>();
            //Getting all list of COMPORTs available on the system.
            List<COMPortFinder> comportList = COMPortFinder.GetCOMPortsInfo();
            foreach (COMPortFinder comPortInfo in comportList)
            {
                //Checking if COMPORT is our desired COMPORT or not.
                if (comPortInfo.Description.Contains(MBED))
                {
                    string deviceUniqueName = String.Format("{0} - {1}", MBED, comPortInfo.Name);
                    string deviceFriendlyName = comPortInfo.Description;
                    /*
                     * The device object which is filled with necessary information like
                     * which driver should be invoked when this particular device is added to the platform.
                     */
                    Device device = new Device(deviceFriendlyName, deviceUniqueName, "", DateTime.Now, "HomeOS.Hub.Drivers.MbedDriver", false);

                    /*
                    * intialize the parameters for this device, 
                    * these paramenters will be passed to the driver when driver is invoked.
                    */
                    device.Details.DriverParams = new List<string>() { deviceUniqueName };

                    retList.Add(device);
                }
            }
            // list of devices will be returned to HomeOS platform/dashboard.
            return retList;
        }

    }

    #region "COMPort Related"
    internal class COMPortFinder
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public COMPortFinder() { }

        public static List<COMPortFinder> GetCOMPortsInfo()
        {
            List<COMPortFinder> COMPortFinderList = new List<COMPortFinder>();

            ConnectionOptions options = ProcessConnection.ProcessConnectionOptions();
            ManagementScope connectionScope = ProcessConnection.ConnectionScope(Environment.MachineName, options, @"\root\CIMV2");

            ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0");
            ManagementObjectSearcher comPortSearcher = new ManagementObjectSearcher(connectionScope, objectQuery);

            using (comPortSearcher)
            {
                string caption = null;
                foreach (ManagementObject obj in comPortSearcher.Get())
                {
                    if (obj != null)
                    {
                        object captionObj = obj["Caption"];
                        if (captionObj != null)
                        {
                            caption = captionObj.ToString();
                            if (caption.Contains("(COM"))
                            {
                                COMPortFinder COMPortFinder = new COMPortFinder();
                                COMPortFinder.Name = caption.Substring(caption.LastIndexOf("(COM")).Replace("(", string.Empty).Replace(")",
                                                                     string.Empty);
                                COMPortFinder.Description = caption;
                                COMPortFinderList.Add(COMPortFinder);
                            }
                        }
                    }
                }
            }
            return COMPortFinderList;
        }
    }
    internal class ProcessConnection
    {

        public static ConnectionOptions ProcessConnectionOptions()
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Impersonation = ImpersonationLevel.Impersonate;
            options.Authentication = AuthenticationLevel.Default;
            options.EnablePrivileges = true;
            return options;
        }

        public static ManagementScope ConnectionScope(string machineName, ConnectionOptions options, string path)
        {
            ManagementScope connectScope = new ManagementScope();
            connectScope.Path = new ManagementPath(@"\\" + machineName + path);
            connectScope.Options = options;
            connectScope.Connect();
            return connectScope;
        }
    }
    #endregion
}
