using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common.Bolt.DataStore;

using System.IO;
using HomeOS.Hub.Common.Bluetooth.BluetoothWrapper;
using HomeOS.Hub.Apps.BluetoothApp.Azure;
using System.Net.Http;
using System.Threading.Tasks;

namespace HomeOS.Hub.Apps.BluetoothApp {
    /// <summary>
    /// Bluetooth App allowing users to configure Bluetooth Devices.
    /// </summary>
    [System.AddIn.AddIn("HomeOS.Hub.Apps.BluetoothApp")]
    public class BluetoothApp : ModuleBase {
        private MobileServices mobileService = new MobileServices();
        private SafeServiceHost serviceHost;
        private WebFileServer appServer;
        private List<String> receivedMessages = new List<String>();
        private SafeThread worker = null;
        private IList<VPort> allPortsList;

        #region Override Methods

        public override void Start() {
            logger.Log("Started: {0} ", ToString());

            BluetoothAppService bluetoothService = new BluetoothAppService(logger, this);
            serviceHost = new SafeServiceHost(logger, typeof(IBluetoothAppContract), bluetoothService, this, Constants.AjaxSuffix, moduleInfo.BaseURL());
            serviceHost.Open();
            //create the app server
            appServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
            //Create a new thread that will try connect to all configured device asynchroniously every 10 sec
            worker = new SafeThread(delegate() { tryConnectToDevices(); }, "AppBluetoothPing-worker", logger);
            worker.Start();
        }

        public override void Stop() { }

        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        public override void PortRegistered(VPort port) {
            ConnectToPairedDevices();
        }

        public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort) { }

        /// <summary>
        /// The worker will try to connect to the specified device and create a listener for
        /// the Bluetooth port messages.
        /// This must be canned async in a separate thread!
        /// </summary>
        public async void Work(BluetoothDevice btDevice) {
            try {
                Boolean success = await Bluetooth.connectToDeviceWithoutPairing(btDevice);
                if (success) {
                    await BluetoothStreamListener(btDevice);
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Resgister all configured ports within the LoT.
        /// </summary>
        /// <param name="portList"></param>
        private void ProcessAllPortsList(IList<VPort> portList) {
            foreach (VPort port in portList) {
                PortRegistered(port);
            }
        }

        public override void PortDeregistered(VPort port) { }

        #endregion

        #region Core Bluetooth App

        /// <summary>
        /// Method that will try to connect to all paired and configured devices.
        /// The connections are done async, in parallel threads to avoid waiting and time-outs.
        /// </summary>
        private void ConnectToPairedDevices() {
            //get all configured devices within LoT
            allPortsList = new List<VPort>();
            while (allPortsList.Count == 0) {
                allPortsList = GetAllPortsFromPlatform();
            }
            //match them with the paired ones. These lists can be different only if the driver failed for some reason.
            List<string> paired = new List<string>();
            foreach (VPort vp in allPortsList) {
                paired.Add(getDeviceAddressFromPort(vp));
            }
            foreach (string p in paired) {
                BluetoothDevice bd = Bluetooth.getDeviceByAddress(p, false);
                if (!bd.Connected || !Bluetooth.devicesStreams.Keys.Contains(bd)) {
                    try {
                        //attempt to connect and add stream listener.
                        worker = new SafeThread(delegate() { Work(bd); }, "AppBluetoothListener-worker", logger);
                        worker.Start();
                    } catch (Exception e) {
                        logger.Log("Got exception in GetPairedDevices: " + e);
                        continue;
                    }
                }
            }
        }
        /// <summary>
        /// Get device Address from VPort.
        /// </summary>
        /// <param name="vp"></param>
        /// <returns></returns>
        private String getDeviceAddressFromPort(VPort vp) {
            VPortInfo vpi = vp.GetInfo();
            string address = vpi.ModuleFacingName();
            string[] split = address.Split('|');
            address = split[split.Length - 1].ToString();
            address = address.Trim();
            return address;
        }
        /// <summary>
        /// Get device Type from VPort
        /// </summary>
        /// <param name="vp"></param>
        /// <returns></returns>
        private String getDeviceTypeFromPort(VPort vp) {
            VPortInfo vpi = vp.GetInfo();
            string address = vpi.ModuleFacingName();
            string[] split = address.Split('|');
            address = split[split.Length - 3].ToString();
            address = address.Trim();
            return address;
        }
        /// <summary>
        /// Returns a list of properties of all connected devices, separated by special character: "|"
        /// </summary>
        /// <returns>
        /// Return format: Name | Address | DeviceType | DeviceConnected
        /// </returns>
        public List<string> GetConnectedDevices() {
            List<string> connectedDevices = new List<string>();
            foreach (VPort port in allPortsList) {
                //get name and address from port
                string name = port.GetInfo().GetFriendlyName();
                string addr = getDeviceAddressFromPort(port);

                BluetoothDevice bd = Bluetooth.getDeviceByAddress(addr, false);
                //get device type and connected from Bluetooth Wrapper
                string deviceType = bd.DeviceType;
                bool deviceConnected = bd.Connected;
                connectedDevices.Add(name + "|" + addr + "|" + deviceType + "|" + deviceConnected);
            }
            return connectedDevices;
        }
        /// <summary>
        /// Send a message to the selected device from the UI expressed as the array of checkboxes.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        public bool SendMessage(string message, string[] check) {
            string m = message;
            byte[] buffer = Encoding.ASCII.GetBytes(m);
            foreach (BluetoothDevice d in Bluetooth.devicesStreams.Keys) {
                Stream s = Bluetooth.devicesStreams[d];
                foreach (string c in check) {
                    if (c.Equals(d.DeviceAddress)) {
                        s.Write(buffer, 0, buffer.Length);
                    }
                }
            }
            return true;
        }

        #endregion

        #region Async Thread Tasks
        /// <summary>
        /// Async Bluetooth Stream Listener waiting and reading messages on the specified Stream.
        /// On message received, the function will try to parse it based on the device type
        /// and store it to the Azure Database in its specific table.
        /// </summary>
        /// <param name="btDevice"></param>
        /// <returns></returns>
        private async Task BluetoothStreamListener(BluetoothDevice btDevice) {
            if (!Bluetooth.devicesStreams.Keys.Contains(btDevice)) {
                //failed to connect to device
                return;
            }
            Stream s = Bluetooth.devicesStreams[btDevice];
            while (true) {
                byte[] buffer = new byte[1];
                string result = "";
                string bufferChar = "";
                while (!bufferChar.Equals("}")) {
                    try {
                        s.Read(buffer, 0, 1);
                    } catch (Exception e) {
                        continue;
                    }
                    bufferChar = Encoding.UTF8.GetString(buffer);
                    result += bufferChar;
                }
                Console.WriteLine(result);
                LoTDevice device;
                if (btDevice.DeviceType.Equals(BluetoothDevice.DEVICE_ENGDUINO)) {
                    device = new Engduino();
                } else if (btDevice.DeviceType.Equals(BluetoothDevice.DEVICE_ANDROIDPHONE)) {
                    device = new AndroidPhone();
                } else {
                    //maybe print the message in the console
                    continue;
                }
                device.MAC = btDevice.DeviceAddress;
                try {
                    device.parseMessage(result);
                } catch (Exception e) {
                    logger.Log("Failed to parse message: " + result + " for device: " + btDevice.DeviceName);
                    continue;
                }
                try {
                    await mobileService.WriteDevice(device);
                } catch (HttpRequestException ex) {
                    logger.Log("No internet connection on writing to Azure MS.", ex.ToString());
                } catch (Exception e) {
                    logger.Log("Failed to write to Azure MS.", e.ToString());
                }
            }
        }
        /// <summary>
        /// Function that will try to reconnect to all lost devices every 1 minute.
        /// This is a backup in case of connection lost, device out of range or any
        /// other scenario that may cause connection to interrupt.
        /// </summary>
        private void tryConnectToDevices() {
            while (true) {
                ConnectToPairedDevices();
                System.Threading.Thread.Sleep(60000);
            }
        }

        #endregion
    }
}