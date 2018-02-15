using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Platform.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS
{
    public class PowerMeterScout : IScout
    {
        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        PowerMeterScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new PowerMeterScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("PowerMeterScout initialized");
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
            Device device = new Device("PowerMeterdevice", "PowerMeterdevice", "", DateTime.Now, "HomeOS.Hub.Drivers.PowerMeter", false);
           // Device device = new Device("PowerMeterdevice", "PowerMeterdevice", "", DateTime.Now, "HomeOS.Hub.Apps.PowerMeter", false);

            //intialize the parameters for this device
            device.Details.DriverParams = new List<string>() { device.UniqueName };

            return new List<Device>() { device };
        }


        internal string GetInstructions()
        {
            return "Placeholder for instructions for PowerMeter scout";
        }
    }
}