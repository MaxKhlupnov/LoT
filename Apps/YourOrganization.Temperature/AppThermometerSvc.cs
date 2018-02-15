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

namespace HomeOS.Hub.Apps.Thermometer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class AppThermometerService : ISimplexThermometerNotifierContract
    {
        private VLogger logger;
        private AppThermometer thermometerApp;

        public AppThermometerService(AppThermometer thermometerApp, VLogger logger)
        {
            this.logger = logger;
            this.thermometerApp = thermometerApp;
        }

        public static SafeServiceHost CreateServiceHost(VLogger logger, ModuleBase moduleBase, ISimplexThermometerNotifierContract instance,
                                                     string address)
        {
            SafeServiceHost service = new SafeServiceHost(logger, moduleBase, instance, address);

            var contract = ContractDescription.GetContract(typeof(ISimplexThermometerNotifierContract));

            var webBinding = new WebHttpBinding();
            var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(service.BaseAddresses()[0]));
            webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());

            service.AddServiceEndpoint(webEndPoint);

            service.AddServiceMetadataBehavior(new ServiceMetadataBehavior());

            return service;
        }

        public double GetTemperature()
        {
            return thermometerApp.Temperature;
        }

        public string SetLEDs(double low, double high)
        {
            thermometerApp.setLEDs(low, high);
            return "";
        }
    }

    [ServiceContract]
    public interface ISimplexThermometerNotifierContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        double GetTemperature();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string SetLEDs(double low, double high);
    }

}
