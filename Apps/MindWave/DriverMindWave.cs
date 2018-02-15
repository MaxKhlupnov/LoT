using HomeOS.Hub.Common;
using HomeOS.Hub.Custom;
using HomeOS.Hub.Platform.Views;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace HomeOS.Hub.Drivers.MindWave
{
    //Nicholas Kostriken

    /// <summary>
    /// A driver module that 
    /// 1. Connects to ThinkGearConnector's TCP port
    /// 2. Makes headset data available through simple accessors
    /// </summary>

    class MindWaveInfo
    {
        //The current headset connection strength
        public int connection { get; set; }

        //The current attention/meditation level
        public int attention { get; set; }
        public int meditation { get; set; }
        
        //The current values of the EEG spectrum
        public int delta { get; set; }
        public int theta { get; set; }
        public int lowAlpha { get; set; }
        public int highAlpha { get; set; }
        public int lowBeta { get; set; }
        public int highBeta { get; set; }
        public int lowGamma { get; set; }
        public int highGamma { get; set; }

        //A list of the most recent blink events
        //public List<DateTime> blinks = new List<DateTime>{};
        public int blink = 0;
    }


    [System.AddIn.AddIn("HomeOS.Hub.Drivers.MindWave")]
    
    public class DriverMindWave :  ModuleBase
    {
        SafeThread workThread = null; 
        Port mindWavePort;
        MindWaveInfo mindWaveInfo = new MindWaveInfo();
        TcpClient client;
        private WebFileServer imageServer;

        /// <summary>
        /// Tries to start a connection with the "ThinkGearConnector" app.
        /// </summary>
        public override void Start()
        {
            //Try to connect to "ThinkGearConnector"
            try { client = new TcpClient("127.0.0.1", 13854); }
            catch { throw new Exception("You must install the \"ThinkGearConnector\" [http://developer.neurosky.com/docs/doku.php?id=thinkgear_connector_tgc]"); }

            logger.Log("Started: {0}", ToString());
            
            string mindWaveDevice = moduleInfo.Args()[0];

            //Instantiate the port
            VPortInfo portInfo = GetPortInfoFromPlatform(mindWaveDevice);
            mindWavePort = InitPort(portInfo);
            //Initialize the list of roles we are going to export and bind to the role
            List<VRole> listRole = new List<VRole>() { RoleMindWave.Instance };
            BindRoles(mindWavePort, listRole);
            //Register the port after the binding is complete
            RegisterPortWithPlatform(mindWavePort);

            workThread = new SafeThread(delegate() { Work(); } , "DriverMindWave work thread" , logger);
            workThread.Start();

            imageServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
        }

        /// <summary>
        /// Stops any connection to "ThinkGearConnector" etc.
        /// </summary>
        public override void Stop()
        {
            logger.Log("Stop() at {0}", ToString());

            if (workThread != null)
                workThread.Abort();

            imageServer.Dispose();
        }


        /// <summary>
        /// Tries to obtain JSON data packets from "ThinkGearConnector"
        /// </summary>
        public void Work()
        {
            NetworkStream stream = client.GetStream();
            Byte[] buffer = new Byte[8192];
            String response = String.Empty;

            //Request data in json format
            Byte[] data = System.Text.Encoding.ASCII.GetBytes("{\"enableRawOutput\":false,\"format\":\"Json\"}");
            int bytes;


            bool json = false;
            Console.Out.WriteLine(">> Waiting for MindWave data stream...");
            while (true)
            {
                bytes = stream.Read(buffer, 0, buffer.Length);
                response = System.Text.Encoding.ASCII.GetString(buffer, 0, bytes);

                try
                {
                    string[] packets = response.Split('\r');
                    foreach (string packet in packets)
                    {
                        JToken d = JObject.Parse(packet);

                        //Parse out any blinks
                        int blink = Convert.ToInt32(d["blinkStrength"]);
                        mindWaveInfo.blink = blink;

                        //Parse out the connection strength
                        mindWaveInfo.connection = Convert.ToInt32(d["poorSignalLevel"]);

                        //Parse out the attention/meditation data
                        mindWaveInfo.attention = Convert.ToInt32(d["eSense"]["attention"]);
                        mindWaveInfo.meditation = Convert.ToInt32(d["eSense"]["meditation"]);

                        //Parse out the EEG data
                        mindWaveInfo.delta = Convert.ToInt32(d["eegPower"]["delta"]);
                        mindWaveInfo.theta = Convert.ToInt32(d["eegPower"]["theta"]);
                        mindWaveInfo.lowAlpha = Convert.ToInt32(d["eegPower"]["lowAlpha"]);
                        mindWaveInfo.highAlpha = Convert.ToInt32(d["eegPower"]["highAlpha"]);
                        mindWaveInfo.lowBeta = Convert.ToInt32(d["eegPower"]["lowBeta"]);
                        mindWaveInfo.highBeta = Convert.ToInt32(d["eegPower"]["highBeta"]);
                        mindWaveInfo.lowGamma = Convert.ToInt32(d["eegPower"]["lowGamma"]);
                        mindWaveInfo.highGamma = Convert.ToInt32(d["eegPower"]["highGamma"]);

                        if (!json)
                            Console.Out.WriteLine(">> MindWave data is streaming!");
                            json = true;
                    }
                }
                catch {
                    //If the data is being sent as binary instead of JSON, request it as JSON
                    if (!json)
                    {
                        stream.Write(data, 0, data.Length);
                        System.Threading.Thread.Sleep(1000);
                    }
                }

            }
        }

        
        /// <summary>
        /// Accessor for connection quality
        /// </summary>
        /// <returns>Quality of the connection : 0=Connected, 200=OffHead</returns>
        public int GetConnection()
        {
            return mindWaveInfo.connection;
        }

        /// <summary>
        /// Accessor for current attention level
        /// </summary>
        /// <returns>Attention level : 0=Min, 100=Max</returns>
        public int GetAttention()
        {
            return mindWaveInfo.attention;
        }

        /// <summary>
        /// Accessor for current meditation level
        /// </summary>
        /// <returns>Meditation level : 0=Min, 100=Max</returns>
        public int GetMeditation()
        {
            return mindWaveInfo.meditation;
        }

        /// <summary>
        /// Accessor for the raw EEG wave values
        /// </summary>
        /// <returns>A list of the unitless EEG levels</returns>
        public List<VParamType> GetWaves()
        {
            return new List<VParamType>() { new ParamType(mindWaveInfo.delta), new ParamType(mindWaveInfo.theta), new ParamType(mindWaveInfo.lowAlpha), new ParamType(mindWaveInfo.highAlpha),
                new ParamType(mindWaveInfo.lowBeta), new ParamType(mindWaveInfo.highBeta), new ParamType(mindWaveInfo.lowGamma), new ParamType(mindWaveInfo.highGamma) };
        }

        /// <summary>
        /// Accessor for the most recent blink strength. 0=none, 200=max strength
        /// </summary>
        /// <returns>The strength of the last blink</returns>
        public int GetBlink()
        {
            int blink = mindWaveInfo.blink;
            mindWaveInfo.blink = 0;

            return blink;
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

            if (!roleName.Equals(RoleMindWave.RoleName))
            {
                logger.Log("Invalid role {0} in OnInvoke", roleName);
                return null;
            }

            switch (opName.ToLower())
            {
                //Return connection quality
                case RoleMindWave.OpGetConnection:
                    return new List<VParamType>() { new ParamType(GetConnection()) };
                //Return attention level
                case RoleMindWave.OpGetAttention:
                    return new List<VParamType>() { new ParamType(GetAttention()) };
                //Return meditation level
                case RoleMindWave.OpGetMeditation:
                    return new List<VParamType>() { new ParamType(GetMeditation()) };
                //Return individual wave values
                case RoleMindWave.OpGetWaves:
                    return GetWaves();
                //Return concentration indicator
                case RoleMindWave.OpGetBlinks:
                    return new List<VParamType>() { new ParamType( GetBlink() ) };

                default:
                    logger.Log("Invalid operation: {0}", opName);
                    return null;
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        public override void PortRegistered(VPort port) {}

        /// <summary>
        ///  Called when a new port is deregistered with the platform
        /// </summary>
        public override void PortDeregistered(VPort port) { }
    }



}