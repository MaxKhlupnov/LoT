using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;
using System.Timers;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO.Ports;

namespace HomeOS.Hub.Scouts.Valve
{
    public class ValveScout : IScout
    {
        const int queryPortNumber = 48468;    //port where we should query the device
        const int responsePortNumber = 48467;   //port where the device responds
        const int listenPortNumber = 48469;   //port where gadgeteer sends unsolicited beacons

        static byte[] request = { 0x00, 0x00 };

        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        ValveScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        UdpClient listenClient;
        bool listenClientClosed = false;

        Dictionary<string, SerialPort> serialPorts = new Dictionary<string, SerialPort>();

        DeviceList currentDeviceList = new DeviceList();

        //these variables are for asynchronous receives
        byte[] asyncBuffer = new byte[2000];
        SocketFlags asyncSocketFlags = SocketFlags.None;
        EndPoint asyncRemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new ValveScoutService(baseUrl + "/webapp", this, platform, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            //IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, listenPortNumber);
            //listenClient = new UdpClient(endpoint);

            //listenClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            //listenClient.Client.BeginReceiveMessageFrom(asyncBuffer, 0, 2000, asyncSocketFlags, ref asyncRemoteEndPoint, new AsyncCallback(ReceiveCallback), null);

            //create a time that fires ScanNow() periodically
            var scanTimer = new Timer(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * 1000);
            scanTimer.Enabled = true;
            scanTimer.Elapsed += new ElapsedEventHandler(ScanNow);

            logger.Log("ValveScout initialized");
        }

        private void ScanNow(object source, ElapsedEventArgs e)
        {
            //we first scan USB and then Wifi because if a device is seen on both, we want the Wifi device
            ScanUsb();

            //ScanWifi();

            currentDeviceList.RemoveOldDevices(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * ScoutHelper.DefaultNumPeriodsToForgetDevice);

            platform.ProcessNewDiscoveryResults(currentDeviceList.GetClonedList());
        }

        public List<Device> GetDevices()
        {

            //lets trigger a scan now that the user wants to check on devices
            ScanNow(null, null);

            return currentDeviceList.GetClonedList();
        }

        void ScanUsb()
        {
            logger.Log("ValveScout:ScanUSB\n");
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

                        SerialPort port;

                        if (!serialPorts.ContainsKey(portName))
                        {
                            port = new SerialPort(portName, 93750, Parity.None, 8, StopBits.One);
                            port.Handshake = Handshake.None;
                            port.ReadTimeout = 5000;  //enough time for the device to respond?
                            port.WriteTimeout = 500;
                            serialPorts.Add(portName, port);
                        }
                        else
                        {
                            port = serialPorts[portName];
                        }

                        int maxAttemptsToOpen = 2;

                        while (!port.IsOpen && maxAttemptsToOpen > 0)
                        {

                            //Thread.Sleep(100);
                            try
                            {
                                port.Open();
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


                        port.WriteLine("init\r\n");

                        string serialString = port.ReadLine();
                        logger.Log(serialString);


                        //check if this the device we want
                        if (serialString.Contains("HomeOSValveDevice"))
                        {
                            Device device = CreateDeviceUsb(portName);
                            currentDeviceList.InsertDevice(device);
                        }

                        port.Close();
                    }
                    catch (TimeoutException e)
                    {
                        logger.Log("Timed out while trying to read " + portName + "(" + e.ToString() + ")");
                    }
                    catch (Exception e)
                    {
                        logger.Log("Serial port exception for {0}: {1}", portName, e.ToString());
                    }
                }
            }
        }



        private Device CreateDeviceUsb(string serialString)
        {
            var devId = "Valve+" + serialString;
            string driverName = GetDriverName(devId);
            var device = new Device(devId, devId, serialString, DateTime.Now, driverName, false);

            //intialize the parameters for this device
            device.Details.DriverParams = new List<string>() { device.UniqueName };

            return device;
        }

        private string GetDriverName(string devId)
        {
            return "HomeOS.Hub.Drivers.LancasterUni.Valve";
        }


        #region functions called from the UI

        internal string GetInstructions()
        {
            return "Placeholder for helping to find the valve device";
        }
        #endregion

        #region cleanup code
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

                    lock (this)
                    {
                        if (listenClient != null)
                        {
                            listenClient.Close();
                            listenClientClosed = true;
                        }
                    }
                }

                disposed = true;
            }
        }
        #endregion

    }
}
