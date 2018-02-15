using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Common;
using System.IO.Ports;
using System.Text.RegularExpressions;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Drivers.Envi
{
    /// <summary>
    /// An envi driver module that 
    /// 1. opens and registers a port
    /// 2. reads the active power consumption of an appliance from the USB port whenever it is available
    /// 3. Notifies applications that subscribed to this port
    /// </summary>

   [System.AddIn.AddIn("HomeOS.Hub.Drivers.Envi")]
    public class Envi : ModuleBase
    {
        private Port enviPort;
        private SerialPort serialport;
        private string SerialPortName;
        private const string Prolific = "Prolific";

        public override void Start()
        {
            logger.Log("Started: {0}", ToString());

            List<COMPortInfo> comportList = COMPortInfo.GetCOMPortsInfo();

            foreach (COMPortInfo comPortInfo in comportList)
            {
                if (comPortInfo.Description.Contains(Prolific))
                {
                    this.SerialPortName = comPortInfo.Name;
                    break;
                }
            }
            logger.Log("Discovered envi sensor on COM port: "+SerialPortName);


            // ..... initialize the list of roles we are going to export
            List<VRole> listRole = new List<VRole>() {RoleSensorMultiLevel.Instance };
            //AJ add the other roles - like temperature into this list and then below do notifications on them
            //List<VRole> listRole = new List<VRole>() { RoleSensor.Instance };

            //.................instantiate the port
            VPortInfo portInfo = GetPortInfoFromPlatform("envi");
            enviPort = InitPort(portInfo);

            //..... bind the port to roles and delegates
            BindRoles(enviPort, listRole, null);

            //.................register the port after the binding is complete
            RegisterPortWithPlatform(enviPort);

            ReadFromSerialPort();
        }

        private void ReadFromSerialPort()
        {
            serialport = new SerialPort(SerialPortName, 57600, Parity.None, 8, StopBits.One);
            try
            {
                // Close the serial port if it is open
                if (serialport.IsOpen)
                {
                    serialport.Close();
                }
                // Open the serial port again
                serialport.Open();
            }
            catch (System.IO.IOException)
            {

                logger.Log("Driver Envi Error: failed to open {0} port", SerialPortName);
                return;
            }

            // Defining the event handler function
            serialport.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            IList<VParamType> retVals = new List<VParamType>();
            float value = 0;
            Regex measurementsBoth = new Regex(@".+<watts>(\d+)</watts>.+<watts>(\d+)</watts>.+");
            Regex measurementSingle = new Regex(@".+<watts>(\d+)</watts>.+");
            String str = serialport.ReadLine();
            Match mBoth = measurementsBoth.Match(str);
            if (mBoth.Success)
            {
                int ch1 = Convert.ToInt32(mBoth.Groups[1].Value);
                int ch2 = Convert.ToInt32(mBoth.Groups[2].Value);
              
                // Adding power consumptions of both channels, the result is the total power consumption
                value = ch1 + ch2;            
            }
            else
            {
                // If we aren't measuring anything we get a single channel with zero. 
                Match mSingle = measurementSingle.Match(str);
                if (mSingle.Success)
                {
                    value = Convert.ToInt32(mSingle.Groups[1].Value);
                }
                else  //something really bogus happened don't do anything.
                {
                    logger.Log("{0} is not a valid measurment data", str);
                    return;
                }
            }

            // Setting the return parameter   
            retVals.Add(new ParamType(value));

            // Notifying modules (e.g., AppEnvi) subscribed to the enviPort
            enviPort.Notify(RoleSensorMultiLevel.RoleName, RoleSensorMultiLevel.OpGetName, retVals);

            logger.Log("{0}: issued notification on port. read value {1}", SerialPortName, value.ToString());
        }

        public override void Stop()
        {
            this.serialport.Close();
            Finished();
        }

        // <summary>
        // The demultiplexing routing for incoming
        // </summary>
        // <param name="message"></param>
        //private List<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> parameters)
        //{
        //    return new List<VParamType>() { new ParamType(-1 * payload) };
        //}

        /// <summary>
        ///  Called when a new port is registered with the platform
        ///        the dummy driver does not care about other ports in the system
        /// </summary>
        public override void PortRegistered(VPort port) { }

        /// <summary>
        ///  Called when a new port is deregistered with the platform
        ///        the dummy driver does not care about other ports in the system
        /// </summary>
        public override void PortDeregistered(VPort port) { }

    }

}
