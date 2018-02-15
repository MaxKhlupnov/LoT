using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;
using Microsoft.Kinect;

namespace HomeOS.Hub.Scouts.Kinect
{
    public class KinectScout : IScout
    {
        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        KinectScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new KinectScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("KinectScout initialized");
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

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {

                    string uniqueID = potentialSensor.UniqueKinectId.Replace("\\", "-").Replace("&", ".");

                    string deviceName =  "Kinect Sensor:" + uniqueID   ;
                    Device device = new Device(deviceName, deviceName, "", DateTime.Now, "HomeOS.Hub.Drivers.Kinect", false);
                    //intialize the parameters for this device
                    device.Details.DriverParams = new List<string>() { device.UniqueName };

                    retList.Add(device);
                }
            }
            return retList;
        }

        public string[] GetDevicesList()
        {
            List<string> retList = new List<string>();

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    string deviceName = potentialSensor.ToString();
                    retList.Add(deviceName);
                }
            }
            return System.Linq.Enumerable.ToArray<string>(retList);
        }


        internal string GetInstructions()
        {
            return "Placeholder for instructions to help discover kinect.";
        }
    }
}
