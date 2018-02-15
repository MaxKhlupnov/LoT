using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Collections;
using InTheHand.Net;
using System.IO;

namespace HomeOS.Hub.Common.Bluetooth.BluetoothWrapper {
    /// <summary>
    /// Bluetooth Wrapper responsible for general Bluetooth related operations.
    /// The wrapper is using the 32 Feet library (InTheHand.dll)
    /// http://32feet.codeplex.com/
    /// </summary>
    public class Bluetooth {

        public static BluetoothClient bc = new BluetoothClient();
        public static Dictionary<BluetoothDevice, Stream> devicesStreams = new Dictionary<BluetoothDevice, Stream>();
        private static Dictionary<BluetoothDevice, BluetoothClient> devicesClients = new Dictionary<BluetoothDevice, BluetoothClient>();

        /// <summary>
        /// Get a list of all bluetooth devices in range.
        /// </summary>
        /// <returns>
        /// Will not return devices already paired or connected to the host.
        /// </returns>
        public static List<BluetoothDevice> getAllDevices() {
            List<BluetoothDevice> allDevices = new List<BluetoothDevice>();
            BluetoothDeviceInfo[] devices = bc.DiscoverDevices();
            foreach (BluetoothDeviceInfo btInfo in devices) {
                allDevices.Add(new BluetoothDevice(btInfo));
            }
            return allDevices;
        }

        /// <summary>
        /// Returns all already paired device to the host (both connected and not connected).
        /// </summary>
        /// <returns></returns>
        public static List<BluetoothDevice> getAllPairedDevices() {
            List<BluetoothDevice> allDevices = new List<BluetoothDevice>();
            BluetoothDeviceInfo[] devices = bc.DiscoverDevices(255, false, true, false, false);
            foreach (BluetoothDeviceInfo btInfo in devices) {
                allDevices.Add(new BluetoothDevice(btInfo));
            }
            return allDevices;
        }

        public static Boolean pairWithDevice(BluetoothDevice device, String passCode) {
            return BluetoothSecurity.PairRequest(device.btDeviceInfo.DeviceAddress, passCode);
        }

        public static Boolean pairWithDevice(BluetoothDevice device) {
            return pairWithDevice(device, null);
        }
        /// <summary>
        /// Attempt to connect to the BluetoothDevice, without trying to pair first.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static async Task<Boolean> connectToDeviceWithoutPairing(BluetoothDevice device) {
            BluetoothEndPoint endPoint = new BluetoothEndPoint(device.btDeviceInfo.DeviceAddress, BluetoothService.SerialPort);
            BluetoothClient client = new BluetoothClient();
            if (!device.Authenticated) {
                return false;
            }
            else {
                try {
                    client.Connect(endPoint);

                    if (devicesStreams.Keys.Contains(device)) {
                        devicesStreams.Remove(device);
                    }
                    devicesStreams.Add(device, client.GetStream());

                    if (devicesClients.Keys.Contains(device)) {
                        devicesClients.Remove(device);
                    }
                    devicesClients.Add(device, client);
                }
                catch (Exception ex) {
                    //System.Console.Write("Could not connect to device: " + device.DeviceName + " " + device.DeviceAddress);
                    return false;
                }
                return true;
            }
        }

        public static void disconnectFromDevice(BluetoothDevice device) {
            if (devicesClients.Keys.Contains(device)) {
                try {
                    devicesClients[device].Close();
                    devicesClients.Remove(device);
                    devicesStreams.Remove(device);
                } catch (Exception e) { }
            }
        }

        public static void disconnectFromAllDevices() {
            foreach (BluetoothDevice device in devicesClients.Keys) {
                try {
                    devicesClients[device].Close();
                    devicesStreams.Remove(device);
                } catch (Exception e) { }
            }
            devicesClients.Clear();
        }
        /// <summary>
        /// Returns list of devices with the specified name
        /// </summary>
        /// <param name="deviceName">Name to search.</param>
        /// <param name="forceRescan">
        /// If true, will search in all non-paired devices. (this requires rescan, which is time consuming)
        /// If false, will search in all paired devices.
        /// </param>
        /// <returns></returns>
        public static List<BluetoothDevice> getDeviceByName(String deviceName, Boolean forceRescan) {
            List<BluetoothDevice> ret = new List<BluetoothDevice>();
            List<BluetoothDevice> devices;
            if (forceRescan) {
                devices =  getAllDevices();
            } else {
                devices = getAllPairedDevices();
            }
            foreach (BluetoothDevice device in devices) {
                if (device.DeviceName.Equals(deviceName)) {
                    ret.Add(device);
                }
            }
            return ret;
        }
        /// <summary>
        /// Returns device with the specified address,
        /// </summary>
        /// <param name="deviceName">Address to search.</param>
        /// <param name="forceRescan">
        /// If true, will search in all non-paired devices. (this requires rescan, which is time consuming!)
        /// If false, will search in all paired devices.
        /// </param>
        /// <returns></returns>
        public static BluetoothDevice getDeviceByAddress(String deviceAddress, Boolean forceRescan) {
            List<BluetoothDevice> devices;
            if (forceRescan) {
                devices = getAllDevices();
            } else {
                devices = getAllPairedDevices();
            }
            if (forceRescan) {
                getAllDevices();
            }
            foreach (BluetoothDevice device in devices) {
                if (device.DeviceAddress.Equals(deviceAddress)) {
                    return device;
                }
            }
            return null;
        }
    }
}
