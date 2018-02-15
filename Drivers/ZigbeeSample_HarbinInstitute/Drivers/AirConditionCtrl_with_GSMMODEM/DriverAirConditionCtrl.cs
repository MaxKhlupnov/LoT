using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Threading;
using GSMMODEM;
namespace HomeOS.Hub.Drivers.AirConditionCtrl
{

    /// <summary>
    /// A airconditionctrl driver module that 
    /// 1. opens and register a airconditionctrl ports
    /// 2. sends periodic notifications  (in Work())
    /// 3. sends back responses to received echo requests (in OnOperationInvoke())
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Drivers.AirConditionCtrl")]
    public class DriverAirConditionCtrl : ModuleBase
    {
        private GsmModem gm = new GsmModem();
        string setphoneNumber;
        string setmsgContent;
        string getphoneNumber;
        string getmsgContent;
        string readPhoneNumber;
        string readMsgContent;
        SafeThread workThread = null;
        Port airConditionCtrlPort;
       
        private WebFileServer imageServer;

        public override void Start()
        {
            logger.Log("Started: {0}", ToString());

            string airConditionCtrlDevice = moduleInfo.Args()[0];

            //.................instantiate the port
            VPortInfo portInfo = GetPortInfoFromPlatform(airConditionCtrlDevice);
            airConditionCtrlPort = InitPort(portInfo);

            // ..... initialize the list of roles we are going to export and bind to the role
            List<VRole> listRole = new List<VRole>() { RoleSwitchMultiLevel.Instance };
            BindRoles(airConditionCtrlPort, listRole);

            //.................register the port after the binding is complete
            RegisterPortWithPlatform(airConditionCtrlPort);

            workThread = new SafeThread(delegate() { Work(); }, "DriverAirConditionCtrl work thread", logger);
            workThread.Start();

            imageServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);

            gm.ComPort = "COM6";
            gm.BaudRate = 9600;
            gm.Open();
            gm.SmsRecieved+=new EventHandler(gm_SmsRecieved);

            
        
        }

        private void gm_SmsRecieved(object sender, EventArgs e)
        {
            DecodedMessage dm = gm.ReadNewMsg();
             getphoneNumber = dm.PhoneNumber;
             getmsgContent = dm.SmsContent;
             
            //throw new NotImplementedException();
        }
        

        public override void Stop()
        {
            logger.Log("Stop() at {0}", ToString());
            if (workThread != null)
                workThread.Abort();
            imageServer.Dispose();
        }


        /// <summary>
        /// Sit in a loop and send notifications 
        /// </summary>
        public void Work()
        {
            //int counter = 0;
            //while (true)
            //{
            //    counter++;

            //    //IList<VParamType> retVals = new List<VParamType>() { new ParamType(counter) };

            //    //airConditionCtrlPort.Notify(RoleSwitchMultiLevel.RoleName, RoleSwitchMultiLevel.OpEchoSubName, retVals);

            //    Notify(airConditionCtrlPort, RoleSwitchMultiLevel.Instance, RoleSwitchMultiLevel.OpGetName, new ParamType(counter));

            //    System.Threading.Thread.Sleep(1 * 5 * 1000);
            //}

           // smsSend();
        }

        /// <summary>
        /// The demultiplexing routing for incoming operations
        /// </summary>
        /// <param name="message"></param>
        public override IList<VParamType> OnInvoke(string roleName, String opName, IList<VParamType> args)
        {

            if (!roleName.Equals(RoleSwitchMultiLevel.RoleName))
            {
                logger.Log("Invalid role {0} in OnInvoke", roleName);
                return null;
            }

            switch (opName.ToLower())
            {
                case RoleSwitchMultiLevel.OpGetName:
                    int payload = (int)args[0].Value();
                    logger.Log("{0} Got EchoRequest {1}", this.ToString(), payload.ToString());

                    return new List<VParamType>() {new ParamType(-1 * payload)};
                case RoleSwitchMultiLevel.OpSendMsgName:
                     setphoneNumber = (string)args[0].Value();
                     setmsgContent = (string)args[1].Value();
                     smsSend();
                     return null;
                case RoleSwitchMultiLevel.OpGetMsgName:
                     IList<VParamType> retVals = new List<VParamType>();
                     retVals.Add(new ParamType(ParamType.SimpleType.text, "smsnum", getphoneNumber));
                     retVals.Add(new ParamType(ParamType.SimpleType.text, "smstext",getmsgContent));
                     //return new List<VParamType>() { new ParamType(ParamType.SimpleType.text, "smsnum", getphoneNumber), new ParamType(ParamType.SimpleType.text, "smstext", getmsgContent) };
                     return retVals;
                case RoleSwitchMultiLevel.OpReadMsgName: 
                     int rcvdReadId = (int)args[0].Value();
                     smsReadById(rcvdReadId);
                     IList<VParamType> retReadVals = new List<VParamType>();
                     retReadVals.Add(new ParamType(ParamType.SimpleType.text, "smsnum", readPhoneNumber));
                     retReadVals.Add(new ParamType(ParamType.SimpleType.text, "smstext", readMsgContent));
                     return retReadVals;
                case RoleSwitchMultiLevel.OpDelMsgName:
                     int rcvdDelId = (int)args[0].Value();
                     smsDelByID(rcvdDelId);
                     return null;
                default:
                    logger.Log("Invalid operation: {0}", opName);
                    return null;
            }

           
        }

        public void smsSend()
        {
            if (gm.IsOpen)
            {
                try
                {
                    gm.SendMsg(setphoneNumber, setmsgContent);
                    Console.WriteLine("控制空调信息发送成功！");
                }
                catch
                {
                    Console.WriteLine("控制空调信息发送失败！");
                }
            }
         }
        public void smsReadById(int Sms_ID)
        {
            if (gm.IsOpen)
            {
                try
                {
                    DecodedMessage dm = gm.ReadMsgByIndex(Sms_ID);
                      readPhoneNumber = dm.PhoneNumber;
                      readMsgContent = dm.SmsContent;
                }
                catch
                {
                    Console.WriteLine("读取短信失败！");
                }

            }
        }
        public void smsDelByID(int Sms_ID)
        {
            if (gm.IsOpen)
            {
                try
                {
                    gm.DeleteMsgByIndex(Sms_ID);
                    Console.WriteLine("成功删除短信！");
                }
                catch
                {
                    Console.WriteLine("短信删除失败！");
                }

            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        ///        the airconditionctrl driver does not care about other ports in the system
        /// </summary>
        public override void PortRegistered(VPort port) {}

        /// <summary>
        ///  Called when a new port is deregistered with the platform
        ///        the airconditionctrl driver does not care about other ports in the system
        /// </summary>
        public override void PortDeregistered(VPort port) { }
    }
}