using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common.WebCam.WebCamWrapper.Camera;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Scouts.WebCam
{
    public class WebCamScout : IScout 
    {
        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        WebCamScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new WebCamScoutService(baseUrl + "/webapp", this, platform, logger); 

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("WebCamScout initialized");
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
            List<Device> retList = new List<Device>();

            foreach (Camera camera in CameraService.AvailableCameras)
            {
                string deviceName = camera.ToString();

                Device device = new Device(deviceName, deviceName, "", DateTime.Now, "HomeOS.Hub.Drivers.WebCam", false);

                //intialize the parameters for this device
                device.Details.DriverParams = new List<string>() { device.UniqueName};

                retList.Add(device);
            }

            return retList;
        }

        public string[] GetCameraList()
        {
            List<string> retList = new List<string>();

            foreach (Camera camera in CameraService.AvailableCameras)
            {
                retList.Add(camera.ToString());
            }

            return System.Linq.Enumerable.ToArray<string>(retList);
        }

        internal string GetInstructions()
        {
            return "Placeholder for instructions to help discover webcam";
        }
    }
}
