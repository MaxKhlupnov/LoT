using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Threading;
using System.IO;
using HomeOS.Hub.Common.Bluetooth.BluetoothWrapper;

namespace HomeOS.Hub.Drivers.BluetoothDriver {
    /// <summary>
    /// Bluetooth Driver responsible for pairing with the configured Bluetooth Devices.
    /// </summary>
    [System.AddIn.AddIn("HomeOS.Hub.Drivers.BluetoothDriver")]
    public class BluetoothDriver : ModuleBase {
        SafeThread workThread = null;
        Port bluetoothPort;

        public override void Start() {
            logger.Log("Started: {0}", ToString());

            string deviceName = moduleInfo.Args()[0];
            string deviceAddress = moduleInfo.Args()[1];
            string deviceClassType = moduleInfo.Args()[2];
            string deviceType = moduleInfo.Args()[3];

            //try pair with the device.
            bool pair = tryPairWithDevice(deviceAddress, deviceType);
            if (!pair) {
                //think of something
            } else {
                logger.Log("Bluetooth Driver: Paired with \"" + deviceName + " - " + deviceAddress + "\"");
            }
            //init the port
            VPortInfo portInfo = GetPortInfoFromPlatform(deviceName);
            bluetoothPort = InitPort(portInfo);

            //init the role
            List<VRole> listRole = new List<VRole>() { RoleBluetooth.Instance };
            BindRoles(bluetoothPort, listRole);

            //register the port
            RegisterPortWithPlatform(bluetoothPort);

            workThread = new SafeThread(delegate() { Work(); }, "DriverBluetooth work thread", logger);
            workThread.Start();
        }

        /// <summary>
        /// Maybe change this to inheritance with a class implementation of an interface
        /// or abstract class  for each type of device.
        /// </summary>
        /// <param name="deviceAddress">Bluetooth Device Address</param>
        /// <param name="deviceType">Bluetooth Device Type</param>
        /// <returns>
        /// Succefful Paired to device.
        /// </returns>
        private bool tryPairWithDevice(string deviceAddress, string deviceType) {
            string pin = "";
            if (deviceType.Equals("Engduino")) {
                //currently hard coded. Without this line, a Windows popup will appear to enter PIN.
                pin = "1234";
            }
            List<BluetoothDevice> pairedDevices = Bluetooth.getAllPairedDevices();
            foreach (BluetoothDevice device in pairedDevices) {
                if (device.DeviceAddress.Equals(deviceAddress)) {
                    return true;
                }
            }
            BluetoothDevice btDevice = Bluetooth.getDeviceByAddress(deviceAddress, false);
            if (btDevice == null) {
                btDevice = Bluetooth.getDeviceByAddress(deviceAddress, true);
            }
            if (btDevice != null) {
                return Bluetooth.pairWithDevice(btDevice, pin);
            }
            return false;
        }

        public override void Stop() {
            logger.Log("Stop() at {0}", ToString());
            if (workThread != null) {
                workThread.Abort();
            }
        }


        /// <summary>
        /// Sit in a loop and send notifications 
        /// </summary>
        public void Work() {
            int counter = 0;
            while (true) {
                counter++;

                //IList<VParamType> retVals = new List<VParamType>() { new ParamType(counter) };

                //dummyPort.Notify(RoleDummy2.RoleName, RoleDummy2.OpEchoSubName, retVals);

                Notify(bluetoothPort, RoleBluetooth.Instance, RoleBluetooth.OpEchoSubName, new ParamType(counter));

                System.Threading.Thread.Sleep(1 * 5 * 1000);
            }
        }

        /// <summary>
        /// The demultiplexing routing for incoming operations
        /// </summary>
        /// <param name="message"></param>
        public override IList<VParamType> OnInvoke(string roleName, String opName, IList<VParamType> args) {

            if (!roleName.Equals(RoleBluetooth.RoleName)) {
                logger.Log("Invalid role {0} in OnInvoke", roleName);
                return null;
            }

            switch (opName.ToLower()) {
                case RoleBluetooth.OpEchoName:
                    int payload = (int)args[0].Value();
                    logger.Log("{0} Got EchoRequest {1}", this.ToString(), payload.ToString());

                    return new List<VParamType>() { new ParamType(-1 * payload) };

                default:
                    logger.Log("Invalid operation: {0}", opName);
                    return null;
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        ///        the dummy driver does not care about other ports in the system
        /// </summary>
        public override void PortRegistered(VPort port) { }

        /// <summary>
        ///  Called when a new port is deregistered with the platform
        ///        the dummy driver does not care about other ports in the system
        /// </summary>
        public override void PortDeregistered(VPort port) { }
    }
}