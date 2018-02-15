using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common.DataStore;
using System.IO.Ports;
using System.Runtime.InteropServices;
using GSMMODEM;

namespace HomeOS.Hub.Apps.AirConditionCtrl
{

    /// <summary>
    /// A airconditionctrl a module that 
    /// 1. sends ping messages to all active airconditionctrl ports
    /// </summary>
    
    [System.AddIn.AddIn("HomeOS.Hub.Apps.AirConditionCtrl")]
    public class AirConditionCtrl : ModuleBase
    {
       
        //static SerialPort comm = new SerialPort();
       
        static  List<string> receivedMessageList;
        static List<string> readMsgByIdList;
        static List<string> rcvdNewMsgList;
       
         //private GsmModem gm = new GsmModem();

        delegate void UpdataDelegate();
        UpdataDelegate UpdateHandle = null;


        //list of accessible dummy ports in the system
        List<VPort> accessibleAirConditionCtrlPorts;

        private SafeServiceHost serviceHost;

        private WebFileServer appServer;

        int SmsID = 0;
        SafeThread worker = null;

        IStream datastream;

        public override void Start()
        {
            logger.Log("Started: {0} ", ToString());

            AirConditionCtrlService airConditionCtrlService = new AirConditionCtrlService(logger, this);
            serviceHost = new SafeServiceHost(logger, typeof(IAirConditionCtrlContract), airConditionCtrlService, this, Constants.AjaxSuffix, moduleInfo.BaseURL());
            serviceHost.Open();

            appServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
            
 
            //........... instantiate the list of other ports that we are interested in
            accessibleAirConditionCtrlPorts = new List<VPort>();

            //..... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();

            if (allPortsList != null)
                ProcessAllPortsList(allPortsList);

            receivedMessageList = new List<string>();

            readMsgByIdList = new List<string>();
            rcvdNewMsgList = new List<string>();
             //remoteSync flag can be set to true, if the Platform Settings has the Cloud storage
             //information i.e., DataStoreAccountName, DataStoreAccountKey values
            datastream = base.CreateFileStream<StrKey, StrValue>("dumb", false /* remoteSync */);

            worker = new SafeThread(delegate()
            {
                Work();
            }, "AppAirConditionCtrl-worker", logger);
            worker.Start();

             
            //gm.ComPort = "COM6";
            //gm.BaudRate = 9600;
            //gm.Open();

             
            //gm.SmsRecieved += new EventHandler(gm_SmsRecieved);

          }

 
        public void Work()
        {
            //int counter = 0;
            //while (true)
            //{
            //    counter++;

            //    lock (this)
            //    {
                    //foreach (VPort port in accessibleAirConditionCtrlPorts)
                    //{
                    //    //SendEchoRequest(port, counter);
                    //}
            //}
            
           
                WriteToStream();
                System.Threading.Thread.Sleep(1 * 10 * 1000);
            //}
        }

      public void WriteToStream()
        {
            StrKey key = new StrKey("DummyKey");
            datastream.Append(key, new StrValue("DummyVal"));
            logger.Log("Writing {0} to stream " , datastream.Get(key).ToString());
        }

       //private void gm_SmsRecieved(object sender, EventArgs e)
       // {

       //     DecodedMessage dm = gm.ReadNewMsg();
       //     String phoneNumber = dm.PhoneNumber;
       //     String msgContent = dm.SmsContent;
       //     DateTime msgSendTime = dm.SendTime;


       //     receivedMessageList.Add("SmsNum:" + phoneNumber +
       //         "SmsContent:" + msgContent +
       //         "SmsSendTime:" + msgSendTime);

       //     Console.WriteLine("SmsNum:" + phoneNumber +
       //         "SmsContent:" + msgContent +
       //         "SmsSendTime:" + msgSendTime);
       //  }
       //public void SmsReceived()
       //{ 
       
       //}

      public void SmsSend1(string Sms_TelNum, string Sms_Text)
     {
             
            foreach(VPort port in accessibleAirConditionCtrlPorts)
            {
                if (Sms_Text != null || Sms_Text != " "||Sms_TelNum != null || Sms_TelNum != " ")
                {
                    //ParamType args = new ParamType();
                    //IList<VParamType> args=new List<VParamType>();
                    //args.Add(new ParamType(ParamType.SimpleType.text, "smsnum", Sms_TelNum));
                    //args.Add(new ParamType(ParamType.SimpleType.text, "smstext", Sms_Text));
                    //port.Invoke(RoleSwitchMultiLevel.RoleName, RoleSwitchMultiLevel.OpSendMsgName, args, ControlPort, );                 
                    Invoke(port, RoleSwitchMultiLevel.Instance, RoleSwitchMultiLevel.OpSendMsgName, new ParamType(ParamType.SimpleType.text, "smsnum", Sms_TelNum), new ParamType(ParamType.SimpleType.text, "smstext", Sms_Text));
                           
                }
            }
      }

      public List<string> SmsRcvdNewMsg()
      {
          IList<VParamType> retVals = new List<VParamType>();
          foreach (VPort port in accessibleAirConditionCtrlPorts)
          {
              retVals = Invoke(port, RoleSwitchMultiLevel.Instance, RoleSwitchMultiLevel.OpGetMsgName, new ParamType(ParamType.SimpleType.text, "smsnum", ""), new ParamType(ParamType.SimpleType.text, "smstext", ""));
             // retMsg = Invoke(port, RoleSwitchMultiLevel.Instance, RoleSwitchMultiLevel.OpGetMsgName, new ParamType(0));
          }
          string strNum = (string)retVals[0].Value();
          string strText = (string)retVals[1].Value();

          rcvdNewMsgList.Add("smsNum:" + strNum + "\t\n" + "smsContent" + strText + "\t\n");

          Console.WriteLine("smsNum:" + strNum + "\t\n" + "smsContent" + strText + "\t\n");

          return rcvdNewMsgList;
         
      }
      public List<string> ReadMsgById(int Sms_ID)
      {
          IList<VParamType> retVals = new List<VParamType>();
         foreach (VPort port in accessibleAirConditionCtrlPorts)
          {
             
              retVals = Invoke(port, RoleSwitchMultiLevel.Instance, RoleSwitchMultiLevel.OpReadMsgName, new ParamType(Sms_ID));
              // retMsg = Invoke(port, RoleSwitchMultiLevel.Instance, RoleSwitchMultiLevel.OpGetMsgName, new ParamType(0));
           }
          string strNum = (string)retVals[0].Value();
          string strText = (string)retVals[1].Value();
          readMsgByIdList.Add("smsNum:" + strNum + "\t\n" + "smsContent" + strText + "\t\n");

          Console.WriteLine("smsNum:" + strNum + "\t\n" + "smsContent" + strText + "\t\n");

          return readMsgByIdList;
          //List<string> retList = new List<string>(readMsgByIdList);
          //retList.Reverse();
          //return retList;
           
      }
      public void DelMsgById(int Sms_ID)
      {
          
          IList<VParamType> retVals = new List<VParamType>();
          foreach (VPort port in accessibleAirConditionCtrlPorts)
          {

              retVals = Invoke(port, RoleSwitchMultiLevel.Instance, RoleSwitchMultiLevel.OpDelMsgName, new ParamType(Sms_ID));
               
          }
         
      }

        public override void Stop()
        {
            logger.Log("AppAirConditionCtrl clean up");
            if (worker != null)
                worker.Abort();
            if (datastream != null)
                datastream.Close();
        }


        public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        {
            string message;
            lock (this)
            {
                switch (opName.ToLower())
                {
                    case RoleSwitchMultiLevel.OpGetName:
                        int rcvdNum = (int)retVals[0].Value();

                        message = String.Format("async echo response from {0}. rcvd = {1}", senderPort.ToString(), rcvdNum.ToString());
                        //this.receivedMessageList.Add(message);
                        break;
                    default:
                        message = String.Format("Invalid async operation return {0} from {1}", opName.ToLower(), senderPort.ToString());
                        break;
                }
            }
            logger.Log("{0} {1}", this.ToString(), message);

        }

       
      private void ProcessAllPortsList(IList<VPort> portList)
        {
            foreach (VPort port in portList)
            {
                PortRegistered(port);
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        public override void PortRegistered(VPort port)
        {

            logger.Log("{0} got registeration notification for {1}", ToString(), port.ToString());

            lock (this)
            {
                if (!accessibleAirConditionCtrlPorts.Contains(port) && 
                    Role.ContainsRole(port, RoleSwitchMultiLevel.RoleName) && 
                    GetCapabilityFromPlatform(port) != null)
                {
                    accessibleAirConditionCtrlPorts.Add(port);

                    logger.Log("{0} added port {1}", this.ToString(), port.ToString());

                    if (Subscribe(port, RoleSwitchMultiLevel.Instance, RoleSwitchMultiLevel.OpGetName))
                        logger.Log("{0} subscribed to port {1}", this.ToString(), port.ToString());
                    else
                        logger.Log("failed to subscribe to port {1}", this.ToString(), port.ToString());
                }
            }
        }

        
        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (accessibleAirConditionCtrlPorts.Contains(port))
                {
                    accessibleAirConditionCtrlPorts.Remove(port);
                    logger.Log("{0} deregistered port {1}", this.ToString(), port.GetInfo().ModuleFacingName());
                }
            }
        }

      
        public List<string> GetReceivedMessages()
        {
            

            List<string> retList = new List<string>(receivedMessageList);
            retList.Reverse();
            return retList;
        }

    }
}