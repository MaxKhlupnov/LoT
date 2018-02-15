using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Scouts.Dummy
{
    public class DummyScout : IScout
    {
        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        DummyScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new DummyScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("DummyScout initialized");
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
            Device device = new Device("dummydevice", "dummydevice", "", DateTime.Now, "HomeOS.Hub.Drivers.Dummy", false);

            //intialize the parameters for this device
            device.Details.DriverParams = new List<string>() { device.UniqueName };

            return new List<Device>() { device };
        }


        internal string GetInstructions()
        {
            return "Placeholder for instructions for dummy scout";
        }
    }
}
