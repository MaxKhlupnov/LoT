using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;
using System.ServiceModel.Web;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common;
using System.Threading.Tasks;

namespace HomeOS.Hub.Apps.BluetoothApp {
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class BluetoothAppService : IBluetoothAppContract {
        protected VLogger logger;
        BluetoothApp Dummy;

        public BluetoothAppService(VLogger logger, BluetoothApp Dummy) {
            this.logger = logger;
            this.Dummy = Dummy;
        }

        public List<string> GetConnectedDevices() {
            List<string> retVal = new List<string>();
            try {
                retVal = Dummy.GetConnectedDevices();
            } catch (Exception e) {
                logger.Log("Got exception in GetPairedDevices: " + e);
            }
            return retVal;
        }

        public bool SendMessage(string message, string[] check) {
            bool retVal = false;
            try {
                retVal = Dummy.SendMessage(message, check);
            } catch (Exception e) {
                logger.Log("Got exception in SendMessage: " + e);
            }
            return retVal;
        }
    }

    [ServiceContract]
    public interface IBluetoothAppContract {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetConnectedDevices();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        bool SendMessage(string message, string[] check);
    }
}