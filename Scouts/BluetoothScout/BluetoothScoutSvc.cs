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

namespace HomeOS.Hub.Scouts.BluetoothScout
{
        [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
        public class BluetoothScoutService : IBluetoothScoutContract
        {
            VLogger logger;
            BluetoothScout bluetoothScout;
            SafeServiceHost service;
            private bool disposed = false;
            public BluetoothScoutService(string baseAddress, BluetoothScout btScout, ScoutViewOfPlatform platform, VLogger logger)
            {
                this.logger = logger;
                this.bluetoothScout = btScout;

                service = new SafeServiceHost(logger, platform, this, baseAddress);

                var contract = ContractDescription.GetContract(typeof(IBluetoothScoutContract));

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
                return new List<string>() {"", bluetoothScout.GetInstructions()};
            }

        }


    [ServiceContract]
    public interface IBluetoothScoutContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetInstructions();
    }
}
