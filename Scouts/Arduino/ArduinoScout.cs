using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;
using System.Timers;
using System.IO.Ports;


namespace HomeOS.Hub.Scouts.Arduino
{
    public class ArduinoScout : IScout
    {
        //This scout is closely modeled on the GadgeteerScout
        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        ArduinoScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        Dictionary<string, SerialPort> serialPorts = new Dictionary<string, SerialPort>();
        string serialPortofArduino = ""; //need to tell driver which serial port this one is on

        DeviceList currentDeviceList = new DeviceList();

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new ArduinoScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            //create a time that fires ScanNow() periodically
            //var scanTimer = new Timer(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * 1000);
            //scanTimer.Enabled = true;
            //scanTimer.Elapsed += new ElapsedEventHandler(ScanNow);

            ScanNow(null, null);

            logger.Log("ArduinoScout initialized");
        }



        private void ScanNow(object source, ElapsedEventArgs e)
        {
            //scan USB 
            ScanUsb();


            //currentDeviceList.RemoveOldDevices(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * ScoutHelper.DefaultNumPeriodsToForgetDevice);

            //platform.ProcessNewDiscoveryResults(currentDeviceList.GetClonedList());
        }

        void ScanUsb()
        {
            logger.Log("ArduinoScout:ScanUSB\n");

            string[] portNames = SerialPort.GetPortNames();

            //we are putting this lock here just in case two of these are running at the same time (e.g., if the ScanNow timer was really short)
            //only one of these calls can be active because of socket conflicts
            lock (this)
            {
                foreach (string portName in portNames)
                {
                    try
                    {
                        int portNumber = int.Parse(portName.Substring(3));

                        //skip low nubmered ports; they cannot be our device
                        //we can't skip Com 1..16 because Win8 assigns low-numbered ports starting from COM3
                        if (portNumber < 3)
                            continue;

                        SerialPort serPort;

                        if (!serialPorts.ContainsKey(portName))  {
                           
                           serPort = new SerialPort(portName, 9600);
                            serPort.DtrEnable = true;  //all this needed for arduino micro
                            serPort.WriteTimeout = 500;
                            serPort.ReadTimeout = 1000;  //enough time for the device to respond?
                            serPort.StopBits = StopBits.One;
                            serPort.Parity = Parity.None;
                            serPort.Handshake = Handshake.None;
                            serPort.DataBits = 8;
                            serPort.RtsEnable = false;
                            
                            serialPorts.Add(portName, serPort);
                        }
                        else {
                            serPort = serialPorts[portName];
                        }

                        int maxAttemptsToOpen = 1;

                        while (!serPort.IsOpen && maxAttemptsToOpen > 0) {
                            try
                            {
                                serPort.Open();
                                break;
                            }
                            catch (Exception e)
                            {
                                maxAttemptsToOpen--;

                                logger.Log("Got {0} exception while opening {1}. Num attempts left = {2}",
                                            e.Message, portName, maxAttemptsToOpen.ToString());

                                //sleep if we'll atttempt again
                                if (maxAttemptsToOpen > 0)
                                    System.Threading.Thread.Sleep(1 * 1000);
                            }
                        }

                        if (serPort.IsOpen)  //if we got it open
                        {

                            serPort.Write("[?]");

                            string serialString = serPort.ReadTo("]");
                            serialString = serialString.TrimStart('[');  //remove opening bracket

                            //check if this the device we want
                            if (serialString.Contains("HomeOSArduinoDevice"))
                            {
                                serialPortofArduino = portName; //remember the name of the correct port                            
                                Device device = CreateDeviceUsb(serialString);
                                currentDeviceList.InsertDevice(device);
                            }



                            serPort.Close();
                        }
                    }
                    catch (TimeoutException)
                    {
                        logger.Log("Timed out while trying to read " + portName);
                    }
                    catch (Exception e)
                    {
                        // let us not print this 
                        //logger.Log("Serial port exception for {0}: {1}", portName, e.ToString());
                    }
                }
            }
        }


        //this is for devices discovered over Usb
        private Device CreateDeviceUsb(string serialString)
        {
            var devId = serialString;

            string driverName = GetDriverName(devId);
            string friendlyName = GetFriendlyName(devId);
            var device = new Device(friendlyName, devId, "", DateTime.Now, driverName, false);

            //intialize the parameters for this device
            device.Details.DriverParams = new List<string>() { device.UniqueName, serialPortofArduino };

            return device;
        }


        private string GetFriendlyName(string devId)
        {
            //Example: HomeOSArduinoDevice_ConChair_MicrosoftResearch_1234
            var split = devId.Split('_');
            if (split.Count() != 4)
            {
                logger.Log("ERROR::cannot find Arduino device friendly name - incorrect number of underscores: " + devId);
                return devId;
            }
            return split[1].Replace(" ", "") + ":" + split[3];

            
        }

        private string GetDriverName(string devId)
        {
            // devId is HomeOSArduinoDevice_ConChair_MicrosoftResearch_31256042363112461688 
            // driver will be HomeOS.Hub.Drivers.Arduino.ManufacturerName.DeviceType  (i.e. last two fields are MicrosoftResearch.ConChair in above example)

            var split = devId.Split('_');
            if (split.Count() != 4)
            {
                logger.Log("ERROR::cannot find Arduino driver - incorrect number of underscores: " + devId);
                return "unknown";
            }
            return "HomeOS.Hub.Drivers.Arduino." + split[2].Replace(" ", "") + "." + split[1].Replace(" ", "");
        }



        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    scoutService.Dispose();
                    appServer.Dispose();
                }

                disposed = true;
            }
        }

        public List<Device> GetDevices()
        {
           
            ScanNow(null, null);

            return currentDeviceList.GetClonedList();
        }


        internal string GetInstructions()
        {
            return "Placeholder for instructions for Arduino scout";
        }
    }
}
