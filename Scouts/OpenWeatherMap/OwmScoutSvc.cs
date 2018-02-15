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

namespace HomeOS.Hub.Scouts.OpenWeatherMap
{
        [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
        public class OwmScoutService : IOwmScoutContract
        {

            VLogger logger;
            OwmScout owmScout;
            SafeServiceHost service;
            private bool disposed = false;

            public OwmScoutService(string baseAddress, OwmScout owmScout, ScoutViewOfPlatform platform, VLogger logger)
            {
                this.logger = logger;
                this.owmScout = owmScout;

                service = new SafeServiceHost(logger, platform, this, baseAddress);

                var contract = ContractDescription.GetContract(typeof(IOwmScoutContract));

                var webBinding = new WebHttpBinding();
                var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(baseAddress));
                webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());

                service.AddServiceEndpoint(webEndPoint);

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
                            logger.Log("Exception in disposing "+this.ToString()+". "+e);
                        }
                    }

                    disposed = true;
                }
            }

            public List<string> GetInstructions()
            {
                return new List<string>() {"", owmScout.GetInstructions()};
            }

            public List<string> GetAppId(string uniqueDeviceId)
            {
                try
                {
                    return new List<string>() { "", owmScout.GetAppId(uniqueDeviceId) };
                }
                catch (Exception e)
                {
                    logger.Log("Got exception in GetAppId: " + e);
                    return new List<string>() { e.Message };
                }
            }

            public List<string> SetAppId(string uniqueDeviceId, string appId)
            {
                try
                {
                    return new List<string>() {owmScout.SetAppId(uniqueDeviceId, appId)};
                }
                catch (Exception e)
                {
                    logger.Log("Got exception in SetAppId: " + e);
                    return new List<string>() { e.Message };
                }
            }

            public List<string> QueryLocation(string uniqueDeviceId, string locationHint)
            {
                try
                {
                    var result = owmScout.QueryLocation(uniqueDeviceId, locationHint);

                    //add the error code as the first element
                    result.Insert(0, "");

                    return result;
                }
                catch (Exception e)
                {
                    logger.Log("Got exception in QueryLocation({0}): {3}", uniqueDeviceId, locationHint, e.ToString());
                    return new List<string>() { e.Message };
                }
            }

            public List<string> SetLocation(string uniqueDeviceId, string location)
            {
                try
                {
                    owmScout.SetLocation(uniqueDeviceId, location);

                    return new List<string>() { "" };
                }
                catch (Exception e)
                {
                    logger.Log("Got exception in SetLocation({0}, {1}): {2}", uniqueDeviceId, location, e.ToString());
                    return new List<string>() { e.Message };
                }
            }
        }


    [ServiceContract]
    public interface IOwmScoutContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetInstructions();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetAppId(string uniqueDeviceId);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> SetAppId(string uniqueDeviceId, string appId);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> QueryLocation(string uniqueDeviceId, string locationHint);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> SetLocation(string uniqueDeviceId, string location);

    }
}
