using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;

namespace HomeOS.Hub.Scouts.SynapseWireless
{
    public class SynapseWirelessScout : IScout
    {

        const int queryPortNumber = 8400; //where we query the synapse device controller
        const int responsePortNumber = 8401; //where we get a response from the synapse device controller

        string baseUrl;
        ScoutViewOfPlatform platform;
        VLogger logger;

        Process process = null;
        ProcessStartInfo startinfo;
        const string synapseControllerDirectory = "C:\\synapseController\\";
        const string synapseControllerArgs = "DS_StartDataCollection.py";
        const string synapseControllerScript = "C:\\python27\\python.exe";


        SynapseWirelessScoutService scoutService;
        WebFileServer appServer;
        private bool disposed = false;

        public void Init(string baseUrl, string baseDir, ScoutViewOfPlatform platform, VLogger logger)
        {
            this.baseUrl = baseUrl;
            this.platform = platform;
            this.logger = logger;

            scoutService = new SynapseWirelessScoutService(baseUrl + "/webapp", this, logger);

            appServer = new WebFileServer(baseDir, baseUrl, logger);

            startSynapseController();

            logger.Log("SynapseWirelessScout initialized");
        }
        //kill the python process in here
        public void Dispose()
        {
            
            var client = new UdpClient(8401);
            Byte[] sendBytes = Encoding.ASCII.GetBytes("kill");
            client.Send(sendBytes, sendBytes.Length, "localhost", queryPortNumber);

            //logger.Log("SynapseController killed");

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

        //return a list of the synapse wireless connect to this hub
        //options are "doorjamb", "water fixture"
        public List<Device> GetDevices()
        {

            //query the controller for devices
            List<Device> retList = ScanDevices();
            //wait for device listing response (Device Name and Device ID (synapseID))
            return retList;

        }

        private List<Device> ScanDevices()
        {
            List<Device> retList = new List<Device>();
            //only one of these calls can be active because of socket conflicts
            lock (this)
            {
                logger.Log("SynapseWirelessScout:ScanningDevices\n");

                using (var client = new UdpClient(8401))
                {
                    
                    client.Client.ReceiveTimeout = 1000;

                    Byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");
                    client.Send(sendBytes, sendBytes.Length, "localhost", queryPortNumber);
                    try
                    {
                        // loop until you timeout
                        while (true)
                        {
                            //IPEndPoint object will allow us to read datagrams sent from any source.
                            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                            // Blocks until a message returns on this socket from a remote host.
                            Byte[] receiveBytes = client.Receive(ref RemoteIpEndPoint);
                            string returnData = Encoding.ASCII.GetString(receiveBytes);

                            //create device
                            string deviceName = returnData;
                            string driverName = GetDriverName(deviceName);
                            string deviceID = deviceName;
                            Device device = new Device(deviceName, deviceID, "", DateTime.Now, driverName, false);

                            //intialize the parameters for this device
                            device.Details.DriverParams = new List<string>() { device.UniqueName };
                            //logger.Log("Put device on deivcelist: {0} \n", device.ToString());

                            retList.Add(device);
                        }
                    }
                    catch
                    {
                        ;
                    }
                }
            }
            return retList;
        }

        private string GetDriverName(string deviceName)
        {
            if (deviceName.Contains("Doorjamb"))
                return "HomeOS.Hub.Drivers.Doorjamb";
            else if (deviceName.Contains("Water Fixture"))
                return "HomeOS.Hub.Drivers.WaterFixture";
            else
            {
                logger.Log("ERROR::cannot find synapse wireless driver for " + deviceName);
                return "unknown";
            }
        }

        private void startSynapseController()
        {

            //logger.Log("STARTING SYNAPSE CONTROLLER!!");


            startinfo = new ProcessStartInfo("Synapse Controller");
            startinfo.WorkingDirectory = synapseControllerDirectory;
            startinfo.Arguments = synapseControllerArgs;
            startinfo.FileName = synapseControllerScript;
            startinfo.UseShellExecute = false;
            startinfo.CreateNoWindow = true;
            startinfo.RedirectStandardOutput = true;
            startinfo.RedirectStandardError = true;

            process = new Process();
            process.StartInfo = startinfo;
            process.Start();
            //logger.Log("STARTED SYNAPSE CONTROLLER!!");
        }

        internal string GetInstructions()
        {
            return "Placeholder for instructions to help discover synapse wireless device";
        }
    }
}
