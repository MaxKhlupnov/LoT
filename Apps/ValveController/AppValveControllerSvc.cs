using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace HomeOS.Hub.Apps.ValveController
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class AppValveControllerService : ISimplexValveControllerNotifierContract
    {
        private VLogger logger;
        private AppValveController app;

        public AppValveControllerService(AppValveController app, VLogger logger)
        {
            this.logger = logger;
            this.app = app;
        }

        public static SafeServiceHost CreateServiceHost(VLogger logger, ModuleBase moduleBase, ISimplexValveControllerNotifierContract instance,
                                                     string address)
        {
            SafeServiceHost service = new SafeServiceHost(logger, moduleBase, instance, address);

            var contract = ContractDescription.GetContract(typeof(ISimplexValveControllerNotifierContract));

            var webBinding = new WebHttpBinding();
            var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(service.BaseAddresses()[0]));
            webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());

            service.AddServiceEndpoint(webEndPoint);

            service.AddServiceMetadataBehavior(new ServiceMetadataBehavior());

            return service;
        }

        public string SendValveAddress(int value)
        {
            app.SendValveAddress();
            return "";
        }

        public string DoneValveAddress(int value)
        {
            app.DoneValveAddresses();
            return "";
        }

        public string ResetValveAddress(int value)
        {
            app.ResetValveAddresses();
            return "";
        }

        public string SetOneValve(int valve, double percentage)
        {
            app.SetOneValve(valve, percentage);
            return "";
        }

        public string SetAllValves(double percentage)
        {
            app.SetAllValves(percentage);
            return "";
        }


        /// <summary>
        /// Gets the total number of valves (data can be null)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int GetTotalValveNumber(int data)
        {
            return app.GetTotalValveNumber();
        }
    }

    [ServiceContract]
    public interface ISimplexValveControllerNotifierContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string SendValveAddress(int value);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string DoneValveAddress(int valve);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string SetOneValve(int valve, double percentage);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string SetAllValves(double percentage);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string ResetValveAddress(int valve);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        int GetTotalValveNumber(int data);
    }

}
