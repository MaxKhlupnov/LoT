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
using System.Text.RegularExpressions;
using System.IO;
using System.Timers;
using UPNPLib;
using Newtonsoft.Json;
using Newtonsoft;
using Newtonsoft.Json.Linq;

namespace HomeOS.Hub.Scouts.HueBridge
{
    public class HueBridgeScout : IScout
    {


        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        HueBridgeScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        DeviceList currentDeviceList = new DeviceList();

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new HueBridgeScoutService(baseUrl + "/webapp", this, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("HueBridgeScout initialized");

            //create a time that fires ScanNow() periodically
            var scanTimer = new System.Timers.Timer(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * 1000);
            scanTimer.Enabled = true;
            scanTimer.Elapsed += new System.Timers.ElapsedEventHandler(ScanNow);
        }

        private void ScanNow(object source, ElapsedEventArgs e)
        {
            FillList();

            currentDeviceList.RemoveOldDevices(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * ScoutHelper.DefaultNumPeriodsToForgetDevice);

            platform.ProcessNewDiscoveryResults(currentDeviceList.GetClonedList());
        }

        public List<Device> GetDevices()
        {
            //lets look again, since the user asked us to
            FillList();

            return currentDeviceList.GetClonedList();
        }

        private void FillList()
        {
            //var finder = new UPnPDeviceFinder();
            //var devices = finder.FindByType("upnp:rootdevice", 0);

            var devices = ScoutHelper.UpnpGetDevices();

            foreach (UPnPDevice upnpDevice in devices)
            {
                if (IsHueBridge(upnpDevice))
                {
                    var device = CreateDevice(upnpDevice);

                    currentDeviceList.InsertDevice(device);
                }
            }
        }

        private static Device CreateDevice(UPnPDevice upnpDevice)
        {

            string ipAddress = ExtractIpAddress(upnpDevice.PresentationURL);

            var device = new Device(GetUniqueName(upnpDevice), GetUniqueName(upnpDevice), ipAddress, DateTime.Now, "HomeOS.Hub.Drivers.HueBridge");

            //intialize the parameters for this device
            device.Details.DriverParams = new List<string>() { device.UniqueName, "root", "" };

            return device;
        }

        private static string GetUniqueName(UPnPDevice device)
        {
            return "huebridge:" + device.SerialNumber;
        }

        // The presentation url string is of the form "http://172.0.193.5/". This routine
        // extracts the ipaddress part.
        private static string ExtractIpAddress(string presentationURL)
        {
            Regex re = new Regex(@"http://(?<ipaddr>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})/?");
            var m = re.Match(presentationURL);
            if (m.Success)
            {
                return m.Result("${ipaddr}");
            }
            else
            {
                return "";
            }
        }

        private bool IsHueBridge(UPnPDevice device)
        {
            return (device.ModelURL != null && device.ModelURL.Length > 0 && device.ModelURL.ToLower() == "http://www.meethue.com/");
        }

        private HttpWebResponse SendHttpRequest(string requestUrl, string method, string jsonData, int timeout = 10000)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(requestUrl);
            webRequest.Method = method;
            webRequest.ContentType = "text/json";
            webRequest.Timeout = timeout;

            logger.Log("HTTP {0} to hue: {1} {2}", method, requestUrl, jsonData);

            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
               streamWriter.Write(jsonData);
            }

            return (HttpWebResponse)webRequest.GetResponse();
        }


        internal List<string> SetAPIUsername(string uniqueDeviceId, string username)
        {
            Device device = currentDeviceList.GetDevice(uniqueDeviceId);

            if (device == null)
                return new List<string>() { "Could not find the device in my current list" };

            string url = String.Format("http://{0}/api", device.DeviceIpAddress);

            string json = @"{ ""devicetype"": ""homeos"", ""username"": """ + username + @""" }";

            var response = SendHttpRequest(url, "POST", json);

            string responseText = @"{""error"": {""description"": ""did not get a response from the bridge""}}";

            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                responseText = streamReader.ReadToEnd();

                //remove the first '[' and the last ']'
                responseText = responseText.Substring(1, responseText.Length - 2);
            }


            JObject jobject = (JObject) JsonConvert.DeserializeObject(responseText);

            //did we get an error?
            foreach (var child in jobject.Children())
            {
                if (child.Type == JTokenType.Property)
                {
                    string name = ((JProperty)child).Name;

                    if (name.ToString().Equals("error"))
                    {
                        return new List<string>() { jobject["error"]["description"].ToString() };
                    }
                }
            }

            string usernameInResponse = jobject["success"]["username"].ToString();

            if (!usernameInResponse.Equals(username))
            {
                return new List<string>() { "Mismatch in username. Input = " + username + " Output = " + usernameInResponse};
            }

            List<string> driverParams = new List<string>() { uniqueDeviceId, username};
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
            return "Placeholder for helping to find your hue bridge";
        }
    }
}
