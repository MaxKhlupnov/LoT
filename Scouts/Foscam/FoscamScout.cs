// The purpose of this class is to discover the presence of a foscam-based IP cameras 
// on the network. I figured this out by sniffing the network trafic when I 
// ran the supplied discovery program supplied by the manufacturer. They discover
// the camera by sending out a UDP request and then parsing the response of the 
// camera. The request packet is an exact copy of what was sent out by the
// manafacturer's discovery program. By inspecting the packet I determined
// that the UDP request packet was sent to port number 10000. I found that
// the response packet contained the MAC and IP address of the camera
// along with the IP address of the router to which the camera is connected.
// The location each of these fields was figured out by inspection.using System;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.IO;
using System.Timers;

namespace HomeOS.Hub.Scouts.Foscam
{
    public class FoscamScannedNetwork
    {
        public string bssid;
        public string ssid;
        public int mode;
        public int security;
    }


    public class FoscamScout : IScout
    {
        const int m_portNumber = 10000;
        const int m_offset_MAC = 0x17;
        const int m_length_MAC = 12;
        const int m_offset_Camera_IP = 0x39;
        const int m_offset_router_IP = 0x41;
        const int m_length_IP = 4;

        static byte[] request = 
            {
                0x4D, 0x4F, 0x5F, 0x49, 0x00, 0x00, 0x00, 0x00
              , 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04 
              , 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00
              , 0x00, 0x00, 0x01
            };


        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        FoscamScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        DeviceList currDeviceList = new DeviceList(); 

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new FoscamScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("FoscamScout initialized");

            //create a time that fires ScanNow() periodically
            var scanTimer = new System.Timers.Timer(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * 1000);
            scanTimer.Enabled = true;
            scanTimer.Elapsed += new System.Timers.ElapsedEventHandler(ScanNow);
        }

        private void ScanNow(object source, ElapsedEventArgs e)
        {
            FillList();
            
            //remove devices not seen for 5 discovery intervals
            currDeviceList.RemoveOldDevices(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * ScoutHelper.DefaultNumPeriodsToForgetDevice);

            platform.ProcessNewDiscoveryResults(currDeviceList.GetClonedList());
        }

        public List<Device> GetDevices()
        {
            //lets go discover again in case we find something new
            FillList();

            return currDeviceList.GetClonedList();
        }

        private void FillList()
        {
            lock (this)
            {
                using (var client = new UdpClient(m_portNumber))
                {
                    //configure the socket properly
                    client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
                    client.Client.EnableBroadcast = true;
                    client.Client.ReceiveTimeout = 1000;

                   ScoutHelper.BroadcastRequest(request, m_portNumber, logger);

                    try
                    {
                        // loop until you timeout or read a bad client
                        while (true)
                        {
                            //IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                            //var buf = client.Receive(ref endPoint);
                            //int bytesRead = buf.Length;

                            SocketFlags socketFlags = SocketFlags.None;
                            IPPacketInformation packetInfo;
                            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                            byte[] buf = new byte[2000];
                            int bytesRead = client.Client.ReceiveMessageFrom(buf, 0, 2000, ref socketFlags, ref endPoint, out packetInfo);

                            if (ScoutHelper.IsMyAddress(((IPEndPoint)endPoint).Address))
                                continue;

                            if (bytesRead < m_offset_router_IP + 4) 
                            {
                                logger.Log("Foscam scout got invalid UDP packet of length {0}", bytesRead.ToString());
                                continue;
                            }

                            //var device = CreateDevice(buf, null);

                            var device = CreateDevice(buf, ScoutHelper.GetInterface(packetInfo));

                            currDeviceList.InsertDevice(device);
                        }
                    }
                    catch (SocketException)
                    {
                        //we expect this error becuase of timeout
                    }
                    catch (Exception e)
                    {
                        logger.Log("Exception in FoscamScout FillList: {0}", e.ToString());
                    }

                    client.Close();
                }
            }
       }

        private static Device CreateDevice(byte[] response, NetworkInterface netInterface)
        {
            var macAddress = getString(response, m_offset_MAC, m_length_MAC);
            var cameraIP = getIPAddressString(response, m_offset_Camera_IP);
            var routerIP = getIPAddressString(response, m_offset_router_IP);

            var device = new Device("Foscam - " + macAddress, GetUniqueName(macAddress), netInterface, cameraIP, DateTime.Now, "HomeOS.Hub.Drivers.Foscam");

            //intialize the parameters for this device
            device.Details.DriverParams = new List<string>() { device.UniqueName, "admin", "" };

            return device;
        }        
        private static byte[] getSubArray(byte[] array, int offset, int count)
        {
            byte[] ans = new byte[count];
            System.Array.Copy(array, offset, ans, 0, count);
            return ans;
        }
        private static string getString(byte[] response, int offset, int count)
        {
            var buf = getSubArray(response, offset, count);
            return Encoding.ASCII.GetString(buf);
        }
        private static string getIPAddressString(byte[] response, int offset)
        {
            byte[] buf = getSubArray(response, offset, m_length_IP);
            IPAddress addr = new IPAddress(buf);
            return addr.ToString();
        }
        private static string GetUniqueName(string macaddr)
        {
            return "foscam:" + macaddr;
        }

        private string GetHttpResponseStr(HttpWebResponse webResponse)
        {
            int bytesRead = 0;
            byte[] result = new byte[webResponse.ContentLength];

            Stream responseStream = webResponse.GetResponseStream();

            while (bytesRead < webResponse.ContentLength)
            {
                int n = responseStream.Read(result, bytesRead, (int)webResponse.ContentLength - bytesRead);
                bytesRead += n;
            }

            return System.Text.Encoding.ASCII.GetString(result);
        }

        private HttpWebResponse SendHttpRequest(string requestUrl, string username, string password, int timeout = 10000)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(requestUrl);
            webRequest.PreAuthenticate = true;
            webRequest.Credentials = new NetworkCredential(username, password);
            webRequest.Timeout = timeout;

            logger.Log("Sending to foscam: {0}", requestUrl);

            return (HttpWebResponse)webRequest.GetResponse();
        }


        // this function relies on the following empirically observed behavior of wireless foscams
        //    the MAC address embedded in the discovery response is that of the ethernet NIC 
        //    so cameras on WiFi have inconsistency in MAC addresses in discovery response and in ARP
        public List<string> IsDeviceOnWifi(string unqiueDeviceId)
        {

            Device device = currDeviceList.GetDevice(unqiueDeviceId);

            if (device == null)
                return new List<string>() { "Could not find the device in my current list", "" };

            string macAddress = ScoutHelper.GetMacAddressByIP(IPAddress.Parse(device.DeviceIpAddress)).Replace("-", string.Empty);

            if (macAddress == null)
                return new List<string>() { "Could not get MAC address of device", "" };

            string uniqueName = GetUniqueName(macAddress);

            if (!uniqueName.Equals(device.UniqueName))
                return new List<string>() { "", "true" };

            return new List<string>() { "", "false" };
        }

        public List<string> AreCameraCredentialsValid(string uniqueDeviceId, string username, string password)
        {
            Device device = currDeviceList.GetDevice(uniqueDeviceId);

            if (device == null)
                return new List<string>() { "Could not find the device in my current list", "" };

            string requestStr = String.Format("http://{0}/get_camera_params.cgi", device.DeviceIpAddress);

            try
            {
                HttpWebResponse webResponse = SendHttpRequest(requestStr, username, password);

                //we get the string just so we can eat it
                GetHttpResponseStr(webResponse);

                // if the request went through we are good
                return new List<string>() { "", "true" };

            }
            catch (Exception webEx)
            {
                //if the exception contains 401, that implies unauthorized access
                if (webEx.Message.Contains("401"))
                    return new List<string>() { "", "false" };

                //otherwise, we don't know what is going on; propagate this exception up
                logger.Log("Got exception while checking foscam credentials: ", webEx.ToString());

                return new List<string>() { webEx.Message, "" };
            }
        }

        List<FoscamScannedNetwork> ParseScanResults(string scanResultStr)
        {
            List<FoscamScannedNetwork> scanResults = new List<FoscamScannedNetwork>();

            Regex regex = new Regex(@"ap_(.+)\[(\d+)\]=(.+);");

            var lines = scanResultStr.Split(new[] { '\r', '\n' });

            foreach (string line in lines)
            {
                Match match = regex.Match(line);

                if (match.Success)
                {
                    string key = match.Groups[1].Value;
                    int num = int.Parse(match.Groups[2].Value);
                    string value = match.Groups[3].Value;

                    FoscamScannedNetwork network;



                    if (key.Equals("bssid"))
                    {
                        //we expect bssid to be listed first
                        if (scanResults.Count != num)
                            throw new Exception("counts don't match up");

                        network = new FoscamScannedNetwork();
                        network.bssid = StripQuotes(value);

                        scanResults.Add(network);

                    }
                    else
                    {
                        network = scanResults[num];


                        switch (key)
                        {
                            case "ssid":
                                network.ssid = StripQuotes(value);
                                break;
                            case "mode":
                                network.mode = int.Parse(value);
                                break;
                            case "security":
                                network.security = int.Parse(value);
                                break;
                            default:
                                throw new Exception("unknown key type");

                        }
                    }
                }

            }

            return scanResults;

        }

        string StripQuotes(string input)
        {
            return input.Substring(1, input.Length - 2);
        }

        private void IssueScanCommand(string cameraIp, string username, string password)
        {
            string scanIssueUrl = String.Format("http://{0}/wifi_scan.cgi?user=undefined&pwd=undefined", cameraIp);
            HttpWebResponse webResponse = SendHttpRequest(scanIssueUrl, username, password);

            if (webResponse.StatusCode != HttpStatusCode.OK)
            {
                logger.Log("FoscamScout got bad status code {0} in scan issue query", webResponse.StatusCode.ToString());
            }

            //to eat the response
            GetHttpResponseStr(webResponse);
        }

        private void Reboot(string cameraIp, string username, string password)
        {
            string rebootUrl = String.Format("http://{0}/reboot.cgi?user=undefined&pwd=undefined&next_url=reboot.htm", cameraIp);
            //HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(scanIssueUrl);
            //webRequest.Credentials = new NetworkCredential(username, password); ;
            //HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

            HttpWebResponse webResponse = SendHttpRequest(rebootUrl, username, password);

            if (webResponse.StatusCode != HttpStatusCode.OK)
            {
                logger.Log("FoscamScout got bad status code {0} in reboot command", webResponse.StatusCode.ToString());
            }

            //we don't need it but lets eat the response
            GetHttpResponseStr(webResponse);
        }

        public List<FoscamScannedNetwork> GetScanResult(string cameraIp, string username, string password)
        {

            IssueScanCommand(cameraIp, username, password);

            //the web pages sleep for 4 seconds
            System.Threading.Thread.Sleep(4000);

            string url = String.Format("http://{0}/get_wifi_scan_result.cgi?user=undefined&pwd=undefined", cameraIp);

            HttpWebResponse webResponse = SendHttpRequest(url, username, password);

            //int maxTries = 3;
            //while (maxTries > 0)
            //{
            //    try
            //    {
            //        //HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            //        //webRequest.Credentials = new NetworkCredential(username, password);
            //        //webRequest.Timeout = 10000; //3 seconds

            //        //webRequest.ProtocolVersion = HttpVersion.Version10;
            //        //webRequest.PreAuthenticate = true;

            //        //webResponse = (HttpWebResponse)webRequest.GetResponse();

            //        webResponse = SendHttpRequest(url, username, password, 5000);

            //        //to eat the response

            //        break;
            //    }
            //    catch (Exception e)
            //    {
            //        Console.Error.WriteLine("Exception in getting scan results: " + e.Message);
            //    }

            //    maxTries--;
            //}

            //if (webResponse == null)
            //{
            //    Console.Error.WriteLine("web response was null after exhausting all tries in this scan");
            //    return null;
            //}

            if (webResponse.StatusCode != HttpStatusCode.OK)
            {
                logger.Log("FoscamScount got bad status code {0} in scan result", webResponse.StatusCode.ToString());
                return null;
            }

            if (!webResponse.ContentType.Equals("text/plain"))
            {
                logger.Log("FoscamScount got bad content type {0} in scan query", webResponse.ContentType);
                return null;
            }

            string resultStr = GetHttpResponseStr(webResponse);

            //Console.WriteLine("Scan results:\n{0}", resultStr);

            return ParseScanResults(resultStr);
        }

        public bool SendWifiCredentials(FoscamScannedNetwork network, string passPhrase, string cameraIp, string username, string password)
        {
            string url = @"set_wifi.cgi?user=undefined&pwd=undefined&next_url=rebootme.htm&channel=5&mode=0";
            string enable_p = @"&enable=0";
            string ssid_p = @"&ssid=";
            string encrypt_p = "&encrypt=0";
            string authtype_p = "&authtype=0";
            string keyformat_p = "&keyformat=0";
            string defkey_p = "&defkey=0";
            string key1_p = "&key1=";
            string key2_p = "&key2=";
            string key3_p = "&key3=";
            string key4_p = "&key4=";
            string key1_bits_p = "&key1_bits=0";
            string key2_bits_p = "&key2_bits=0";
            string key3_bits_p = "&key3_bits=0";
            string key4_bits_p = "&key4_bits=0";
            string wpa_psk_p = "&wpa_psk=";

            enable_p = "&enable=1";
            ssid_p += Uri.EscapeDataString(network.ssid);
            encrypt_p = "&encrypt=" + network.security;

            if (network.security == 1)
            {
                throw new Exception("I don't deal with WEP");
                //authtype_p="&authtype="+authtype.selectedIndex;
                //keyformat_p="&keyformat="+keyformat.selectedIndex;
                //defkey_p="&defkey="+defkey.selectedIndex;
                //key1_p+=encodeURIComponent(key1.value);
                //key2_p+=encodeURIComponent(key2.value);
                //key3_p+=encodeURIComponent(key3.value);
                //key4_p+=encodeURIComponent(key4.value);
                //key1_bits_p="&key1_bits="+key1_bits.selectedIndex;
                //key2_bits_p="&key2_bits="+key2_bits.selectedIndex;
                //key3_bits_p="&key3_bits="+key3_bits.selectedIndex;
                //key4_bits_p="&key4_bits="+key4_bits.selectedIndex;
            }
            else if (network.security > 1)
            {
                wpa_psk_p += Uri.EscapeDataString(passPhrase);
            }


            string location = url + enable_p + ssid_p + encrypt_p + authtype_p + keyformat_p + defkey_p + key1_p + key2_p + key3_p + key4_p + key1_bits_p + key2_bits_p + key3_bits_p + key4_bits_p + wpa_psk_p;

            string urlToCall = String.Format("http://{0}/{1}", cameraIp, location);

            HttpWebResponse webResponse = SendHttpRequest(urlToCall, username, password);

            //we don't need the response, but lets eat it.
            GetHttpResponseStr(webResponse);

            if (webResponse.StatusCode != HttpStatusCode.OK)
            {
                logger.Log("FoscamScout got bad status code {0} while setting wifi", webResponse.StatusCode.ToString());
                return false;
            }

            return true;
        }

        internal List<string> SetCameraCredentials(string uniqueDeviceId, string username, string password)
        {
            Device device = currDeviceList.GetDevice(uniqueDeviceId);

            if (device == null)
                return new List<string>() { "Could not find the device in my current list"};

            List<string> driverParams = new List<string>() {uniqueDeviceId, username, password};

            platform.SetDeviceDriverParams(device, driverParams);

            return new List<string>() { "" };
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

        internal string GetInstructions()
        {
            return "Success! Your camera has your wireless network password. Now:<br> 1. Unplug camera from your router and from power.<br> 2. Plug-in camera to power where you want to use it<br> 3. Wait until camera finishes moving around (get coffee)<br> 4. Click the Go button.  HomeHub will find your camera on the wireless network.";
        }

        internal List<string> SendWifiCredentials(string uniqueDeviceId)
        {
            Device device = currDeviceList.GetDevice(uniqueDeviceId);

            if (device == null)
                return new List<string>() { "Could not find the device in my current list" };

            //get the username and password for the camera
            var driverParams = platform.GetDeviceDriverParams(device);

            if (driverParams.Count != 3)
                return new List<string>() { "DriverParams are in unknown state. Param count " + driverParams.Count.ToString() };

            string username = driverParams[1];
            string password = driverParams[2];

            //get the wifi credentials
            string wifiSsid = platform.GetPrivateConfSetting("WifiSsid");
            string wifiKey = platform.GetPrivateConfSetting("WifiKey");

            if (string.IsNullOrWhiteSpace(wifiSsid))
                return new List<string>() { "WifiSsid is not configured in the hub" };


            //now get scan results
            int maxScansLeft = 3;
            List<FoscamScannedNetwork> scanResults = null;

            while (maxScansLeft >= 1)
            {
                try
                {
                    scanResults = GetScanResult(device.DeviceIpAddress, username, password);
                }
                catch (Exception)
                {
                    return new List<string>() {"Error in getting scan results from the camera"};
                }

                if (scanResults.Count == 0)
                    maxScansLeft--;
                else
                    break;
            }

            if (scanResults == null || scanResults.Count == 0)
            {
                logger.Log("Skipping {0} as Wifi scan did not find any nework", device.UniqueName);

                return new List<string>() {"Wifi scan by camera did not find any network"};
            }

            //check if we the network we know was seen
            FoscamScannedNetwork targetNetwork = null;
            foreach (var network in scanResults)
            {
                if (network.ssid.Equals(wifiSsid))
                {
                    targetNetwork = network;
                    break;
                }
            }

            if (targetNetwork == null)
            {
                logger.Log("Skipping {0} as Wifi scan did not find our ssid {1}", device.UniqueName, wifiSsid);

                return new List<string>() {"Wifi scan by camera did not find our ssid " + wifiSsid};
            }

            // now configure the thing!
            bool result = false;
            try
            {
                result = SendWifiCredentials(targetNetwork, wifiKey, device.DeviceIpAddress, username, password);
            }
            catch (Exception)
            {
                return new List<string>() {"Error in configuring wifi for the camera"};
            }
            if (result)
            {
                logger.Log("Foscam configuration (seems to have) succeeded!");

                //we do not reboot, because we expect the user to power cycle the camera
                //in the process of moving it someplace else
                
                //Reboot(cameraIp, username, password);
                //System.Threading.Thread.Sleep(30 * 1000);

                return new List<string>() {""};
            }
            else
            {
                logger.Log("Configuration failed!");

                return new List<string>() {"configuration failed due to unknown reason"};
            }

        }
    }
}
