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

namespace HomeOS.Hub.Scouts.AxisCam
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class AxisCamScoutService : IAxisCamScoutContract
    {
        VLogger logger;
        AxisCamScout axisCamScout;
        ServiceHost service;
        private bool disposed = false;
        public AxisCamScoutService(string baseAddress, AxisCamScout acScout, VLogger logger)
        {
            this.logger = logger;
            this.axisCamScout = acScout;

            service = new ServiceHost(this, new Uri(baseAddress));

            var contract = ContractDescription.GetContract(typeof(IAxisCamScoutContract));

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
                return new List<string>() { "", axisCamScout.GetInstructions() };
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

        public List<string> AreCameraCredentialsValid(string uniqueDeviceId, string username, string password)
        {
            logger.Log("AxisCamScout:UIcalled AreCameraCredentialsValid {0} {1} {2}", uniqueDeviceId, username, password);
            try
            {
                return axisCamScout.AreCameraCredentialsValid(uniqueDeviceId, username, password);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

        public List<string> SetCameraCredentials(string uniqueDeviceId, string username, string password)
        {
            logger.Log("AxisCamScout:UIcalled SetCameraCredentials {0} {1} {2}", uniqueDeviceId, username, password);
            try
            {
                return axisCamScout.SetCameraCredentials(uniqueDeviceId, username, password);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }
    }


    [ServiceContract]
    public interface IAxisCamScoutContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetInstructions();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> AreCameraCredentialsValid(string uniqueDeviceId, string username, string password); 
        
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> SetCameraCredentials(string uniqueDeviceId, string username, string password);

    }
}
