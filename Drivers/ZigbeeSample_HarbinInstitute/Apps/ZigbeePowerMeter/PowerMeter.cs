using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common.DataStore;
using System.Text.RegularExpressions;
using System.Collections;

namespace HomeOS.Hub.Apps.PowerMeter
{

    /// <summary>
    /// A dummy a module that 
    /// 1. sends ping messages to all active dummy ports
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Apps.PowerMeter")]
    public class PowerMeter : ModuleBase
    {
        //list of accessible dummy ports in the system
        List<VPort> accessiblePowerMeterPorts;

        private SafeServiceHost serviceHost;

        private WebFileServer appServer;

        List<string> receivedMessageList;
        
        List<string> PowerMeters = new List<string>();

        Queue commandQueue = null;

        public Hashtable statusDevice = new Hashtable();
        
        SafeThread worker = null;

        IStream datastream;


        public override void Start()
        {
            logger.Log("Started: {0} ", ToString());

            PowerMeterService powermeterService = new PowerMeterService(logger, this);
            serviceHost = new SafeServiceHost(logger, typeof(IPowerMeterContract), powermeterService, this, Constants.AjaxSuffix, moduleInfo.BaseURL());
            serviceHost.Open();

            appServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);


            //........... instantiate the list of other ports that we are interested in
            accessiblePowerMeterPorts = new List<VPort>();

            //..... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();

            if (allPortsList != null)
                ProcessAllPortsList(allPortsList);

            this.receivedMessageList = new List<string>();

            // remoteSync flag can be set to true, if the Platform Settings has the Cloud storage
            // information i.e., DataStoreAccountName, DataStoreAccountKey values
            datastream = base.CreateFileStream<StrKey, StrValue>("dumb", false /* remoteSync */);

            
            //you can change SerialportName:"COM3",Maybe yours is not "COM3"
            foreach (VPort port in accessiblePowerMeterPorts)
            {

              Invoke(port, RolePowerMeter.Instance, RolePowerMeter.OpOpenSerialPort, new ParamType(ParamType.SimpleType.text, "SerialPortName", "COM3"));
            }

            SendCommand("status");

            GetAllPowerMeterStatus();

            worker = new SafeThread(delegate()
            {
                Work();
            }, "AppPowerMeter-worker", logger);
            worker.Start();

        }

       public void  SendCommand(string command)
       {
         //commandQueue.Enqueue(command);
         foreach (VPort port in accessiblePowerMeterPorts)
         {
             
             Invoke(port, RolePowerMeter.Instance, RolePowerMeter.OpWriteSerialPort, new ParamType(ParamType.SimpleType.text, "WriteCommand", command));
             ReadMessage();
          }
        }




        public void ReadMessage()
        {
            string message = "";
            foreach (VPort port in accessiblePowerMeterPorts)
            {
               IList<VParamType> retVals = new List<VParamType>();
               System.Threading.Thread.Sleep(1 * 1 * 1000);
               retVals = Invoke(port, RolePowerMeter.Instance, RolePowerMeter.OpReadSerialPort, new ParamType(0));
               message = retVals[0].Value().ToString();
               analysisResult(message);
            }
            
        }



         public void analysisResult(string massage)
         {
            if (
                massage.Contains("Neighbor Table:") && massage.Contains("Route Table:") &&
                massage.Contains("Max ZED") && massage.Contains("CHILD DATA")
                )
            {

                GetAllDevices(massage);
                Console.WriteLine("***********");
                GetPowerMeterList();
            }
            else if (massage.Length == 198 && massage.Contains("$") && massage.Contains("0080004115"))
            {
                GetPower(massage);
            }
            else if (massage.Contains("FF,FF,0104,0006,01,01,1D40") && massage.Contains("FF,0104,0006,01,01,0D40"))
            {
                GetDeviceStatus(massage);
            }

          }



        public void GetAllDevices(String Statusresult)
        {
            //number of powermeters
            MatchCollection mc;
            string[] DeviceID = new String[100];
            String test = Statusresult;

            String[] results = new String[20];
            int[] matchposition = new int[20];
            Regex r = new Regex("eui:");
            mc = r.Matches(test);
            for (int i = 0; i < mc.Count; i++)
            {
                results[i] = mc[i].Value;
                matchposition[i] = mc[i].Index;
                DeviceID[i] = test.Substring(matchposition[i] + 7, 16);
            }
            for (int j = 0; j < DeviceID.Length; j++)
            {
                if (DeviceID[j] != null && !PowerMeters.Contains(DeviceID[j]))
                {
                    PowerMeters.Add(DeviceID[j]);
                    // findDeviceStatus(DeviceID[j]);
                    Console.WriteLine("PowermeterList:" + j + ":" + DeviceID[j]);

                }
                else
                    break;
            }
        }
        



        public void GetAllPowerMeterStatus() 
        {
                foreach (VPort port in accessiblePowerMeterPorts)
                {
                    foreach (string powermeter in PowerMeters)
                    {
                        string command = "$01,CD," + powermeter + ",0104,0006,01,01,FFFF,00,00,00,FFFF,0000";
                        SendCommand(command);
                    }
                }   
        }




        //Get PowerMeter's status (ON or OFF)
        public void GetDeviceStatus(string status)
        {
            string powerMeterID = "";
            string statusResult = "";
        
            powerMeterID = status.Substring(7, 16);
            if (statusDevice.Contains(powerMeterID))
                return;
            else
            {
                if (status.Contains("0000001001"))
                {
                    statusResult = "ON";
                }
                else if (status.Contains("0000001000"))
                {
                    statusResult = "OFF";
                }
                statusDevice.Add(powerMeterID, statusResult);

                Console.WriteLine("%%%%%%%%%" + powerMeterID + ":" + statusResult + "%%%%%%%%%");
            }
        }
         

        public void GetPower(String Result)
        {
            int index = Result.IndexOf("0080004115");
            String DeviceID = Result.Substring(7, 16);

            Hashtable ht = new Hashtable();
            String HexVoltage = Result.Substring(index + 10, 4);
            String HexCurrent = Result.Substring(index + 14, 4);
            String HexFrequency = Result.Substring(index + 18, 4);
            String HexPowerFactor = Result.Substring(index + 22, 2);
            String HexActivePower = Result.Substring(index + 24, 8);
            String HexApparentPower = Result.Substring(index + 32, 8);
            String HexMainEnergy = Result.Substring(index + 40, 12);



            String TenVoltage = Hex2Ten(HexVoltage) + "V";
            String TenCurrent = Hex2Ten(HexCurrent) + "A";
            String TenFrequency = Hex2Ten(HexFrequency) + "Hz";
            String TenPowerFactor = Hex2Ten(HexPowerFactor);
            String TenActivePower = Hex2Ten(HexActivePower) + "W";
            String TenApparentPower = Hex2Ten(HexApparentPower) + "VA";
            String TenMainEnergy = Hex2Ten(HexMainEnergy) + "KW";

            //PowerMessageList.Add(Hex2Ten(HexActivePower));


            receivedMessageList.Add("DeviceID:" + DeviceID +
                "TenVoltage:" + TenVoltage +
                "TenCurrent:" + TenCurrent +
                "TenFrequency:" + TenFrequency +
                "TenPowerFactor:" + TenPowerFactor +
                "TenActivePower:" + TenActivePower +
                "TenApparentPower:" + TenApparentPower +
                "TenMainEnergy:" + TenMainEnergy + "\r\n");

            //EnergyDic.Add(DeviceID, "TenVoltage:" + TenVoltage +
            //    "TenCurrent:" + TenCurrent +
            //    "TenFrequency:" + TenFrequency +
            //    "TenPowerFactor:" + TenPowerFactor +
            //    "TenActivePower:" + TenActivePower +
            //    "TenApparentPower:" + TenApparentPower +
            //    "TenMainEnergy:" + TenMainEnergy);

            Console.WriteLine("DeviceID:" + DeviceID +
               "TenVoltage:" + TenVoltage +
               "TenCurrent:" + TenCurrent +
               "TenFrequency:" + TenFrequency +
               "TenPowerFactor:" + TenPowerFactor +
               "TenActivePower:" + TenActivePower +
               "TenApparentPower:" + TenApparentPower +
               "TenMainEnergy:" + TenMainEnergy + "\r\n");

        }


        public static string Hex2Ten(string hex)
        {
            int ten = 0;
            double result;
            for (int i = 0, j = hex.Length - 1; i < hex.Length; i++)
            {
                ten += HexChar2Value(hex.Substring(i, 1)) * ((int)Math.Pow(16, j));
                j--;
            }
            if (hex.Equals("HexMainEnergy"))
                result = ten / 1000.0;
            else
                result = ten / 100.0;
            return result.ToString();
        }


        public static int HexChar2Value(string hexChar)
        {
            switch (hexChar)
            {
                case "0":
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                    return Convert.ToInt32(hexChar);
                case "a":
                case "A":
                    return 10;
                case "b":
                case "B":
                    return 11;
                case "c":
                case "C":
                    return 12;
                case "d":
                case "D":
                    return 13;
                case "e":
                case "E":
                    return 14;
                case "f":
                case "F":
                    return 15;
                default:
                    return 0;
            }
        }
        public override void Stop()
        {
            logger.Log("AppPowerMeter clean up");
            if (worker != null)
                worker.Abort();
            if (datastream != null)
                datastream.Close();
        }

        /// <summary>
        /// Get All PowerMeters Realtime Energy Data
        /// </summary>
        public void Work()
        {
           
            int counter = 0;
            while (true)
            {
                counter++;

                lock (this)
                {
                    foreach (VPort port in accessiblePowerMeterPorts)
                    {
                        foreach (string powermeter in PowerMeters) 
                        {
                            string command = "$01,CD," + powermeter + ",0104,0702,01,01,FFFF,00,00,00,FFFF,0080";
                            Invoke(port, RolePowerMeter.Instance, RolePowerMeter.OpWriteSerialPort, new ParamType(ParamType.SimpleType.text, "WriteCommand", command));
                            ReadMessage();
                        }
                    }
                }

                WriteToStream();
                System.Threading.Thread.Sleep(1 * 10 * 1000);
            }
           
        }

        public void SetON(String deviceID)
        {

           
          
            string command = "$01,CD," + deviceID + ",0104,0006,01,01,FFFF,01,01,00,FFFF";
            SendCommand(command);
            statusDevice[deviceID] = "ON";

        }

        public void SetOFF(String deviceID)
        {
           
            
            string command = "$01,CD," + deviceID + ",0104,0006,01,01,FFFF,00,01,00,FFFF";
            SendCommand(command);
            statusDevice[deviceID] = "OFF";
           
        }








        public void WriteToStream()
        {
            StrKey key = new StrKey("PowerMeterKey");
            datastream.Append(key, new StrValue("PowerMeterVal"));
            logger.Log("Writing {0} to stream ", datastream.Get(key).ToString());
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
                if (!accessiblePowerMeterPorts.Contains(port) &&
                    Role.ContainsRole(port, RolePowerMeter.RoleName) &&
                    GetCapabilityFromPlatform(port) != null)
                {
                    accessiblePowerMeterPorts.Add(port);

                    logger.Log("{0} added port {1}", this.ToString(), port.ToString());

                }
            }
        }

        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (accessiblePowerMeterPorts.Contains(port))
                {
                    accessiblePowerMeterPorts.Remove(port);
                    logger.Log("{0} deregistered port {1}", this.ToString(), port.GetInfo().ModuleFacingName());
                }
            }
        }

        public List<string> GetReceivedMessages()
        {
            List<string> retList = new List<string>(this.receivedMessageList);
            retList.Reverse();
            return retList;
        }

        public List<string> GetPowerMeterList()
        {
            List<string> retList = new List<string>(this.PowerMeters);
            retList.Reverse();
            //foreach (string test in PowerMeters) 
            //{
            //    Console.WriteLine(test);
            //}
            return retList;
        }
    }
}