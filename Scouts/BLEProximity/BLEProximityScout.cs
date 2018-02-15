using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace HomeOS.Hub.Scouts.BLEProximity
{
    public class BLEProximityScout : IScout
    {
        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        BLEProximityScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new BLEProximityScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("BLEProximityScout initialized");
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
            return GetDevicesAsync().Result;
        }

        public async Task<List<Device>> GetDevicesAsync()
        {
            var batteryServiceDeviceInfoCollection = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromShortId(0x180F), new string[] { "System.Devices.ContainerId" });
            var res = new List<Device>();

            foreach (var device in batteryServiceDeviceInfoCollection)
            {
                //Output("\nDevice id : " + device.Id + "  name " + device.Name);

            //    //foreach (var prop in device.Properties)
            //    //{
            //    //    Output(" " + prop.Key + " " + (prop.Value == null ? null : prop.Value.GetType() + " " + prop.Value));
            //    //}

                var deviceContainerId = "{" + device.Properties["System.Devices.ContainerId"] + "}";

                string macaddr = "";
                try
                {
                    macaddr = device.Id.Split('_')[1].Substring(0, 12);
                }
                catch
                {
                    Output("Unable to parse mac address for device " + device.Id);
                    continue;
                }

                Device d = new Device("BLE device " + device.Name, "bleproximity-"+macaddr, "", DateTime.Now, "HomeOS.Hub.Drivers.BLEProximity", false);
                //intialize the parameters for this device
                 d.Details.DriverParams = new List<string>() { macaddr };
                res.Add(d);
            }
            Output("BLE prox scout found " + res.Count + " paired devices with battery characteristics");
            return res;
        }


        internal string GetInstructions()
        {
            return "Placeholder for instructions for bleproximity scout";
        }


        //async Task<bool> FindDevices()
        //{
        //    // IMPORTANT:
        //    // For any of this to work, the XML for the Packages.appxmanifest file needs to be modified (right-click on Package.appxmanifest - > View Code)
        //    // and check out <m2:DeviceCapability Name="bluetooth.genericAttributeProfile"> entry in this example

        //    // GUID is determined by the device service
        //    //var devicesInfoCollection = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(new Guid("569a1101-b87f-490c-92cb-11ba5ea5167c")));

        //    // see e.g. https://github.com/sandeepmistry/node-bleacon/blob/master/linux-ibeacon.js 
        //    // http://joris.kluivers.nl/blog/2013/09/27/playing-with-ibeacon/
        //    // 
        //    //var devicesInfoCollection2 = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(new Guid("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0")));
        //    //Output("\n\n==== ibeacon found " + devicesInfoCollection2.Count() + " devices ==== ");
        //    //foreach (var device in devicesInfoCollection2)
        //    //{
        //    //    Output("\nDevice id : " + device.Id + "  name " + device.Name);

        //    //    foreach (var prop in device.Properties)
        //    //    {
        //    //        Output(" " + prop.Key + " " + (prop.Value == null ? null : prop.Value.GetType() + " " + prop.Value));
        //    //    }
        //    //}
        //    //   foreach (var devicePaired in devicesPaired.Keys.ToList()) devicesPaired[devicePaired] = false;

        //    var batteryServiceDeviceInfoCollection = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromShortId(0x180F), new string[] { "System.Devices.ContainerId" });
        //    //Output("\n\n==== battery service found " + devicesInfoCollection2.Count() + " devices ==== ");

        //    foreach (var device in batteryServiceDeviceInfoCollection)
        //    {
        //        //Output("\nDevice id : " + device.Id + "  name " + device.Name);

        //        //foreach (var prop in device.Properties)
        //        //{
        //        //    Output(" " + prop.Key + " " + (prop.Value == null ? null : prop.Value.GetType() + " " + prop.Value));
        //        //}

        //        var deviceContainerId = "{" + device.Properties["System.Devices.ContainerId"] + "}";

        //        string macaddr = "";
        //        try
        //        {
        //            macaddr = device.Id.Split('_')[1].Substring(0, 12);
        //        }
        //        catch
        //        {
        //            Output("Unable to parse mac address for device " + device.Id);
        //            return false;
        //        }

        //        //try
        //        //{
        //        //    GattCharacteristic characteristic = null;
        //        //    GattDeviceService _bluetoothGattDeviceService = await GattDeviceService.FromIdAsync(device.Id);
        //        //    if (_bluetoothGattDeviceService == null)
        //        //    {
        //        //        Output("GattDeviceService is null - was manifest set up correctly to permit this service to be used?");
        //        //        continue;
        //        //    }

        //        //    var characteristics = _bluetoothGattDeviceService.GetCharacteristics(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.ConvertShortIdToUuid(0x2a19));
        //        //    if (characteristics == null || characteristics.Count() == 0)
        //        //    {
        //        //        Output("Device has no battery level characteristic");
        //        //        continue;
        //        //    }
        //        //    if (characteristics.Count() != 1) Output("battery level - >1 characteristic returned!");
        //        //    characteristic = characteristics[0];

        //        //    if (!devicesPaired.ContainsKey(deviceContainerId))
        //        //    {
        //        //        devicesPaired.Add(deviceContainerId, true);
        //        //        batteryCharacteristics[deviceContainerId] = characteristic;
        //        //        deviceMacAddresses[deviceContainerId] = macaddr;
        //        //        deviceNames[deviceContainerId] = device.Name;
        //        //        Output("$$$$$ new paired device " + getNameAndMac(deviceContainerId) + " id " + device.Id + " containerId " + deviceContainerId);

        //        //        // see http://msdn.microsoft.com/en-us/library/windows/hardware/dn423915(v=vs.85).aspx - we have to subscribe to an event (valuechanged) to actually force connection to be established - reading/writing only maintains an existing connection

        //        //        characteristic.ValueChanged += (a, b) => characteristic_ValueChanged(device.Id, a, b);
        //        //        Output("*** SUBSCRIBED TO BATTERY LEVEL VALUE CHANGED FOR " + macaddr);

        //        //        var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
        //        //        if (status == GattCommunicationStatus.Unreachable)
        //        //        {
        //        //            Output("Unable to set battery characteristic descriptor to notify");
        //        //        }
        //        //        else
        //        //        {
        //        //            Output("*** BATTERY LEVEL VALUE CHANGED SET TO NOTIFY FOR " + macaddr);
        //        //            devicesSetToNotify.Add(deviceContainerId);
        //        //            devicesConnected.Add(deviceContainerId);
        //        //            Output(" !!!!!   Device " + getNameAndMac(deviceContainerId) + " is connected");
        //        //        }
        //        //    }
        //        //    else // seen this device before
        //        //    {
        //        //        devicesPaired[deviceContainerId] = true;
        //        //        if (!devicesConnected.Contains(deviceContainerId) && !outstandingReads.Contains(deviceContainerId)) // not connected (since we're detecting disconnections OK but not connections)
        //        //        {
        //        //            outstandingReads.Add(deviceContainerId);
        //        //            Task.Run(async () =>
        //        //            {
        //        //                try
        //        //                {
        //        //                    //Output("Entering read for " + getMac(deviceContainerId));
        //        //                    var result = await characteristic.ReadValueAsync(Windows.Devices.Bluetooth.BluetoothCacheMode.Cached);

        //        //                    if (result.Status == GattCommunicationStatus.Success)
        //        //                    {
        //        //                        //var readdata = result.Value.ToArray();
        //        //                        //string readdatastring = "";
        //        //                        //foreach (byte b in readdata)
        //        //                        //{
        //        //                        //    readdatastring += b + " ";
        //        //                        //}
        //        //                        //Output("Success reading battery level from " + getMac(deviceContainerId) + " : len " + result.Value.Length + " data " + readdatastring);
        //        //                    }
        //        //                }
        //        //                catch
        //        //                {
        //        //                    //Output("Unable to read battery level from " + getMac(deviceContainerId);
        //        //                }
        //        //                finally
        //        //                {
        //        //                    outstandingReads.Remove(deviceContainerId);
        //        //                    //Output("Exiting read for " + getMac(deviceContainerId));
        //        //                }
        //        //            });
        //        //            //characteristicsToRead.Add(characteristic);
        //        //        }
        //        //    }
        //        //    }
        //        //    catch (Exception ex)
        //        //    {
        //        //        Output("Exception in FindDevice for " + macaddr + " : " + ex);
        //        //    }

        //        //}

        //        //foreach (var devicePaired in devicesPaired.Where(x => x.Value == false).ToList())
        //        //{
        //        //    Output("$$$$$ Device was unpaired: " + getNameAndMac(devicePaired.Key));
        //        //    batteryCharacteristics.Remove(devicePaired.Key);
        //        //    deviceMacAddresses.Remove(devicePaired.Key);
        //        //    deviceNames.Remove(devicePaired.Key);
        //        //    devicesPaired.Remove(devicePaired.Key);
        //        //}

        //        //return false;
        //    }
        //}

        void Output(string s)
        {
          logger.Log(s);
          }

    }
}
