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

namespace HomeOS.Hub.Apps.Dummy
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class DummyService : IDummyContract
    {
        protected VLogger logger;
        Dummy Dummy;

        public DummyService(VLogger logger, Dummy Dummy)
        {
            this.logger = logger;
            this.Dummy = Dummy;
        }

        public List<string> GetReceivedMessages()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = Dummy.GetReceivedMessages();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetReceivedMessages: " + e);
            }
            return retVal;
        }

    }

     [ServiceContract]
    public interface IDummyContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetReceivedMessages();


    }
}