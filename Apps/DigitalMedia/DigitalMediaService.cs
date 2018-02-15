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

namespace HomeOS.Hub.Apps.DigitalMedia
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class DigitalMediaService : IDigitalMediaContract
    {
        protected VLogger logger;
        DigitalMedia DigitalMedia;

        public DigitalMediaService(VLogger logger, DigitalMedia DigitalMedia)
        {
            this.logger = logger;
            this.DigitalMedia = DigitalMedia;
        }

        public List<string> SetDigitalSignal(int slot, int join, string value)
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = DigitalMedia.SetDigitalSignal(slot, join, value);
            }
            catch (Exception e)
            {
                logger.Log("Got exception in SetDigitalSignal: " + e);
            }
            return retVal;
        }

        public List<string> GetReceivedMessages()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = DigitalMedia.GetReceivedMessages();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetReceivedMessages: " + e);
            }
            return retVal;
        }

    }

     [ServiceContract]
    public interface IDigitalMediaContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
         List<string> SetDigitalSignal(int slot, int join, string value); //TODO : should be boolean type

        List<string> GetReceivedMessages();

    }
}