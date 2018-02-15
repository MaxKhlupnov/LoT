using InTheHand.Net;
using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bluetooth.BluetoothWrapper {
    /// <summary>
    /// Generic wrapper of the Bluetooth Device
    /// </summary>
    public class BluetoothDevice {

        private static readonly String ENGDUINO_DEVICE_CLASS = "1F00";
        private static readonly String ANDROIOD_DEVICE_CLASS = "5A020C";

        public static readonly string DEVICE_UNKNOWN = "Unknown";
        public static readonly string DEVICE_ENGDUINO = "Engduino";
        public static readonly string DEVICE_ANDROIDPHONE = "AndroidPhone";

        internal BluetoothDeviceInfo btDeviceInfo { get; private set; }

        internal BluetoothDevice(BluetoothDeviceInfo btDeviceInfo){
            this.btDeviceInfo = btDeviceInfo;
        }

        public string DeviceAddress {
            get {
                return btDeviceInfo.DeviceAddress.ToString();
            }
        }

        public string DeviceName {
            get {
                return btDeviceInfo.DeviceName;
            }
        }

        public bool Authenticated {
            get {
                return btDeviceInfo.Authenticated;
            }
        }

        public string DeviceClass {
            get {
                return btDeviceInfo.ClassOfDevice.ToString();
            }
        }

        public string DeviceType {
            get {
                //figure out a better way.
                String deviceType = DEVICE_UNKNOWN;
                if (ENGDUINO_DEVICE_CLASS.Equals(this.DeviceClass)) {
                    deviceType = DEVICE_ENGDUINO;
                } else if (ANDROIOD_DEVICE_CLASS.Equals(this.DeviceClass)) {
                    deviceType = DEVICE_ANDROIDPHONE;
                }
                return deviceType;
            }
        }

        public bool Connected
        {
            get
            {
                return btDeviceInfo.Connected;
            }
        }

        public override bool Equals(Object obj) {
            if (obj == null) {
                return false;
            }

            BluetoothDevice btDevice = obj as BluetoothDevice;
            if ((System.Object)btDevice == null) {
                return false;
            }

            return DeviceAddress.Equals(btDevice.DeviceAddress);
        }

        public bool Equals(BluetoothDevice btDevice) {
            if ((object)btDevice == null) {
                return false;
            }

            return DeviceAddress.Equals(btDevice.DeviceAddress);
        }

        public override int GetHashCode() {
            return DeviceAddress.GetHashCode();
        }
    }
}
