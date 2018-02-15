using System;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace HomeOS.Hub.Drivers.PowerMeter
{

    //Sherry Lee

    /// <summary>
    /// A driver module that 
    /// 1. Connects to serial port 
    /// 2. Get PowerMeter Data
    /// </summary>
    /// 
    

    [System.AddIn.AddIn("HomeOS.Hub.Drivers.PowerMeter")]
    public class DriverPowerMeter : ModuleBase
    {
       

        /// <summary>
        /// Serial Port class instance
        /// </summary>
        SerialPort comm = new SerialPort();
        static StringBuilder builder = new StringBuilder();
        SafeThread workThread = null;
        Port powermeterPort;

        private WebFileServer imageServer;
    
        
        public override void Start()
        {
            
           

            logger.Log("Started: {0} ", ToString());
            
            string meterDevice = moduleInfo.Args()[0];

            //Instantiate the port
            VPortInfo portInfo = GetPortInfoFromPlatform(meterDevice);
            powermeterPort = InitPort(portInfo);
            //Initialize the list of roles we are going to export and bind to the role
            List<VRole> listRole = new List<VRole>() {RolePowerMeter.Instance};
            BindRoles(powermeterPort, listRole);

            //Register the port after the binding is complete
            RegisterPortWithPlatform(powermeterPort);

            imageServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
            
         
            System.Threading.Thread.Sleep(1 * 10 * 1000);
           
          
        }


        public override void Stop()
        {
            logger.Log("Stop() at {0}", ToString());
            if (workThread != null)
                workThread.Abort();
            imageServer.Dispose();
        }





        public bool commOpen(string commName)
        {

            comm.PortName = commName;
            try
            {

                comm.BaudRate = 115200;
                comm.DataBits = 8;
                comm.Parity = Parity.None;
                comm.StopBits = System.IO.Ports.StopBits.One;
                comm.RtsEnable = true;
                comm.NewLine = "\r\n";
                comm.DataReceived += new SerialDataReceivedEventHandler(comm_DataReceived);
                comm.Open(); 
                return true;
            }
            catch (Exception ex)
            {
                logger.Log("Exception in initializePort : {0}", ex.Message);
                return false;
            }
        }

        public void WriteCom(String command)
        {
            lock (this)
            {
                for (int i = 0; i < command.Length; i++)
                {
                    comm.Write(command[i] + "");
                    Console.Write(command[i] + "");
                }
                comm.Write(comm.NewLine);
                Console.Write("\r\n");
            }

        }


        private void comm_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            System.Threading.Thread.Sleep(300);
            lock (this)
            {
                int n = comm.BytesToRead;
                byte[] buf = new byte[n];
                try
                {
                    comm.Read(buf, 0, n);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                    comm.Close();
                    return;
                }

                builder.Remove(0, builder.Length);
                
                if (buf.Length > 453)
                    return;
                else
                {
                    String tmp = Encoding.ASCII.GetString(buf);
                    if (tmp == null || tmp.Length == 0)
                        return;
                    else
                    {
                        builder.Append(tmp);
                       // analysisResult(builder.ToString());
                        //Console.WriteLine(builder.ToString());
                        comm.DiscardInBuffer();
                    }
                }
            }
        }

        


       

        /// <summary>
        /// OpenSerialPort
        /// </summary>
        /// <returns>true:open success;false:open fail</returns>
        public int OpenSerialPort(string serialPortName)
        {
            if (commOpen(serialPortName))
                return 1;
            else
                return 0;
        }


        /// <summary>
        /// CloseSerialPort
        /// </summary>
        public void CloseSerialPort()
        {
            comm.Close();
        }


        public void WriteSerialPort(string command) 
        {
            WriteCom(command);
        }

        public string ReadSerialPort() 
        {
            return builder.ToString();
        }

       


        /// <summary>
        /// The demultiplexing routing for incoming operations
        /// </summary>
        /// <param name="roleName">The name of the role</param>
        /// <param name="opName">The name of the operation</param>
        /// <param name="args">The arguments of the operation</param>
        /// <returns></returns>
        public override IList<VParamType> OnInvoke(string roleName, String opName, IList<VParamType> args)
        {

            if (!roleName.Equals(RolePowerMeter.RoleName))
            {
                logger.Log("Invalid role {0} in OnInvoke", roleName);
                return null;
            }
            
            switch (opName.ToLower())
            {
               
                case RolePowerMeter.OpOpenSerialPort:
                   {
                    string serialPortname =  (string)args[0].Value();
                    int result = OpenSerialPort(serialPortname);
                    return new List<VParamType>() { new ParamType(result) };
   
                   }
                    

                
                case RolePowerMeter.OpCloseSerialPort:
                    {
                        CloseSerialPort();
                        return new List<VParamType>() { new ParamType(true) };
                    }


               
                case RolePowerMeter.OpWriteSerialPort:
                    {
                        string command = (string)args[0].Value();
                        WriteSerialPort(command);
                        return null;
                      
                    }

                case RolePowerMeter.OpReadSerialPort:
                    {

                       string message =  ReadSerialPort();
                       return new List<VParamType>() { new ParamType(ParamType.SimpleType.text, "ReadCommand", message) };
                    }

                default:
                    logger.Log("Invalid operation: {0}", opName);
                    return null;
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        public override void PortRegistered(VPort port) { }

        /// <summary>
        ///  Called when a new port is deregistered with the platform
        /// </summary>
        public override void PortDeregistered(VPort port) { }
    }


    }