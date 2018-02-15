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

namespace HomeOS.Hub.Apps.AzureMobileServicesSample
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SensorDBService : ISensorContract
    {
        protected VLogger logger;
        AzureMobileServicesSample SensorInfo;

        public SensorDBService(VLogger logger, AzureMobileServicesSample SensorStuff)
        {
            this.logger = logger;
            this.SensorInfo = SensorStuff;
        }

        public List<string> GetReceivedMessages()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = SensorInfo.GetReceivedMessages();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetReceivedMessages: " + e);
            }
            return retVal;
        }

        public List<string> GetReceivedMessages_get()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = SensorInfo.GetReceivedMessages();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetReceivedMessages: " + e);
            }
            return retVal;
        }

        public List<string> SetLocalDirectory(bool syncLocal, string directoryPath)
        {
            List<string> retVal = new List<string>();

            try
            {
                SensorInfo.ConfigureWritingToLocalFile(syncLocal, directoryPath);
                retVal.Add("");
            }
            catch (Exception e)
            {
                logger.Log("Got exception in SetLocalDirectory: " + e);
                retVal.Add(e.ToString());
            }
            return retVal;
        }

        public List<string> GetLocalDirectorySyncInfo()
        {
            List<string> retVal = new List<string>();

            try
            {
                retVal.Add("");
                bool syncLocal = SensorInfo.GetIsSyncToLocal();
                retVal.Add(syncLocal.ToString());
                string localDir = SensorInfo.GetLocalDirectory();  //maybe only if syncLocal is true
                retVal.Add(localDir);
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetLocalDirectory: " + e);
                retVal.Add(e.ToString());
            }
            return retVal;
        }

      
    }

     [ServiceContract]
    public interface ISensorContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetReceivedMessages();
        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json, UriTemplate = "/get")]
        List<string> GetReceivedMessages_get();
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
       List<string> SetLocalDirectory(bool syncLocal, string directoryPath);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetLocalDirectorySyncInfo();

         
 


        

    }
}