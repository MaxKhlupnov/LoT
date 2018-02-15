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

namespace HomeOS.Hub.Scouts.HueBridge
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class HueBridgeScoutService : IHueBridgeScoutContract
    {
        VLogger logger;
        HueBridgeScout hueBridgeScout;
        ServiceHost service;
        private bool disposed = false;

        public HueBridgeScoutService(string baseAddress, HueBridgeScout hbScout, VLogger logger)
        {
            this.logger = logger;
            this.hueBridgeScout = hbScout;

            service = new ServiceHost(this, new Uri(baseAddress));

            var contract = ContractDescription.GetContract(typeof(IHueBridgeScoutContract));

            var webBinding = new WebHttpBinding();
            var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(baseAddress));
            webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());

            service.AddServiceEndpoint(webEndPoint);

            service.Description.Behaviors.Add(new ServiceMetadataBehavior());
            service.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");

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
            logger.Log("AxisCamScout:UIcalled GetInstructions");
            try
            {
                return new List<string>() { "", hueBridgeScout.GetInstructions() };
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

        public List<string> SetAPIUsername(string uniqueDeviceId, string username)
        {
            logger.Log("HueBridgeScout:UIcalled SetAPIUsername {0} {1}", uniqueDeviceId, username);

            try
            {
                return hueBridgeScout.SetAPIUsername(uniqueDeviceId, username);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }
    }


    [ServiceContract]
    public interface IHueBridgeScoutContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetInstructions();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> SetAPIUsername(string uniqueDeviceId, string username);

    }
}
