using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;
using System.Timers;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO.Ports;

namespace HomeOS.Hub.Scouts.Gadgeteer
{
    public class GadgeteerScout : IScout
    {
        const int queryPortNumber = 48468;    //port where we should query the device
        const int responsePortNumber = 48467;   //port where the device responds
        const int listenPortNumber = 48469;   //port where gadgeteer sends unsolicited beacons

        static byte[] request = { 0x00, 0x00 };

        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        GadgeteerScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        UdpClient listenClient;
        bool listenClientClosed = false;

        Dictionary<string, SerialPort> serialPorts = new Dictionary<string, SerialPort>();

        DeviceList currentDeviceList = new DeviceList();

        //these variables are for asynchronous receives
        byte[] asyncBuffer = new byte[2000];
        SocketFlags asyncSocketFlags = SocketFlags.None;
        EndPoint asyncRemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new GadgeteerScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, listenPortNumber);
            listenClient = new UdpClient(endpoint);

            listenClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            listenClient.Client.BeginReceiveMessageFrom(asyncBuffer, 0, 2000, asyncSocketFlags, ref asyncRemoteEndPoint, new AsyncCallback(ReceiveCallback), null);

            //create a time that fires ScanNow() periodically
            var scanTimer = new Timer(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * 1000);
            scanTimer.Enabled = true;
            scanTimer.Elapsed += new ElapsedEventHandler(ScanNow);

            logger.Log("GadgeteerScout initialized");
        }

        private void ScanNow(object source, ElapsedEventArgs e)
        {
            //we first scan USB and then Wifi because if a device is seen on both, we want the Wifi device
            ScanUsb();

            ScanWifi();

            currentDeviceList.RemoveOldDevices(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * ScoutHelper.DefaultNumPeriodsToForgetDevice);

            platform.ProcessNewDiscoveryResults(currentDeviceList.GetClonedList());
        }

        public List<Device> GetDevices()
        {

            //lets trigger a scan now that the user wants to check on devices
            ScanNow(null, null);
           
            return currentDeviceList.GetClonedList();
        }


        void ScanWifi()
        {
            //we are putting this lock here just in case two of these are running at the same time (e.g., if the ScanNow timer was really short)
            //only one of these calls can be active because of socket conflicts
            lock (this)
            {
                //logger.Log("GadgeteerScout:ScanWifi\n");

                using (var client = new UdpClient(responsePortNumber))
                {
                    //configure the socket properly
                    client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
                    client.Client.EnableBroadcast = true;
                    client.Client.ReceiveTimeout = 1000;

                    ScoutHelper.BroadcastRequest(request, queryPortNumber, logger);

                    try
                    {
                        // loop until you timeout or read a bad client
                        while (true)
                        {
                            //var buf = client.Receive(ref endPoint);

                            SocketFlags socketFlags = SocketFlags.None;
                            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                            IPPacketInformation packetInfo;
                            byte[] buf = new byte[2000];
                            int bytesRead = client.Client.ReceiveMessageFrom(buf, 0, 2000, ref socketFlags, ref endPoint, out packetInfo);

                            if (ScoutHelper.IsMyAddress(((IPEndPoint)endPoint).Address))
                                continue;

                            var device = CreateDeviceWifi(buf, bytesRead, (IPEndPoint) endPoint, ScoutHelper.GetInterface(packetInfo));
                            currentDeviceList.InsertDevice(device);
                            logger.Log("Put device on devicelist: {0} \n", device.ToString());
                        }
                    }
                    catch
                    {
                        ;
                    }
                }
            }
        }

        //NB: this function sends the wifi credentials to the device
        void ScanUsb()
        {
            //logger.Log("GadgeteerScout:ScanUSB\n");

            string[] portNames = SerialPort.GetPortNames();

            //we are putting this lock here just in case two of these are running at the same time (e.g., if the ScanNow timer was really short)
            //only one of these calls can be active because of socket conflicts
            lock (this)
            {
                foreach (string portName in portNames)
                {
                    try
                    {
                        int portNumber = int.Parse(portName.Substring(3));

                        //skip low nubmered ports; they cannot be our device
                        //we can't skip Com 1..16 because Win8 assigns low-numbered ports starting from COM3
                        if (portNumber < 3)
                            continue;

                        SerialPort port;

                        if (!serialPorts.ContainsKey(portName))
                        {
                            port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                            port.Handshake = Handshake.None;
                            port.ReadTimeout = 500;  //enough time for the device to respond?

                            serialPorts.Add(portName, port);
                        }
                        else
                        {
                            port = serialPorts[portName];
                        }

                        int maxAttemptsToOpen = 1;

                        while (!port.IsOpen && maxAttemptsToOpen > 0)
                        {

                            //Thread.Sleep(100);
                            try
                            {
                                port.Open();
                                break;
                            }
                            catch (Exception e)
                            {
                                maxAttemptsToOpen--;

                                logger.Log("Got {0} exception while opening {1}. Num attempts left = {2}",
                                            e.Message, portName, maxAttemptsToOpen.ToString());

                                //sleep if we'll atttempt again
                                if (maxAttemptsToOpen > 0)
                                     System.Threading.Thread.Sleep(1 * 1000);
                            }
                        }

                        port.WriteLine("");
                        string serialString = port.ReadLine();

                        //check if this the device we want
                        if (serialString.Contains("HomeOSGadgeteerDevice"))
                        {
                            //we do not insert this device into our list
                            //if we manage to send it our wifi credentials; we will discover it over WiFi and then add

                            //Device device = CreateDeviceUsb(serialString);
                            //InsertDevice(device);

                            //send wifi credentials to the device
                            string wifiSsid = platform.GetPrivateConfSetting("WifiSsid");
                            string wifiKey = platform.GetPrivateConfSetting("WifiKey");

                            if (wifiSsid == null)
                            {
                                logger.Log("Wifi is not configured for the hub. Cannot configure usb gadget");
                            }
                            else
                            {
                                string stringToWrite = String.Format("ssid {0} key {1}", wifiSsid, wifiKey);

                                port.WriteLine(stringToWrite);

                                logger.Log("Written to {0}. \n Got: {1} \n Wrote: {2}", portName, serialString, stringToWrite);
                            }
                        }

                        port.Close();
                    }
                    catch (TimeoutException)
                    {
                        logger.Log("Timed out while trying to read " + portName);
                    }
                    catch (Exception /*e*/)
                    {
                        // let us not print this 
                        // logger.Log("Serial port exception for {0}: {1}", portName, e.ToString());
                    }
                }
            }
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            lock (this)
            {
                if (listenClientClosed)
                    return;
            }

            IPPacketInformation packetInfo;

            int bytesRead = listenClient.Client.EndReceiveMessageFrom(ar, ref asyncSocketFlags, ref asyncRemoteEndPoint, out packetInfo);

            var device = CreateDeviceWifi(asyncBuffer, bytesRead, (IPEndPoint) asyncRemoteEndPoint, ScoutHelper.GetInterface(packetInfo));

            currentDeviceList.InsertDevice(device);

            //logger.Log("GadgeteerListener got packet from {0}", device.UniqueName);

            //let the platform know of the device
            platform.ProcessNewDiscoveryResults(new List<Device>() { device });

            //start listening again
            //listenClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);
            asyncSocketFlags = SocketFlags.None;
            asyncRemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            listenClient.Client.BeginReceiveMessageFrom(asyncBuffer, 0, 2000, asyncSocketFlags, ref asyncRemoteEndPoint, new AsyncCallback(ReceiveCallback), null);
        }

        //this is for devices discovered over Wifi
        private Device CreateDeviceWifi(byte[] response, int length, IPEndPoint sender, NetworkInterface netInterface)
        {
            var devId = Encoding.ASCII.GetString(response, 0, length);
            string driverName = GetDriverName(devId);
            string friendlyName = GetFriendlyName(devId);
            var device = new Device(friendlyName, devId, netInterface, sender.Address.ToString(), DateTime.Now, driverName, false);

            //intialize the parameters for this device
            device.Details.DriverParams = new List<string>() { device.UniqueName };

            return device;
        }

        //this is for devices discovered over Usb
        private Device CreateDeviceUsb(string serialString)
        {
            var devId = serialString;
            string driverName = GetDriverName(devId);
            string friendlyName = GetFriendlyName(devId);
            var device = new Device(friendlyName, devId, "", DateTime.Now, driverName, false);

            //intialize the parameters for this device
            device.Details.DriverParams = new List<string>() { device.UniqueName };

            return device;
        }

        private string GetFriendlyName(string devId)
        {
            var split = devId.Split('_');
            if (split.Count() != 4)
            {
                logger.Log("ERROR::cannot find gadgeteer device friendly name - incorrect number of underscores: " + devId);
                return devId;
            }
            return split[1].Replace(" ", "");
        }

        private string GetDriverName(string devId)
        {
            // devId is HomeOSGadgeteerDevice_WindowCamera_MicrosoftResearch_31256042363112461688 
            // driver will be HomeOS.Hub.Drivers.Gadgeteer.ManufacturerName.DeviceType  (i.e. last two fields are MicrosoftResearch.WindowCamera in above example)

            var split = devId.Split('_');
            if (split.Count() != 4)
            {
                logger.Log("ERROR::cannot find gadgeteer driver - incorrect number of underscores: " + devId);
                return "unknown";
            }
            return "HomeOS.Hub.Drivers.Gadgeteer." + split[2].Replace(" ", "") + "." + split[1].Replace(" ", "");
        }


        public static bool IsGadgeteerDevice(string deviceId)
        {
            return deviceId.StartsWith("HomeOSGadgeteerDevice");
        }


        #region functions called from the UI

        internal string GetInstructions()
        {
            return "Placeholder for helping to find the gadgeteer device";
        }

        internal List<string> SendWifiCredentials(string uniqueDeviceId, string authCode)
        {
            Device device = currentDeviceList.GetDevice(uniqueDeviceId);

            if (device == null)
                return new List<string>() { "Could not find the device in my current list" };

            //get the wifi credentials
            string wifiSsid = platform.GetPrivateConfSetting("WifiSsid");
            string wifiKey = platform.GetPrivateConfSetting("WifiKey");

            if (wifiSsid == null)
                return new List<string>() { "WifiSsid is not configured in the hub" };

            //http://<ip address of device>/credentials?ssid=HOMESSID&setupauthcode=12345678&key=HOMEKEY 

            string encodedUrl = String.Format("http://{0}/credentials?ssid={1}&setupauthcode={2}&key={3}", 
                                                device.DeviceIpAddress, 
                                                Uri.EscapeDataString(wifiSsid), 
                                                Uri.EscapeDataString(authCode), 
                                                Uri.EscapeDataString(wifiKey));

            logger.Log("GadgeteerConfigUrl = " + encodedUrl);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(encodedUrl);

            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

            if (webResponse.StatusCode != HttpStatusCode.OK)
                return new List<string>() { "Got HTTP status code " + webResponse.StatusCode };

            //lets parse the result
            string result = GetHttpResponseStr(webResponse);

            if (result.Contains("OK::"))
                return new List<string>() { "" };
            else
                return new List<string>() { "Got an odd HTTPresponse" };

        }

        private static string GetHttpResponseStr(HttpWebResponse webResponse)
        {
            int bytesRead = 0;
            byte[] result = new byte[webResponse.ContentLength];

            System.IO.Stream responseStream = webResponse.GetResponseStream();

            while (bytesRead < webResponse.ContentLength)
            {
                int n = responseStream.Read(result, bytesRead, (int)webResponse.ContentLength - bytesRead);
                bytesRead += n;
            }

            return System.Text.Encoding.ASCII.GetString(result);
        }

        internal List<string> IsDeviceOnHostedNetwork(string uniqueDeviceId)
        {
            Device device = currentDeviceList.GetDevice(uniqueDeviceId);

            if (device == null)
                return new List<string>() { "Could not find the device in my current list" };

            if (device.LocalInterface == null)
                return new List<string>() { "Local interface was not recorded for this device" };

            if (device.LocalInterface.Description.Contains("Microsoft Hosted Network Virtual Adapter"))
                return new List<string>() { "", "true" };

            return new List<string>() { "", "false" };
        }

        internal List<string> IsDeviceOnHomeWifi(string uniqueDeviceId)
        {
            //let us issue a scan first
            ScanWifi();

            Device device = currentDeviceList.GetDevice(uniqueDeviceId);

            if (device == null)
                return new List<string>() { "Could not find the device in my current list" };

            if (device.LocalInterface == null)
                return new List<string>() { "Local interface was not recorded for this device" };

            if (!device.LocalInterface.Description.Contains("Microsoft Hosted Network Virtual Adapter"))
                return new List<string>() { "", "true" };

            return new List<string>() { "", "false" };
        }

        #endregion

        #region cleanup code
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

                    lock (this)
                    {
                        listenClient.Close();
                        listenClientClosed = true;
                    }
                }

                disposed = true;
            }
        }
        #endregion

    }
}
