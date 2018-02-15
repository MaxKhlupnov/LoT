using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Threading;

namespace HomeOS.Hub.Drivers.DigitalMedia
{

    /// <summary>
    /// A dummy driver module that 
    /// 1. opens and register a dummy ports
    /// 2. sends periodic notifications  (in Work())
    /// 3. sends back responses to received echo requests (in OnOperationInvoke())
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Drivers.DigitalMedia")]
    public class DriverDigitalMedia : ModuleBase
    {
        SafeThread workThread = null; 
        Port digitalMediaPort;

        private WebFileServer imageServer;
        private ActiveCNXConnection crestronConnection;

        public override void Start()
        {
            logger.Log("Started: {0}", ToString());

            string digitalMediaDevice = moduleInfo.Args()[0];

            //.................instantiate the port
            VPortInfo portInfo = GetPortInfoFromPlatform(digitalMediaDevice);
            digitalMediaPort = InitPort(portInfo);

            // ..... initialize the list of roles we are going to export and bind to the role
            List<VRole> listRole = new List<VRole>() { RoleSignalDigital.Instance};
            BindRoles(digitalMediaPort, listRole);

            //.................register the port after the binding is complete
            RegisterPortWithPlatform(digitalMediaPort);

            workThread = new SafeThread(delegate() { Work(); } , "DriverMedia work thread" , logger);
            workThread.Start();

            imageServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
        }

        public override void Stop()
        {
            logger.Log("Stop() at {0}", ToString());
            crestronConnection.Disconnect();

            if (workThread != null)
                workThread.Abort();
            imageServer.Dispose();
        }


        /// <summary>
        /// Sit in a loop and send notifications 
        /// </summary>
        public void Work()
        {
            int counter = 0;

            string [] args = this.moduleInfo.Args();
            //, , , 
            // TODO: Add parameter value verification
            string IPAddress = args[1];  // device.DeviceIpAddress, 
            int IPID = int.Parse(args[2]); //parameters.IPID.ToString(), 
            int IPPort = int.Parse(args[3]) ; // parameters.IPPort.ToString(), 
            string UserName = args[4] ;// parameters.UserName, 
            string Password = args[5]; //parameters.Password, 
            bool UseSSL = bool.Parse(args[6]); // parameters.UseSSL.ToString()


            crestronConnection = new ActiveCNXConnection(IPID, IPAddress, IPPort, UserName, Password, UseSSL, this.logger);
            crestronConnection.Connect(digitalMediaPort);

            while (true)
            {
                counter++;

               // IList<VParamType> retVals = new List<VParamType>() { new ParamType(counter), new ParamType(10), new ParamType(0)};

              //  digitalMediaPort.Notify(RoleSignalDigital.RoleName, RoleSignalDigital.OpSetDigitalName, retVals);

                //Notify(digitalMediaPort, RoleSignalDigital.Instance, RoleSignalDigital.OpSetDigitalName, new ParamType(counter));

                System.Threading.Thread.Sleep(1 * 5 * 1000);
            }
        }

        /// <summary>
        /// The demultiplexing routing for incoming operations
        /// </summary>
        /// <param name="message"></param>
        public override IList<VParamType> OnInvoke(string roleName, String opName, IList<VParamType> args)
        {

            if (!roleName.Equals(RoleSignalDigital.RoleName))
            {
                logger.Log("Invalid role {0} in OnInvoke", roleName);
                return null;
            }

            switch (opName.ToLower())
            {
              
                case RoleSignalDigital.OpSetDigitalName:
                    int slot = (int)args[0].Value();
                    int join = (int)args[1].Value();

                    DigitalSignalValue value = (DigitalSignalValue)Enum.Parse(typeof(DigitalSignalValue), args[2].Value().ToString(), true);

                    logger.Log(string.Format("{0} SendDigital Request  slot: {1} join: {2}", this.ToString(), slot, join));
                    bool returnValue = crestronConnection.SendDigital(slot, join, value);
                    return new List<VParamType>() { new ParamType(returnValue) };

                default:
                    logger.Log("Invalid operation: {0}", opName);
                    return null;
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        ///        the dummy driver does not care about other ports in the system
        /// </summary>
        public override void PortRegistered(VPort port) {}

        /// <summary>
        ///  Called when a new port is deregistered with the platform
        ///        the dummy driver does not care about other ports in the system
        /// </summary>
        public override void PortDeregistered(VPort port) { }
    }
}