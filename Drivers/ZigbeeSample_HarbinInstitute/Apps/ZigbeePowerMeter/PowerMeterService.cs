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
using System.Collections;

namespace HomeOS.Hub.Apps.PowerMeter
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class PowerMeterService : IPowerMeterContract
    {
        protected VLogger logger;
        PowerMeter powermeter;

        public PowerMeterService(VLogger logger, PowerMeter powermeter)
        {
            this.logger = logger;
            this.powermeter = powermeter;
        }

      
        public List<string> GetReceivedMessages_get()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = powermeter.GetReceivedMessages();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetReceivedMessages: " + e);
            }
            return retVal;
        }

        public List<string> GetReceivedMessages()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = powermeter.GetReceivedMessages();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetReceivedMessages: " + e);
            }
            return retVal;
        }

        public List<string> GetPowerMeterList()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = powermeter.GetPowerMeterList();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetPowerMeterList: " + e);
            }
            return retVal;
        }

        public Hashtable GetPowerMeterStatus()
        {
            return powermeter.statusDevice;
        }

        public void SetON(String powermeterID)
        {
            powermeter.SetON(powermeterID);
           
        }

        public void SetOFF(String powermeterID)
        {
            powermeter.SetOFF(powermeterID);
           
        }
    }

     [ServiceContract]
    public interface IPowerMeterContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetReceivedMessages();
        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json, UriTemplate = "/get")]
        List<string> GetReceivedMessages_get();
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetPowerMeterList();
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        Hashtable GetPowerMeterStatus();
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        void SetON(String powermeterID);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        void SetOFF(String powermeterID);


    }
}