using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.IO.Ports;

namespace HomeOS.Hub.Drivers.LancasterUni.Valve
{
    [System.AddIn.AddIn("HomeOS.Hub.Drivers.LancasterUni.Valve")]
    public class DriverLancasterUniValve : ModuleBase
    {
        protected string deviceId;
        //protected IPAddress deviceIp;
        protected string PortName;
        protected Port devicePort;
        protected SerialPort sPort = null;
        protected SafeThread worker = null;
        protected WebFileServer imageServer;

        

        public override void Start()
        {
            try
            {
                string[] words = moduleInfo.Args();

                deviceId = words[0];
            }
            catch (Exception e)
            {
                logger.Log("{0}: Improper arguments: {1}. Exiting module", this.ToString(), e.ToString());
                return;
            }

            //get the IP address
            PortName = GetDevicePort(deviceId);
            logger.Log("Got a port name of {0}", new string[] { PortName });

            if (PortName == null)
            {
                logger.Log("{0} did not get a device ip for deviceId: {1}. Returning", base.moduleInfo.BinaryName(), deviceId.ToString());
                return;
            }

            //add the service port
            VPortInfo pInfo = GetPortInfoFromPlatform("valve-" + deviceId);
            devicePort = InitPort(pInfo);

            // add role and register with platform
            BindRoles(devicePort, GetRoleList(), OnOperationInvoke);
            RegisterPortWithPlatform(devicePort);

            imageServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
        }

        public string GetDevicePort(string deviceId)
        {
            return deviceId.Replace("Valve+", "");
        }

        public override void Stop() { }

        public override string GetDescription(string hint)
        {
            logger.Log("DriverValve.GetDescription for {0}", hint);
            return moduleInfo.FriendlyName();
        }

        private List<VRole> GetRoleList()
        {
            return new List<VRole>() { RoleValve.Instance };
        }

        private List<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> parameters)
        {
            if (roleName.ToLower().Equals(RoleValve.RoleName))
            {
                switch (opName.ToLower())
                {
                    case RoleValve.OpSend:
                        {
                            SerialPort port = GetPort();

                            port.WriteLine("send\r\n");

                            port.Close();
                        }
                        break;
                    case RoleValve.OpDone:
                        {
                            SerialPort port = GetPort();

                            port.WriteLine("done\r\n");

                            port.Close();
                        }
                        break;
                    case RoleValve.OpReset:
                        {
                            SerialPort port = GetPort();

                            port.WriteLine("reset\r\n");

                            port.Close();
                        }
                        break;
                    case RoleValve.OpSetAllValves:
                        {
                            SerialPort port = GetPort();

                            int value = (int)parameters[0].Value();

                            port.WriteLine(string.Format("setpa {0}\r\n", value));

                            port.Close();
                        }
                        break;
                    case RoleValve.OpSetValve:
                        {
                            SerialPort port = GetPort();

                            int valve = (int)parameters[0].Value();
                            int valveValue = (int)parameters[1].Value();

                            port.WriteLine(string.Format("setp {0} {1}\r\n", valve, valveValue));

                            port.Close();
                        }
                        break;
                    case RoleValve.OpGetValveNumber:
                        {
                            SerialPort port = GetPort();

                            port.WriteLine("getvalves\r\n");

                            string readValue = port.ReadLine().Trim();

                            int totalValveNumber = int.Parse(readValue);

                            logger.Log(string.Format("Total valves = {0}", totalValveNumber));

                            List<VParamType> returnValues = new List<VParamType>();

                            returnValues.Add(new ParamType(totalValveNumber));

                            return returnValues;
                        }
                        break;
                    default:
                        return new List<VParamType>();
                }
            }
            return new List<VParamType>();
        }

        private SerialPort GetPort()
        {
            if (sPort == null || !sPort.IsOpen)
            {

                sPort = new SerialPort(PortName, 93750, Parity.None, 8, StopBits.One);
                sPort.Handshake = Handshake.None;
                sPort.ReadTimeout = 5000;  //enough time for the device to respond?
                sPort.WriteTimeout = 500;

                int maxAttemptsToOpen = 2;

                while (!sPort.IsOpen && maxAttemptsToOpen > 0)
                {

                    //Thread.Sleep(100);
                    try
                    {
                        sPort.Open();
                        break;
                    }
                    catch (Exception e)
                    {
                        maxAttemptsToOpen--;

                        logger.Log("Got {0} exception while opening {1}. Num attempts left = {2}",
                                    e.Message, PortName, maxAttemptsToOpen.ToString());

                        //sleep if we'll atttempt again
                        if (maxAttemptsToOpen > 0)
                            System.Threading.Thread.Sleep(1 * 1000);
                    }
                }
            }
            return sPort;
            
        }

        //we have nothing to do with other ports
        public override void PortRegistered(VPort port) { }
        public override void PortDeregistered(VPort port) { }
    }
}
