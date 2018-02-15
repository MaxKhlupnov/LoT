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

namespace HomeOS.Hub.Scouts.Foscam
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class FoscamScoutService : IFoscamScoutContract
    {
        VLogger logger;
        FoscamScout foscamScout;
        SafeServiceHost service;
        private bool disposed = false;

        public FoscamScoutService(string baseAddress, FoscamScout wcScout,  ScoutViewOfPlatform platform, VLogger logger)
        {
            this.logger = logger;
            this.foscamScout = wcScout;

            service = new SafeServiceHost(logger, platform, this, baseAddress);

            var contract = ContractDescription.GetContract(typeof(IFoscamScoutContract));

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
            logger.Log("FoscamScout:UIcalled GetInstructions");
            try
            {
                return new List<string>() { "", foscamScout.GetInstructions() };
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

        public List<string> AreCameraCredentialsValid(string deviceId, string username, string password)
        {
            logger.Log("FoscamScout:UIcalled AreCameraCredentialsValid {0} {1} {2}", deviceId, username, password);
            try
            {
                return foscamScout.AreCameraCredentialsValid(deviceId, username, password);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

        public List<string> SetCameraCredentials(string deviceId, string username, string password)
        {
            logger.Log("FoscamScout:UIcalled SetCameraCredentials {0} {1} {2}", deviceId, username, password);
            try
            {
                return foscamScout.SetCameraCredentials(deviceId, username, password);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

        public List<string> IsDeviceOnWifi(string deviceId)
        {
            logger.Log("FoscamScout:UIcalled IsDeviceOnWifi {0}", deviceId);
            try
            {
                return foscamScout.IsDeviceOnWifi(deviceId);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

        public List<string> SendWifiCredentials(string deviceId)
        {
            logger.Log("FoscamScout:UIcalled SendWifiCredentials {0}", deviceId);
            try
            {
                return foscamScout.SendWifiCredentials(deviceId);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }
        }

    }


    [ServiceContract]
    public interface IFoscamScoutContract
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

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> IsDeviceOnWifi(string uniqueDeviceId);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> SendWifiCredentials(string uniqueDeviceId);

    }
}
