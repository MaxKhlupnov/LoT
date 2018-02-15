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
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Scouts.Gadgeteer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class GadgeteerScoutService : IGadgeteerScoutContract
    {
        VLogger logger;
        GadgeteerScout gadgeteerScout;
        SafeServiceHost service;
        private bool disposed = false;
        public GadgeteerScoutService(string baseAddress, GadgeteerScout gScout, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.logger = logger;
            this.gadgeteerScout = gScout;

            service = new SafeServiceHost(logger, platform, this, baseAddress);

            var contract = ContractDescription.GetContract(typeof(IGadgeteerScoutContract));

            var webBinding = new WebHttpBinding();
            var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(baseAddress));
            webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());

            service.AddServiceEndpoint(webEndPoint);

            //service.Description.Behaviors.Add(new ServiceMetadataBehavior());
            //service.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
            service.AddServiceMetadataBehavior(new ServiceMetadataBehavior());

            service.Open();
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
                    try
                    {
                        service.Close();
                    }
                    catch (Exception e)
                    {
                        logger.Log("Exception in disposing " + this.ToString() + ". " + e);
                    }
                }

                disposed = true;
            }
        }


        public List<string> GetInstructions()
        {
            logger.Log("GadgeteerScout:UIcalled GetInstructions");
            try
            {
                return new List<string>() { "", gadgeteerScout.GetInstructions() };
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

        public List<string> SendWifiCredentials(string uniqueDeviceId, string authCode)
        {
            logger.Log("GadgeteerScout:UIcalled SendWifiCredentials {0} {1}", uniqueDeviceId, authCode);
            try
            {
                return gadgeteerScout.SendWifiCredentials(uniqueDeviceId, authCode);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

        public List<string> IsDeviceOnHostedNetwork(string uniqueDeviceId)
        {
            logger.Log("GadgeteerScout:UIcalled IsDeviceOnHostedNetwork {0}", uniqueDeviceId);
            try
            {
                return gadgeteerScout.IsDeviceOnHostedNetwork(uniqueDeviceId);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

        public List<string> IsDeviceOnHomeWifi(string uniqueDeviceId)
        {
            logger.Log("GadgeteerScout:UIcalled IsDeviceOnHomeWifi {0}", uniqueDeviceId);
            try
            {
                return gadgeteerScout.IsDeviceOnHomeWifi(uniqueDeviceId);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

    }


    [ServiceContract]
    public interface IGadgeteerScoutContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetInstructions();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> SendWifiCredentials(string uniqueDeviceId, string authCode);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> IsDeviceOnHostedNetwork(string uniqueDeviceId);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> IsDeviceOnHomeWifi(string uniqueDeviceId);

    }
}
