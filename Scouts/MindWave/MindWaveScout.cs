using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Scouts.MindWave
{
    public class MindWaveScout : IScout
    {
        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        MindWaveScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new MindWaveScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("MindWaveScout initialized");
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
            Device device = new Device("mindwavedevice", "mindwavedevice", "", DateTime.Now, "HomeOS.Hub.Drivers.MindWave", false);

            //intialize the parameters for this device
            device.Details.DriverParams = new List<string>() { device.UniqueName };

            return new List<Device>() { device };
        }


        internal string GetInstructions()
        {
            return "Placeholder for instructions for mindwave scout";
        }
    }
}
