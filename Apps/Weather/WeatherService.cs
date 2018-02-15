using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;
using System.ServiceModel.Web;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Apps.Weather
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class WeatherService : IWeatherContract
    {
        protected VLogger logger;
        Weather weather;

        public WeatherService(VLogger logger, Weather weather)
        {
            this.logger = logger;
            this.weather = weather;
        }

        public List<string> GetWeather()
        {
            try
            {
                var retList = weather.GetWeatherAsString();

                retList.Insert(0, "");

                return retList;
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetReceivedMessages: " + e);
                return new List<string>() { e.Message }; 
            }
        }
    }

    [ServiceContract]
    public interface IWeatherContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetWeather();

    }
}