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
using GSMMODEM;


namespace HomeOS.Hub.Apps.AirConditionCtrl
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class AirConditionCtrlService : IAirConditionCtrlContract
    {
        protected VLogger logger;
        AirConditionCtrl AirConditionCtrl;

        public AirConditionCtrlService(VLogger logger, AirConditionCtrl AirConditionCtrl)
        {
            this.logger = logger;
            this.AirConditionCtrl = AirConditionCtrl;
        }

        public List<string> GetReceivedMessages()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = AirConditionCtrl.GetReceivedMessages();
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
                retVal = AirConditionCtrl.GetReceivedMessages();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetReceivedMessages: " + e);
            }
            return retVal;
        }

        public void SmsSend1(string Sms_TelNum, string Sms_Text)
        {
            try
            {
                AirConditionCtrl.SmsSend1(Sms_TelNum, Sms_Text);
            }
            catch (Exception e)
            {
                logger.Log("Got exception in SmsSend1: " + e);
            }
        }
        public List<string> SmsRcvdNewMsg()
        {
            List<string> retVal = new List<string>();
            try
            {
                retVal = AirConditionCtrl.SmsRcvdNewMsg();
            }
            catch (Exception e)
            {
                logger.Log("Got exception in ReadMsgById: " + e);

            }
            return retVal;
        }

        public List<string> ReadMsgById(int Sms_ID)
        {
            List<string> retVal = new List<string>();
            try
            {
              retVal = AirConditionCtrl.ReadMsgById(Sms_ID);
            }
            catch (Exception e)
            {
                logger.Log("Got exception in ReadMsgById: " + e);
         
            }
            return retVal;
        }

        public void DelMsgById(int Sms_ID)
        {
            try
            {
                AirConditionCtrl.DelMsgById(Sms_ID);
            }
            catch (Exception e)
            {
                logger.Log("Got exception in DelMsgById: " + e);
            }
        }
    
    }

     [ServiceContract]
    public interface IAirConditionCtrlContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetReceivedMessages();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetReceivedMessages_get();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        void SmsSend1(string Sms_TelNum, string Sms_Text);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> SmsRcvdNewMsg();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> ReadMsgById(int Sms_ID);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        void DelMsgById(int Sms_ID);
    }
}