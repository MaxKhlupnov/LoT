using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;
using HomeOS.Hub.Common.Bluetooth.BluetoothWrapper;

namespace HomeOS.Hub.Scouts.BluetoothScout {
    /// <summary>
    /// Bluetooth Scout responsible for finding all Bluetooth Devices in range of the host.
    /// </summary>
    public class BluetoothScout : IScout {

        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        BluetoothScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger) {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new BluetoothScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("BluetoothScout initialized.");
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                if (disposing) {
                    scoutService.Dispose();
                    appServer.Dispose();
                }

                disposed = true;
            }
        }
        /// <summary>
        /// Returns a list of Device consisting of all Bluetooth Devices in range.
        /// </summary>
        /// <returns>
        /// So far the parameters passed to the Bluetooth Driver are:
        /// 1. Device Unique Name
        /// 2. Device Address (MAC)
        /// 3. Device Class (Bluetooth Class Type)
        /// 4. Device Type (Engduino / AndroidPhone only at the moment)
        /// </returns>
        public List<Device> GetDevices() {
            List<Device> ret = new List<Device>();
            List<BluetoothDevice> devices = Bluetooth.getAllDevices();
            foreach (BluetoothDevice blueoothDevice in devices) {
                List<String> driverParams = new List<String>();
                String uniqueName = "Bluetooth | " + blueoothDevice.DeviceType + " | " + blueoothDevice.DeviceName + " | " + blueoothDevice.DeviceAddress;
                Device device = new Device(uniqueName, uniqueName, "", DateTime.Now, "HomeOS.Hub.Drivers.BluetoothDriver", false);
                driverParams.Add(device.UniqueName);
                driverParams.Add(blueoothDevice.DeviceAddress);
                driverParams.Add(blueoothDevice.DeviceClass);
                driverParams.Add(blueoothDevice.DeviceType);
                //intialize the parameters for this device
                device.Details.DriverParams = driverParams;
                ret.Add(device);
            }
            return ret;
        }


        internal string GetInstructions() {
            return "Placeholder for instructions for Bluetooth scout.";
        }
    }
}
