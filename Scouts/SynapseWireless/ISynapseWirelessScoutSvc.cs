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

namespace HomeOS.Hub.Scouts.SynapseWireless
{
        [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
        public class SynapseWirelessScoutService : ISynapseWirelessScoutContract
        {
            VLogger logger;
            SynapseWirelessScout webCamScout;
            ServiceHost service;
            private bool disposed = false;
            public SynapseWirelessScoutService(string baseAddress, SynapseWirelessScout wcScout, VLogger logger)
            {
                this.logger = logger;
                this.webCamScout = wcScout;

                service = new ServiceHost(this, new Uri(baseAddress));

                var contract = ContractDescription.GetContract(typeof(ISynapseWirelessScoutContract));

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
                            logger.Log("Exception in disposing "+this.ToString()+". "+e);
                        }
                    }

                    disposed = true;
                }
            }


            public List<string> GetInstructions()
            {
                return new List<string>() {"", webCamScout.GetInstructions()};
            }

        }


    [ServiceContract]
    public interface ISynapseWirelessScoutContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetInstructions();
    }
}
