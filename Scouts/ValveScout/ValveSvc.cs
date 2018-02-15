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

namespace HomeOS.Hub.Scouts.Valve
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ValveScoutService : IValveScoutContract
    {
        VLogger logger;
        ValveScout valveScout;
        SafeServiceHost service;
        private bool disposed = false;
        public ValveScoutService(string baseAddress, ValveScout gScout, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.logger = logger;
            this.valveScout = gScout;

            service = new SafeServiceHost(logger, platform, this, baseAddress);

            var contract = ContractDescription.GetContract(typeof(IValveScoutContract));

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
            logger.Log("ValveScout:UIcalled GetInstructions");
            try
            {
                return new List<string>() { "", valveScout.GetInstructions() };
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }


    }

    [ServiceContract]
    public interface IValveScoutContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetInstructions();

    }
}
