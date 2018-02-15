using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Threading;
using System.IO.Ports;


namespace HomeOS.Hub.Drivers.Arduino.MicrosoftResearch.Dummy
{

    /// <summary>
    /// A dummy driver module that communicates with a HomeOS Arduino device
    /// 1. Opens and registers serial port to dummy device
    /// 2. opens and register a dummy ports
    /// 2. sends periodic requests to Arduino device and receives back values  (in Work())
    /// 3. sends values on to Dummy example applicadtion (in OnOperationInvoke())
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Drivers.Arduino.MicrosoftResearch.Dummy")]
    public class DriverArduinoMicrosoftDummy :  ModuleBase
    {
        SafeThread workThread = null; 
        Port dummyPort;
        bool serialPortOpen = false;
        string serialPortNameforArudino;
        SerialPort serPort = null;
        private WebFileServer imageServer;

        public override void Start()
        {
            logger.Log("Started: {0}", ToString());

            string dummyDeviceId = moduleInfo.Args()[0];
            serialPortNameforArudino = moduleInfo.Args()[1];

            //.... Open the serial port - AJB TODO - error checking on port name
            serialPortOpen = OpenSerialPort();
         
            //.................instantiate the port
            VPortInfo portInfo = GetPortInfoFromPlatform("arduino-" + dummyDeviceId);
            dummyPort = InitPort(portInfo);

            // ..... initialize the list of roles we are going to export and bind to the role
            List<VRole> listRole = new List<VRole>() { RoleDummy.Instance};
            BindRoles(dummyPort, listRole);

            //.................register the port after the binding is complete
            RegisterPortWithPlatform(dummyPort);

            workThread = new SafeThread(delegate() { Work(); } , "ArduinoDriverDummy work thread" , logger);
            workThread.Start();

            imageServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
        }

        private bool OpenSerialPort()
        {
            if (serialPortOpen && serPort != null)
                serPort.Close();

            serPort = new SerialPort(serialPortNameforArudino, 9600);
            serPort.WriteTimeout = 500;
            serPort.ReadTimeout = 500;
            serPort.DtrEnable = true;  //all stuff needed for arduino micro
            serPort.StopBits = StopBits.One;
            serPort.Parity = Parity.None;
            serPort.Handshake = Handshake.None;
            serPort.DataBits = 8;
            serPort.RtsEnable = false;
                            
            int maxAttemptsToOpen = 2;

            while (!serPort.IsOpen && maxAttemptsToOpen > 0)
            {
                try
                {
                    serPort.Open();
                    return true;
                }
                catch (Exception e)
                {
                    maxAttemptsToOpen--;

                    logger.Log("ArduinoDummyDriver: Got {0} exception while opening {1}. Num attempts left = {2}",
                                e.Message, serialPortNameforArudino, maxAttemptsToOpen.ToString());

                    //sleep if we'll atttempt again
                    if (maxAttemptsToOpen > 0)
                        System.Threading.Thread.Sleep(1 * 1000);
                }
            }
            return false;             
        }

        public override void Stop()
        {
            if (serialPortOpen)
                serPort.Close();
                
            logger.Log("Stop() at {0}", ToString());
            if (workThread != null)
                workThread.Abort();

            if (imageServer != null)
               imageServer.Dispose();
        }


        /// <summary>
        /// Sit in a loop and send notifications 
        /// </summary>
        public void Work()
        {
            int counter = 0;
            string rawDataFromArduino;
            string  cleanDataFromArduino;
            while (true)
            {
                counter++;
                int numVal = -1;

                if (!serialPortOpen)
                    serialPortOpen = OpenSerialPort();
                
                if (serialPortOpen)
                {
                    //Ping the Arduino HomeOS Microsoft Research Dummy device and pass the value back.
                    //right now keeping the serial port open and closing when driver stops

                    try
                    {
                        serPort.Write("[v]"); //ask for value
                        rawDataFromArduino = serPort.ReadTo("]");
                        cleanDataFromArduino = rawDataFromArduino.TrimStart('[');  //remove opening bracket
                        try
                        {
                            numVal = Convert.ToInt32(cleanDataFromArduino);
                        }
                        catch (FormatException e)
                        {
                            logger.Log("ArduinoDummyDriver: Value received from device is not a sequence of digits.");
                        }

                        //notify applications intersted in role dummy and so example works with DummyApplication and others
                        Notify(dummyPort, RoleDummy.Instance, RoleDummy.OpEchoSubName, new ParamType(numVal));
                       
                    }
                    catch (Exception e)
                    {

                        logger.Log("ArduinoDummyDriver: Problem in SerPort Write/Read");
                    }
                }
          
                
                System.Threading.Thread.Sleep(1 * 5 * 1000);
            }
        }

      
        /// <summary>
        /// The demultiplexing routing for incoming operations
        /// </summary>
        /// <param name="message"></param>
        public override IList<VParamType> OnInvoke(string roleName, String opName, IList<VParamType> args)
        {

            if (!(roleName.Equals(RoleDummy.RoleName)))
            {
                logger.Log("Invalid role {0} in OnInvoke", roleName);
                return null;
            }

            switch (opName.ToLower())
            {
                case RoleDummy.OpEchoName:
                    int payload = (int)args[0].Value();
                    
                    logger.Log("{0} Got EchoRequest {1}", this.ToString(), payload.ToString());

                    return new List<VParamType>() {new ParamType(-1 * payload)};

                //TODO Show example of sending message to Arduino

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