using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Threading;
using System.Xml;

namespace HomeOS.Hub.Drivers.OpenWeatherMap
{
    /// <summary>
    /// A driver to fetch data from open weather map
    /// It is not complete yet. It was written mainly to serve as an example of a driver based on communication with the cloud rather than devices
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Drivers.OpenWeatherMap")]
    public class DriverOwm :  ModuleBase
    {

        public class WeatherData
        {
            public DateTime LastUpdateTime { get; set; }
 
            public double CurTemp_C { get; set; }
            public double MinTemp_C { get; set; }
            public double MaxTemp_C { get; set; }

            public DateTime SunriseTime { get; set; }
            public DateTime SunsetTime { get; set; }

            public string PrecipitationMode { get; set; }
            public string CloudsName { get; set; }
            public string WeatherValue { get; set; }

            // we don't do these fields yet
            //public int Humidity_percent { get; set; }
            //public int Pressure_hPa { get; set; }
            //public float WindSpeed { get; set; }
            //public float WindDirection { get; set; }
            //public float Clouds { get; set; }
            //public float PrecipitationValue { get; set; }
            //public string PrecipitationMode { get; set; }
            //public string WeatherValue { get; set; }

            public WeatherData()
            {
                LastUpdateTime = DateTime.MinValue;

                CurTemp_C = 0;
                MinTemp_C = 0;
                MaxTemp_C = 0;

                SunriseTime = DateTime.MinValue;
                SunsetTime = DateTime.MaxValue;

                PrecipitationMode = "unknown";
                CloudsName = "unknown";
                WeatherValue = "unknown";
            }
        }


        //frequency with which to fetch weather data
        const int WeatherFetchPeriodMs = 15 * 60 * 1000; //15 minutes

        const string OpenWeatherMapUrl = "http://api.openweathermap.org/data/2.5/";

        Port weatherPort;

        string appId;
        string lattitude;
        string longitude;

        WeatherData latestWeather;

        Timer timer;

        public override void Start()
        {
            logger.Log("Started: {0}", ToString());

            //sanity check and recover the parameters
            if (moduleInfo.Args().Count() != 4)
            {
                logger.Log("DriverOwm: incorrect number of parameters: {0}", moduleInfo.Args().Count().ToString());
                return;
            }

            string deviceId = moduleInfo.Args()[0];
            appId = moduleInfo.Args()[1];
            lattitude = moduleInfo.Args()[2];
            longitude = moduleInfo.Args()[3];

            latestWeather = new WeatherData();

            //.................instantiate the port
            VPortInfo portInfo = GetPortInfoFromPlatform("owm-" + deviceId);
            weatherPort = InitPort(portInfo);

            // ..... initialize the list of roles we are going to export and bind to the role
            List<VRole> listRole = new List<VRole>() { RoleWeather.Instance };
            BindRoles(weatherPort, listRole);

            //.................register the port after the binding is complete
            RegisterPortWithPlatform(weatherPort);

            timer = new Timer(GetWeather, null, 0, WeatherFetchPeriodMs);
        }

        private void GetWeather(object state)
        {
            string requestUri = null;

            try
            {
                requestUri = String.Format("{0}weather?mode=xml&units=metric&appid={1}&lat={2}&lon={3}", OpenWeatherMapUrl, appId, lattitude, longitude);

                XmlDocument xmlDoc = new XmlDocument();
                XmlReader xmlReader = XmlReader.Create(requestUri);
                xmlDoc.Load(xmlReader);

                WeatherData curWeatherData = new WeatherData();

                // get sun stuff
                XmlElement xmlSun = (XmlElement) xmlDoc.GetElementsByTagName("sun")[0];

                curWeatherData.SunriseTime = DateTime.Parse(xmlSun.GetAttribute("rise"));
                curWeatherData.SunsetTime = DateTime.Parse(xmlSun.GetAttribute("set"));

                // get temperature stuff
                XmlElement xmlTemp = (XmlElement)xmlDoc.GetElementsByTagName("temperature")[0];

                string units = xmlTemp.GetAttribute("unit");

                if (!units.Equals("celsius"))
                    throw new Exception("Unexpected units in temperature data: " + units);

                curWeatherData.CurTemp_C = double.Parse(xmlTemp.GetAttribute("value"));
                curWeatherData.MinTemp_C = double.Parse(xmlTemp.GetAttribute("min"));
                curWeatherData.MaxTemp_C = double.Parse(xmlTemp.GetAttribute("max"));

                // get cloud stuff
                XmlElement xmlClouds = (XmlElement)xmlDoc.GetElementsByTagName("clouds")[0];
                curWeatherData.CloudsName = xmlClouds.GetAttribute("name");

                // get precipitation stuff
                XmlElement xmlPrecipitation = (XmlElement)xmlDoc.GetElementsByTagName("precipitation")[0];
                curWeatherData.PrecipitationMode = xmlPrecipitation.GetAttribute("mode");

                // get precipitation stuff
                XmlElement xmlWeather = (XmlElement)xmlDoc.GetElementsByTagName("weather")[0];
                curWeatherData.WeatherValue = xmlWeather.GetAttribute("value");

                // get the update time
                XmlElement xmlLastUpdate = (XmlElement)xmlDoc.GetElementsByTagName("lastupdate")[0];
                curWeatherData.LastUpdateTime = DateTime.Parse(xmlLastUpdate.GetAttribute("value"));

                xmlReader.Close();

                InstallCurrWeather(curWeatherData);

            }
            catch (Exception e)
            {
                logger.Log("Exception while fetching and parsing weather using URI {0}: {1}", requestUri, e.ToString());
            }
        }

        private void InstallCurrWeather(WeatherData newData)
        {
            lock (latestWeather)
            {
                latestWeather = newData;
            }
        }

        public override void Stop()
        {
            logger.Log("Stop() at {0}", ToString());

            if (timer != null)
                timer.Dispose();

            //if (imageServer != null)
            //   imageServer.Dispose();
        }



        /// <summary>
        /// The demultiplexing routing for incoming operations
        /// </summary>
        /// <param name="message"></param>
        public override IList<VParamType> OnInvoke(string roleName, String opName, IList<VParamType> args)
        {

            if (!roleName.Equals(RoleWeather.RoleName))
            {
                logger.Log("Invalid role {0} in OnInvoke", roleName);
                return null;
            }

            lock (latestWeather)
            {
                if (latestWeather == null)
                {
                    logger.Log("Error: Someone asked for weather when latestWeather is null");
                    return null;
                }

                switch (opName.ToLower())
                {
                    case RoleWeather.OpGetWeather:
                        logger.Log("Got WeatherRequest");

                        return new List<VParamType>() { new ParamType(latestWeather.WeatherValue) };

                    case RoleWeather.OpGetTemperature:
                        logger.Log("Got temperature request");

                        return new List<VParamType>() { new ParamType(latestWeather.CurTemp_C), new ParamType(latestWeather.MinTemp_C), new ParamType(latestWeather.MaxTemp_C) };

                    case RoleWeather.OpGetPrecipitation:
                        logger.Log("Got Precipitation Request");

                        return new List<VParamType>() { new ParamType(latestWeather.PrecipitationMode) };


                    default:
                        logger.Log("Invalid operation: {0}", opName);
                        return null;
                }
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        ///        the dummy driver does not care about other ports in the system
        /// </summary>
        public override void PortRegistered(VPort port) {}

        /// <summary>
        ///  Called when a new port is deregistered with the platform
        ///        the dummy driver does not care about other ports in the system
        /// </summary>
        public override void PortDeregistered(VPort port) { }
    }
}