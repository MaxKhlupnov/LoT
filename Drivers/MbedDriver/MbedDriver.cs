using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.AddIn;
using System.Threading;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common;
using System.IO.Ports;

namespace HomeOS.Hub.Drivers.MbedDriver
{
    /// <summary>
    /// Driver for mbed devices. To use this you will need an LPC1768 mbed device and CC2500 RF module.
    /// For the code hosted on mbed LPC1768 device and other info, visit http://www.sysnet.org.pk/w/SoftUPS
    /// </summary>

    #region "Commands"
    public class Commands
    {
        public const char ACK = (char)0x01;
        public const char SWITCH1_ON = (char)0x03;
        public const char SWITCH2_ON = (char)0x04;
        public const char SWITCH3_ON = (char)0x05;
        public const char SWITCH4_ON = (char)0x06;
        public const char SWITCH1_OFF = (char)0x07;
        public const char SWITCH2_OFF = (char)0x08;
        public const char SWITCH3_OFF = (char)0x09;
        public const char SWITCH4_OFF = (char)0x0A;
        public const char TURNOFF_ALL = (char)0x0B;
        public const char TURNON_ALL = (char)0x0C;
        public const char GET_STATE_SWITCH_3 = (char)0x0D;
        public const char GET_STATE_SWITCH_4 = (char)0x0E;
        public const char GET_NUMBER_DEVICES = (char)0x0F;
    }
    #endregion

    [System.AddIn.AddIn("HomeOS.Hub.Drivers.MbedDriver")]
    public class MbedDriver : ModuleBase
    {
        #region "Data members"

        /// <summary>
        /// Commands for serial driver
        /// </summary>
        //Commands commands;

        /// <summary>
        /// mbed serial name
        /// </summary>
        private const string MBED = "mbed";

        /// <summary>
        /// Serial Port class instance
        /// </summary>
        private SerialPort serialPort = new SerialPort();

        /// <summary>
        /// BaudRate set to default for Serial Port Class
        /// </summary>
        private int baudRate = 9600;

        /// <summary>
        /// DataBits set to default for Serial Port Class
        /// </summary>
        private int dataBits = 8;

        /// <summary>
        /// Handshake set to default for Serial Port Class
        /// </summary>
        private Handshake handshake = Handshake.None;

        /// <summary>
        /// Parity set to default for Serial Port Class
        /// </summary>
        private Parity parity = Parity.None;

        /// <summary>
        /// Communication Port name, not default in SerialPort. Defaulted to COM1
        /// </summary>
        private string portName = string.Empty;

        /// <summary>
        /// StopBits set to default for Serial Port Class
        /// </summary>
        private StopBits stopBits = StopBits.One;

        /// <summary>
        /// Holds data received until we get a terminator.
        /// </summary>
        private string tString = string.Empty;

        /// <summary>
        /// End of transmition byte in this case EOT (ASCII 4).
        /// </summary>
        private byte terminator = 0x4;

        /// <summary>
        /// data from device 
        /// </summary>
        public string dataFromDevice;

        /// <summary>
        /// Flag for data receiving
        /// </summary>
        public bool dataReceivedFlag = false;

        Port mbedPort;
        /// <summary>
        /// Gets or sets BaudRate (Default: 9600)
        /// </summary>
        public int BaudRate { get { return this.baudRate; } set { this.baudRate = value; } }

        /// <summary>
        /// Gets or sets DataBits (Default: 8)
        /// </summary>
        public int DataBits { get { return this.dataBits; } set { this.dataBits = value; } }

        /// <summary>
        /// Gets or sets Handshake (Default: None)
        /// </summary>
        public Handshake Handshake { get { return this.handshake; } set { this.handshake = value; } }

        /// <summary>
        /// Gets or sets Parity (Default: None)
        /// </summary>
        public Parity Parity { get { return this.parity; } set { this.parity = value; } }

        /// <summary>
        /// Gets or sets PortName (Default: COM1)
        /// </summary>
        public string PortName { get { return this.portName; } set { this.portName = value; } }

        /// <summary>
        /// Gets or sets StopBits (Default: One}
        /// </summary>
        public StopBits StopBits { get { return this.stopBits; } set { this.stopBits = value; } }

        /// <summary>
        /// Thread to check for COMPORT connection
        /// </summary>
        private SafeThread connectionChecker;

        #endregion

        #region "Methods for communication with mbed"

        /// <summary>
        /// Initilize the serial port for communication
        /// </summary>
        public bool InitializePort()
        {
            try
            {
                this.serialPort.BaudRate = this.baudRate;
                this.serialPort.DataBits = this.dataBits;
                this.serialPort.Handshake = this.handshake;
                this.serialPort.Parity = this.parity;
                this.serialPort.PortName = this.portName;
                this.serialPort.StopBits = this.stopBits;
                this.serialPort.DataReceived += new SerialDataReceivedEventHandler(this.Mbed_Data_Received);

            }
            catch (Exception ex)
            {
                logger.Log("Exception in initializePort : {0}", ex.Message);
                return false;
            }
            try
            {
                serialPort.DtrEnable = true;
                serialPort.RtsEnable = true;
            }
            catch (Exception ex)
            {
                logger.Log("Exception in initializePort : {0}", ex.Message);
            }

            return true;
        }

        /// <summary>
        /// The callback function to a receive data interrupt from the serial driver 
        /// </summary>
        private void Mbed_Data_Received(object sender, SerialDataReceivedEventArgs e)
        {
            // Show all the incoming data in the port's buffer
            dataReceivedFlag = true;
            logger.Log("Got data from the mbed: {0} ", dataFromDevice);
        }

        /// <summary>
        /// Set a array of data to the serial driver
        /// </summary>
        public bool Send(byte[] data)
        {
            try
            {
                serialPort.Write(data, 0, data.Length);
                System.Threading.Thread.Sleep(500);
                logger.Log("Got data from the mbed: {0} ", data.ToString());
            }
            catch (Exception ex)
            {
                logger.Log("Exception in send(byte[] data): {0}", ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sends a string to the serial device
        /// </summary>
        public bool Send(string data)
        {
            try
            {
                serialPort.Write(data);
                System.Threading.Thread.Sleep(500);
                logger.Log("Got data from the mbed: {0} ", data);
            }
            catch (Exception ex)
            {
                logger.Log("Exception in send(string data): {0}", ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sends a single Character to the serial device
        /// </summary>
        public bool Send(char data)
        {
            try
            {
                char[] datatosend = new char[2];
                datatosend[0] = data;
                serialPort.Write(datatosend, 0, 1);
                System.Threading.Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                logger.Log("Exception in send(char data): {0}", ex.Message);
                return false;
            }
            return true;
        }


        public bool TestPort()
        {
            byte[] data;
            data = new byte[4];
            for (int i = 0; i < 30; i++)
            {
                data[0] = (byte)i;
                Send(data);
                if (data[0] != dataFromDevice.ElementAt(0))
                {
                    return false;
                }

            }
            return true;
        }
        

        public void SwitchOnSlot(int slot)
        {
            switch (slot)
            {
                case 0:
                    Send(Commands.TURNON_ALL);
                    break;
                case 1:
                    Send(Commands.SWITCH1_ON);
                    //send((char)commands.SWITCH1_ON);
                    break;
                case 2:
                    Send(Commands.SWITCH2_ON);
                    //send((char)commands.SWITCH2_ON);
                    break;
                case 3:
                    Send(Commands.SWITCH3_ON);
                    //send((char)commands.SWITCH3_ON);
                    break;
                case 4:
                    Send(Commands.SWITCH4_ON);
                    //send((char)commands.SWITCH4_ON);
                    break;
                default:
                    logger.Log("Invalid slot number:{0} in SwitchOnSlot", slot.ToString());
                    break;
            }
        }

        public void SwitchOffSlot(int slot)
        {
            switch (slot)
            {
                case 0:
                    Send(Commands.TURNOFF_ALL);
                    break;
                case 1:
                    Send(Commands.SWITCH1_OFF);
                    //send((char)commands.SWITCH1_OFF);
                    break;
                case 2:
                    Send(Commands.SWITCH2_OFF);
                    //send((char)commands.SWITCH2_OFF);
                    break;
                case 3:
                    Send(Commands.SWITCH3_OFF);
                    //send((char)commands.SWITCH3_OFF);
                    break;
                case 4:
                    Send(Commands.SWITCH4_OFF);
                    //send((char)commands.SWITCH4_OFF);
                    break;
                default:
                    logger.Log("Invalid slot number:{0} in SwitchOffSlot", slot.ToString());
                    break;
            }
        }

        public int GetTotalConnectedDevices()
        {
            int data = 0;
            Send(Commands.GET_NUMBER_DEVICES);
            //send((char)commands.GET_NUMBER_DEVICES);
            dataReceivedFlag = false;
            DateTime requestTime = DateTime.Now;
            double diffTime = 0;
            while ((dataReceivedFlag == false) && (diffTime == 1))
            {
                diffTime = (DateTime.Now - requestTime).TotalSeconds;
            }

            if (dataReceivedFlag == true)
            {
                data = (int)serialPort.ReadChar();
            }

            return data;
        }

        
        public void CheckComConnection()
        {
            while (true)
            {
                try
                {
                    if (!serialPort.IsOpen)
                    {
                        serialPort.Close();
                        serialPort.Open();
                    }

                }
                catch (Exception ex)
                {
                    logger.Log("In {0} module got exception: ", ToString(), ex.Message);
                    //Console.WriteLine("Error Opening Port");
                }
                Thread.Sleep(1000);
            }
        }

        #endregion

        #region "HomeOS required functions"
        public override void Start()
        {
            string embedDriverArgs = moduleInfo.Args()[0];
            string comPortName = embedDriverArgs;
            if (!String.IsNullOrEmpty(comPortName))
            {
                if (comPortName.Contains(MBED))
                {
                    comPortName = comPortName.Replace(MBED + " - ", "");
                }
                this.portName = comPortName;
            }
            connectionChecker = new SafeThread(delegate() { CheckComConnection(); }, "ComPort connection-checker", logger);
            if (InitializePort() == true)
            {
                try
                {
                    serialPort.Open();
                    connectionChecker.Start();
                    while (!connectionChecker.IsAlive()) ;
                }
                catch (Exception)
                {
                    List<COMPortFinder> comportList = COMPortFinder.GetCOMPortsInfo();

                    foreach (COMPortFinder comPortInfo in comportList)
                    {
                        if (comPortInfo.Description.Contains(MBED))
                        {
                            this.portName = comPortInfo.Name;
                            break;
                        }
                    }
                    InitializePort();
                    //serialPort.Open();
                }

                //testPort();
            }


            //.................instantiate the port
            VPortInfo portInfo = GetPortInfoFromPlatform(embedDriverArgs);
            mbedPort = InitPort(portInfo);

            // ..... initialize the list of roles we are going to export and bind to the role
            List<VRole> listRole = new List<VRole>() { RoleMbedSoftUps.Instance };
            BindRoles(mbedPort, listRole);

            //.................register the port after the binding is complete
            RegisterPortWithPlatform(mbedPort);

        }

        public override void Stop()
        {
            logger.Log("Stop() at {0}", ToString());
            if (connectionChecker != null && connectionChecker.IsAlive())
            {
                connectionChecker.Abort();
                connectionChecker.Join();
            }
            serialPort.Close();
            Finished();
        }

        public override void PortDeregistered(VPort port)
        {

        }

        public override void PortRegistered(VPort port)
        {

        }
        public override IList<VParamType> OnInvoke(string roleName, string opName, IList<VParamType> retVals)
        {
            if (!roleName.Equals(RoleMbedSoftUps.RoleName))
            {
                logger.Log("Invalid role {0} in OnInvoke", roleName);
                return null;
            }

            switch (opName.ToLower())
            {
                case RoleMbedSoftUps.OpOnSwitch:
                    int payload = (int)retVals[0].Value();
                    logger.Log("{0} Got on request {1}", this.ToString(), payload.ToString());
                    SwitchOnSlot(payload);
                    return new List<VParamType>() { new ParamType(-1 * payload) };
                    break;

                case RoleMbedSoftUps.OpOffSwitch:
                    payload = (int)retVals[0].Value();
                    logger.Log("{0} Got on request {1}", this.ToString(), payload.ToString());
                    SwitchOffSlot(payload);
                    return new List<VParamType>() { new ParamType(-1 * payload) };
                    break;

                case RoleMbedSoftUps.OpGetDeviceNum:
                    payload = (int)retVals[0].Value();
                    logger.Log("{0} Got on request {1}", this.ToString(), payload.ToString());
                    payload = GetTotalConnectedDevices();
                    return new List<VParamType>() { new ParamType(payload) };
                    break;

                default:
                    logger.Log("Invalid operation: {0}", opName);
                    return null;
            }
        }
        #endregion
    }
}
