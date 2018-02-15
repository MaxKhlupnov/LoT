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

namespace HomeOS.Hub.Apps.Doorjamb
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class DoorjambService : ISimplexDoorjambContract
    {
        /*public static ServiceHost CreateServiceHost(ISimplexDoorjambContract instance,
                                                     Uri baseAddress)*/
        public static SafeServiceHost CreateServiceHost(VLogger logger, ModuleBase moduleBase, ISimplexDoorjambContract instance,
                                                    string address)
        {

            SafeServiceHost service = new SafeServiceHost(logger, moduleBase, instance, address);

            var contract = ContractDescription.GetContract(typeof(ISimplexDoorjambContract));

            var webBinding = new WebHttpBinding();
            var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(service.BaseAddresses()[0]));
            webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());

            service.AddServiceEndpoint(webEndPoint);

            service.AddServiceMetadataBehavior(new ServiceMetadataBehavior());
            //service.Description.Behaviors.Add(new ServiceMetadataBehavior());
            //service.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");

            return service;
        }

        protected VLogger logger;
        Doorjamb Doorjamb;

        public DoorjambService(VLogger logger, Doorjamb Doorjamb)
        {
            this.logger = logger;
            this.Doorjamb = Doorjamb;
        }

        public List<string> GetAlerts(string mode, string time, int numAlerts)
        {
            return this.Doorjamb.GetAlerts();
        }
        public List<string> GetReceivedMessages()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = Doorjamb.GetReceivedMessages();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetReceivedMessages: " + e);
            }
            return retVal;
        }

        public List<string> GetReceivedMessages_get()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = Doorjamb.GetReceivedMessages();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetReceivedMessages: " + e);
            }
            return retVal;
        }
    }

    [ServiceContract]
    public interface ISimplexDoorjambContract
    {
        
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetReceivedMessages();
        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json, UriTemplate = "/get")]
        List<string> GetReceivedMessages_get();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetAlerts(string mode, string timeReference, int numAlerts);
         


        /**
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string GetSettings();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string SetSettings(string mode, int startHourMin, int endHourMin, int suppressionSeconds);


        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetAlerts(string mode, string timeReference, int numAlerts);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string SetAcknowledgment(string timeReference, bool acknowledgment);


        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetMonitoredDevices();
         * **/

    }
}