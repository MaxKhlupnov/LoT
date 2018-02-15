// The purpose of this class is to discover the presence of Axis Cameras, which uses UPnP.

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

namespace HomeOS.Hub.Scouts.AxisCam
{
    public class AxisCamScout : IScout
    {
       

        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        AxisCamScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        DeviceList currentDeviceList = new DeviceList();

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new AxisCamScoutService(baseUrl + "/webapp", this, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            logger.Log("AxisCamScout initialized");

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
                if (IsAxisDevice(upnpDevice))
                {
                    var device = CreateDevice(upnpDevice);

                    currentDeviceList.InsertDevice(device);
                }
            }
        }

        private static Device CreateDevice(UPnPDevice upnpDevice)
        {
 
            string ipAddress = ExtractIpAddress(upnpDevice.PresentationURL);

            var device = new Device(upnpDevice.FriendlyName, GetUniqueName(upnpDevice.FriendlyName), ipAddress, DateTime.Now, "HomeOS.Hub.Drivers.AxisCamera");

            //intialize the parameters for this device
            device.Details.DriverParams = new List<string>() { device.UniqueName, "root", "" };

            return device;
        }

        private static string GetUniqueName(string name)
        {
            return "axiscam:" + name;
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

        private bool IsAxisDevice(UPnPDevice device)
        {
            return (device.ModelURL != null && device.ModelURL.Length > 0 && device.ModelURL.ToLower() == "http://www.axis.com/");
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

        public List<string> AreCameraCredentialsValid(string deviceId, string username, string password)
        {
            Device device = currentDeviceList.GetDevice(deviceId);

            if (device == null)
                return new List<string>() { "Could not find the device in my current list", "" };

            string requestStr = String.Format("http://{0}/axis-cgi/jpg/image.cgi", device.DeviceIpAddress);

            try
            {
                HttpWebResponse webResponse = SendHttpRequest(requestStr, username, password);

                GetHttpResponseStr(webResponse);

                switch (webResponse.StatusCode)
                {
                    case HttpStatusCode.OK:
                        return new List<string>() { "", "true" };
                    default:
                        return new List<string>() { "Got response status code " + webResponse.StatusCode, "false" };
                }
            }
            catch (Exception webEx)
            {
                if (webEx.ToString().Contains("Unauthorized"))
                    return new List<string>() { "", "false" };

                //otherwise, we don't know what is going on; propagate this exception up
                logger.Log("Got exception while checking foscam credentials: ", webEx.ToString());

                return new List<string>() { webEx.Message, "" };
            }
        }

        internal List<string> SetCameraCredentials(string uniqueDeviceId, string username, string password)
        {
            Device device = currentDeviceList.GetDevice(uniqueDeviceId);

            if (device == null)
                return new List<string>() { "Could not find the device in my current list"};

            List<string> driverParams = new List<string>() { uniqueDeviceId, username, password };

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
            return "Placeholder for helping to find your axis camera";
        }
    }
}
