using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Apps.Weather
{

    /// <summary>
    /// A dummy a module that 
    /// 1. sends ping messages to all active dummy ports
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Apps.Weather")]
    public class Weather :  ModuleBase
    {
        //list of accessible dummy ports in the system
        List<VPort> accessibleWeatherPorts;

        private SafeServiceHost serviceHost;

        private WebFileServer appServer;

        public override void Start()
        {
            logger.Log("Started: {0} ", ToString());

            WeatherService dummyService = new WeatherService(logger, this);
            serviceHost = new SafeServiceHost(logger,typeof(IWeatherContract), dummyService , this, Constants.AjaxSuffix, moduleInfo.BaseURL());
            serviceHost.Open();

            appServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
            
 
            //........... instantiate the list of other ports that we are interested in
            accessibleWeatherPorts = new List<VPort>();

            //..... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();

            if (allPortsList != null)
                ProcessAllPortsList(allPortsList);
        }

        public override void Stop()
        {
            logger.Log("AppWeather clean up");

            if (serviceHost != null)
                serviceHost.Close();

            if (appServer != null)
                appServer.Dispose();
        }

        private void ProcessAllPortsList(IList<VPort> portList)
        {
            foreach (VPort port in portList)
            {
                PortRegistered(port);
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        public override void PortRegistered(VPort port)
        {

            logger.Log("{0} got registeration notification for {1}", ToString(), port.ToString());

            lock (this)
            {
                if (!accessibleWeatherPorts.Contains(port) && 
                    Role.ContainsRole(port, RoleWeather.RoleName) && 
                    GetCapabilityFromPlatform(port) != null)
                {
                    accessibleWeatherPorts.Add(port);

                    logger.Log("{0} added port {1}", this.ToString(), port.ToString());
                }
            }
        }

        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (accessibleWeatherPorts.Contains(port))
                {
                    accessibleWeatherPorts.Remove(port);
                    logger.Log("{0} deregistered port {1}", this.ToString(), port.GetInfo().ModuleFacingName());
                }
            }
        }

        public List<string> GetWeatherAsString()
        {
            List<string> retList = new List<string>();

            if (accessibleWeatherPorts.Count > 0)
            {
                for (int i=0; i < accessibleWeatherPorts.Count; i++) 
                {
                    retList.Add("Weather info from " + accessibleWeatherPorts[i].GetInfo().GetFriendlyName());

                    //retList.Add(GetLastUpdated(accessibleWeatherPorts[i]));

                    retList.Add(GetWeatherValue(accessibleWeatherPorts[i]));

                    retList.Add(GetTemperatures(accessibleWeatherPorts[i]));

                    retList.Add(GetPrecipitation(accessibleWeatherPorts[i]));

                retList.Add("");
                }
            }
            else
            {
                retList.Add("No accessible weather devices found");
            }

            return retList;
        }

        private string GetWeatherValue(VPort port)
        {
            IList<VParamType> retVals = Invoke(port, RoleWeather.Instance, RoleWeather.OpGetWeather);

            if (retVals[0].Maintype() != (int)ParamType.SimpleType.error)
            {
                return (string) retVals[0].Value();
            }
            else
            {
                return "Couldn't get weather value. Error: " + retVals[0].Value();
            }
        }

        private string GetTemperatures(VPort port)
        {
            IList<VParamType> retVals = Invoke(port, RoleWeather.Instance, RoleWeather.OpGetTemperature);

            if (retVals[0].Maintype() != (int)ParamType.SimpleType.error)
            {
                double curTemp = (double) retVals[0].Value();
                double minTemp = (double) retVals[1].Value();
                double maxTemp = (double) retVals[2].Value();

                return String.Format("Temperature {0} C. Min: {1} C. Max: {2} C", curTemp, minTemp, maxTemp);
            }
            else
            {
                return "Couldn't get temperature. Error: " + retVals[0].Value();
            }
        }

        private string GetPrecipitation(VPort port)
        {
            IList<VParamType> retVals = Invoke(port, RoleWeather.Instance, RoleWeather.OpGetPrecipitation);

            if (retVals[0].Maintype() != (int)ParamType.SimpleType.error)
            {
                return "Precipitation: " + (string)retVals[0].Value();
            }
            else
            {
                return "Couldn't get weather value. Error: " + retVals[0].Value();
            }
        }


    }
}