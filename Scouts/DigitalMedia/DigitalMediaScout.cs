using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;


namespace HomeOS.Hub.Scouts.DigitalMedia
{
    public class DigitalMediaScout : IScout
    {

        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        DigitalMediaScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        private string DigitalMediaConnectionUrl = string.Empty;

        public DigitalMediaConfiguration dmConfig;

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            dmConfig = new DigitalMediaConfiguration(baseDir, platform, logger);

            scoutService = new DigitalMediaScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("DigitalMediaScout initialized");
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


        public List<Device> GetDevices()
        {

            List<Device> deviceList = new List<Device>();

            Device device = new Device("Crestron control panel", "digitalsignal", "", DateTime.Now, "HomeOS.Hub.Drivers.DigitalMedia", true);            
            //intialize the parameters for this device
            DigitalMediaPanelDescription parameters = this.dmConfig.GetPanelDescriptions.FirstOrDefault<DigitalMediaPanelDescription>();
            device.DeviceIpAddress = parameters.IPAddress;
            device.NeedsCredentials = true;
            device.Details.DriverParams = new List<string>() {device.UniqueName, parameters.IPAddress, 
                parameters.IPID.ToString(), parameters.IPPort.ToString(), parameters.UserName, parameters.Password, parameters.UseSSL.ToString()};
            device.Details.Configured = false;
            deviceList.Add(device);

            return deviceList;
          /*
            DigitalMediaPanelDescription parameters = this.dmConfig.GetPanelDescriptions.FirstOrDefault<DigitalMediaPanelDescription>();
            Device device = new Device("crestron_panel", "crestron_panel_" + parameters.IPID,
                "", DateTime.Now, "HomeOS.Hub.Drivers.DigitalMedia"); //parameters.IPAddress,true
            //intialize the parameters for this device

            device.Details.DriverParams = new List<string>() { device.UniqueName, device.DeviceIpAddress, 
                parameters.IPID.ToString(), parameters.IPPort.ToString(),
                parameters.UserName, parameters.Password, parameters.UseSSL.ToString()};
            device.Details.Configured = false;
            return new List<Device>() { device };
           * */
        }



        internal string GetInstructions()
        {
            return "Placeholder for instructions for dummy scout";
        }

    }
}
