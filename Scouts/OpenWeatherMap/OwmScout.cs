using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;
using System.Xml;

namespace HomeOS.Hub.Scouts.OpenWeatherMap
{
    public class OwmScout : IScout
    {
        //we got this AppId by registering on openweathermap.org
        //username: msrlot, email: lab-of-things@microsoft.com
        string DefaultAppId = "c8ef59f1e86a48200bd7eb5b6a0c59bd";

        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        OwmScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        Device device = null;

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new OwmScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            //initialize the device we'll use
            device = new Device("OpenWeatherMap", UniqueDeviceId(), "", DateTime.Now, "HomeOS.Hub.Drivers.OpenWeatherMap");

            // the parameters are: uniqueName, appid, lattitude, longitude
            device.Details.DriverParams = new List<string>() { device.UniqueName, DefaultAppId, "", "" };

            logger.Log("DummyScout initialized");
        }

        private string UniqueDeviceId() 
        {
            return "OpenWeatherMapDevice";
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
            if (device != null)
                return new List<Device>() { device };
            else
                return new List<Device>();
        }


        internal string GetInstructions()
        {
            return "Placeholder for instructions for openweathermap scout";
        }

        internal string GetAppId(string uniqueDeviceId)
        {
            //the second parameter, which reflects the appid
            return device.Details.DriverParams[1];
        }

        internal string SetAppId(string uniqueDeviceId, string appId)
        {
            if (appId.Length != 32)
                return "Appears to be bad API key with length " + appId.Length + ". Expected 32 letters.";

            //change the second parameter, which reflects the appid
            device.Details.DriverParams[1] = appId;

            platform.SetDeviceDriverParams(device, device.Details.DriverParams);

            return "";
        }

        internal void SetLocation(string uniqueDeviceId, string location)
        {
            //location packing format: "cityname,country | lat,lon"

            string[] split1 = location.Split('|');

            string[] latlon = split1[1].Split(',');
            
            //change the third and fourth paramters, which reflect location
            device.Details.DriverParams[2] = float.Parse(latlon[0]).ToString();   //going through the Parse / ToString() ringer will clean out whitespace
            device.Details.DriverParams[3] = float.Parse(latlon[1]).ToString();

            platform.SetDeviceDriverParams(device, device.Details.DriverParams);
        }

        //returns a list of cities that match the hint.
        //packing format: "cityname,country | lat,lon
        internal List<string> QueryLocation(string uniqueDeviceId, string locationHint)
        {
            if (String.IsNullOrWhiteSpace(locationHint))
                throw new Exception("QueryLocation called with empty locationHint");

            string requestUri = String.Format("http://api.openweathermap.org/data/2.5/find?q={0}&type=like&mode=xml", Uri.EscapeDataString(locationHint));

            XmlDocument xmlDoc = new XmlDocument();
            XmlReader xmlReader = XmlReader.Create(requestUri);
            xmlDoc.Load(xmlReader);

            var xmlCities = xmlDoc.GetElementsByTagName("city");

            List<string> cityList = new List<string>();

            foreach (XmlElement xmlCity in xmlCities)
            {
                string cityName = xmlCity.GetAttribute("name");

                XmlElement xmlCountry = (XmlElement)xmlCity.GetElementsByTagName("country")[0];
                string country = xmlCountry.InnerText;

                XmlElement xmlCoord = (XmlElement) xmlCity.GetElementsByTagName("coord")[0];
                string lat = xmlCoord.GetAttribute("lat");
                string lon = xmlCoord.GetAttribute("lon");

                cityList.Add(String.Format("{0},{1} | {2},{3}", cityName, country, lat, lon));
            }

            return cityList;
        }
    }
}
