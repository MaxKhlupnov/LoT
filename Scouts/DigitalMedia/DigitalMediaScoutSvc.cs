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
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.DeviceScout;



namespace HomeOS.Hub.Scouts.DigitalMedia
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class DigitalMediaScoutService : IDigitalMediaScoutContract
    {
        VLogger logger;
        DigitalMediaScout digitalMediaScout;
        SafeServiceHost service;
        private bool disposed = false;

        public DigitalMediaScoutService(string baseAddress, DigitalMediaScout wcScout, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.logger = logger;
            this.digitalMediaScout = wcScout;

            service = new SafeServiceHost(logger, platform, this, baseAddress);

            var contract = ContractDescription.GetContract(typeof(IDigitalMediaScoutContract));

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
            return new List<string>() { "", digitalMediaScout.GetInstructions() };
        }

    }

    [ServiceContract]
    public interface IDigitalMediaScoutContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetInstructions();
    }
}
