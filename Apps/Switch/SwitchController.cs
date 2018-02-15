using System;
using System.Collections.Generic;
using System.Threading;
using System.AddIn;
using HomeOS.Hub.Common;
using System.ServiceModel;
using HomeOS.Hub.Platform.Views;
using System.Drawing;
//using System.Threading.Tasks;
//using System.Windows.Threading;
using System.Timers;

namespace HomeOS.Hub.Apps.Switch
{
    public enum SwitchType { Binary, Multi };

    class SwitchInfo
    {
        public VCapability Capability { get; set; }
        public double Level { get; set; }
        public SwitchType Type { get; set; }

        public bool IsColored { get; set; }
        public Color Color {get; set;}
    }

    [AddIn("HomeOS.Hub.Apps.Switch")]
    public class SwitchMultiLevelController : Common.ModuleBase
    {
        private static TimeSpan OneSecond = new TimeSpan(0, 0, 1);
        DateTime lastSet = DateTime.Now - OneSecond;

        Dictionary<VPort, SwitchInfo> registeredSwitches = new Dictionary<VPort, SwitchInfo>();
        Dictionary<string, VPort> switchFriendlyNames = new Dictionary<string, VPort>();

        //For Speech
        List<VPort> speechPorts = new List<VPort>();

        private SafeServiceHost serviceHost;
        private WebFileServer appServer;

        //for doing the disco thing speech command is "GO DISCO" (if you have speech reco role installed)
        private static System.Timers.Timer discoTimer;
        private static int countDiscoEvents = 0;
        private const int maxDiscoEvents = 5;
        private Color[] colorChoices = new Color[] { Color.Red, Color.Pink, Color.Blue, Color.Purple, Color.Orange };


        public override void Start()
        {
            logger.Log("Started: {0}", ToString());

            SwitchSvc service = new SwitchSvc(logger, this);
            serviceHost = new SafeServiceHost(logger, typeof(ISwitchSvcContract), service, this, Constants.AjaxSuffix, moduleInfo.BaseURL());
            serviceHost.Open();

            appServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);

            logger.Log("switch controller is open for business at " + moduleInfo.BaseURL());

            //..... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();
            foreach (VPort port in allPortsList)
            {
                PortRegistered(port);
            }
        }

        internal double GetLevel(string switchFriendlyName)
        {
            if (switchFriendlyNames.ContainsKey(switchFriendlyName))
                return registeredSwitches[switchFriendlyNames[switchFriendlyName]].Level;

            return 0;
        }

  
        public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        {
            lock (this)
            {
                //check if notification is speech event
                if (roleName.Contains(RoleSpeechReco.RoleName) && opName.Equals(RoleSpeechReco.OpPhraseRecognizedSubName))
                { 
                    string rcvdCmd = (string)retVals[0].Value();
   
                    switch (rcvdCmd)
                    {
                       case "ALLON":
                            SetAllSwitches(1.0);
                           break;

                        case "ALLOFF":
                           SetAllSwitches(0.0); 
                        break;

                        case "PLAYMOVIE":
                            SetAllSwitches(0.1);
                        break;

                        case "DISCO":
                            DiscoSwitches();
                        break;

                    }
                    return;
                }

                if (!registeredSwitches.ContainsKey(senderPort))
                    throw new Exception("Got notification from an unknown port " + senderPort.ToString());

                switch (opName)
                {
                    case RoleSwitchBinary.OpGetName:
                        {
                            if (retVals.Count >= 1 && retVals[0].Value() != null)
                            {
                                bool level = (bool)retVals[0].Value();

                                registeredSwitches[senderPort].Level = (level)? 1 : 0;
                            }
                            else
                            {
                                logger.Log("{0} got bad result for getlevel subscription from {1}", this.ToString(), senderPort.ToString());
                            }
                        }
                        break;
                    case RoleSwitchMultiLevel.OpGetName:
                        {
                            if (retVals.Count >= 1 && retVals[0].Value() != null)
                            {
                                double level = (double)retVals[0].Value();

                                registeredSwitches[senderPort].Level = level;
                            }
                            else
                            {
                                logger.Log("{0} got bad result for getlevel subscription from {1}", this.ToString(), senderPort.ToString());
                            }
                        }
                        break;
                    case RoleLightColor.OpGetName:
                        {
                            if (!registeredSwitches[senderPort].IsColored)
                            {
                                logger.Log("Got {0} for non-colored switch {1}", opName, senderPort.ToString());

                                return;
                            }

                            if (retVals.Count >= 3)
                            {
                                byte red, green, blue;

                                red = Math.Min(Math.Max((byte)(int)retVals[0].Value(), (byte)0), (byte)255);
                                green = Math.Min(Math.Max((byte)(int)retVals[1].Value(), (byte)0), (byte)255);
                                blue = Math.Min(Math.Max((byte)(int)retVals[2].Value(), (byte)0), (byte)255);

                                registeredSwitches[senderPort].Color = Color.FromArgb(red, green, blue);
                            }
                            else
                            {
                                logger.Log("{0} got bad result for getlevel subscription from {1}", this.ToString(), senderPort.ToString());
                            }
                        }
                        break;
                    default:
                        logger.Log("Got notification from incomprehensible operation: " + opName);
                        break;
                }
            }
        }

       

        public override void Stop()
        {
            lock (this)
            {
                if (serviceHost != null)
                    serviceHost.Close();

                if (appServer != null)
                    appServer.Dispose();
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        /// <param name="port"></param>
        public override void PortRegistered(VPort port)
        {
            lock (this)
            {
                if (Role.ContainsRole(port, RoleSwitchMultiLevel.RoleName) ||
                    Role.ContainsRole(port, RoleSwitchBinary.RoleName) ||
                    Role.ContainsRole(port, RoleLightColor.RoleName))
                {
                    if (!registeredSwitches.ContainsKey(port) &&
                        GetCapabilityFromPlatform(port) != null)
                    {
                        var switchType = (Role.ContainsRole(port, RoleSwitchMultiLevel.RoleName)) ? SwitchType.Multi : SwitchType.Binary;

                        bool colored = Role.ContainsRole(port, RoleLightColor.RoleName);

                        InitSwitch(port, switchType, colored);
                    }
                   
                }

                else if (Role.ContainsRole(port, RoleSpeechReco.RoleName))
                {

                    if (!speechPorts.Contains(port) &&
                        GetCapabilityFromPlatform(port) != null)
                    {

                        speechPorts.Add(port);

                        logger.Log("SwitchController:{0} added speech port {1}", this.ToString(), port.ToString());


                        //TODO Call it with phrases we care about - FOR NOW HARD CODED in Kinect driver
                        //  var retVal = Invoke(port, RoleSpeechReco.Instance, RoleSpeechReco.OpSetSpeechPhraseName, new ParamType(ParamType.SimpleType.text, "on"));

                        //subscribe to speech reco
                        if (Subscribe(port, RoleSpeechReco.Instance, RoleSpeechReco.OpPhraseRecognizedSubName))
                            logger.Log("{0} subscribed to port {1}", this.ToString(), port.ToString());
                    }
                }
            }
        }

        void InitSwitch(VPort switchPort, SwitchType switchType, bool isColored)
        {

            logger.Log("{0} adding switch {1} {2}", this.ToString(), switchType.ToString(), switchPort.ToString());

            SwitchInfo switchInfo = new SwitchInfo();
            switchInfo.Capability = GetCapability(switchPort, Constants.UserSystem);
            switchInfo.Level = 0;
            switchInfo.Type = switchType;

            switchInfo.IsColored = isColored;
            switchInfo.Color = Color.Black;

            registeredSwitches.Add(switchPort, switchInfo);

            string switchFriendlyName = switchPort.GetInfo().GetFriendlyName();
            switchFriendlyNames.Add(switchFriendlyName, switchPort);

            if (switchInfo.Capability != null)
            {
                IList<VParamType> retVals;

                if (switchType == SwitchType.Multi)
                {
                    retVals = switchPort.Invoke(RoleSwitchMultiLevel.RoleName, RoleSwitchMultiLevel.OpGetName, null,
                    ControlPort, switchInfo.Capability, ControlPortCapability);

                    switchPort.Subscribe(RoleSwitchMultiLevel.RoleName, RoleSwitchMultiLevel.OpGetName, ControlPort, switchInfo.Capability, ControlPortCapability);

                    if (retVals[0].Maintype() < 0)
                    {
                        logger.Log("SwitchController could not get current level for {0}", switchFriendlyName);
                    }
                    else
                    {
                        switchInfo.Level = (double)retVals[0].Value();
                    }
                }
                else
                {
                    retVals = switchPort.Invoke(RoleSwitchBinary.RoleName, RoleSwitchBinary.OpGetName, null,
                    ControlPort, switchInfo.Capability, ControlPortCapability);

                    switchPort.Subscribe(RoleSwitchBinary.RoleName, RoleSwitchBinary.OpGetName, ControlPort, switchInfo.Capability, ControlPortCapability);

                    if (retVals[0].Maintype() < 0)
                    {
                        logger.Log("SwitchController could not get current level for {0}", switchFriendlyName);
                    }
                    else
                    {
                        bool boolLevel = (bool)retVals[0].Value();
                        switchInfo.Level = (boolLevel) ? 1 : 0;
                    }
                }

                //fix the color up now

                if (isColored)
                {
                    var retValsColor = switchPort.Invoke(RoleLightColor.RoleName, RoleLightColor.OpGetName, null,
                                                          ControlPort, switchInfo.Capability, ControlPortCapability);

                    switchPort.Subscribe(RoleLightColor.RoleName, RoleLightColor.OpGetName, ControlPort, switchInfo.Capability, ControlPortCapability);

                    if (retVals[0].Maintype() < 0)
                    {
                        logger.Log("SwitchController could not get color for {0}", switchFriendlyName);
                    }
                    else
                    {
                        byte red, green, blue;

                        red = Math.Min(Math.Max((byte) (int) retValsColor[0].Value(), (byte) 0), (byte) 255);
                        green = Math.Min(Math.Max((byte) (int) retValsColor[1].Value(), (byte) 0), (byte)255);
                        blue = Math.Min(Math.Max((byte) (int) retValsColor[2].Value(), (byte) 0), (byte)255);

                        switchInfo.Color = Color.FromArgb(red, green, blue);
                    }
                }
            }
        }
        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        /// <param name="port"></param>
        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (registeredSwitches.ContainsKey(port))
                    ForgetSwitch(port);
            }
        }

        void ForgetSwitch(VPort switchPort)
        {
            switchFriendlyNames.Remove(switchPort.GetInfo().GetFriendlyName());

            registeredSwitches.Remove(switchPort);

            logger.Log("{0} removed switch/light port {1}", this.ToString(), switchPort.ToString());
        }

        public void Log(string format, params string[] args)
        {
            logger.Log(format, args);
        }

        internal void DiscoSwitches()
        {
            //do the first color change
            SetDiscoColor();

            // Set the Interval to 2 seconds (2000 milliseconds).
            discoTimer = new System.Timers.Timer(2000);
            // Hook up the Elapsed event for the timer.
            discoTimer.Elapsed += new ElapsedEventHandler(OnDiscoEvent);       
            discoTimer.Start();  
        }

        //Called by timer 5 times
        private  void OnDiscoEvent(object source, ElapsedEventArgs e)
        {
            if (countDiscoEvents <= maxDiscoEvents) {
                countDiscoEvents++;
                SetDiscoColor();
            }
            else {
                discoTimer.Stop();
                countDiscoEvents = 0;
            }
        }


        private void SetDiscoColor()
        {           
            var r = new Random(); //used to randomly pick color from array of colors

            foreach (KeyValuePair<string, VPort> switchs in switchFriendlyNames) {
                //check if switch has color and then set it.
                if (registeredSwitches[switchs.Value].IsColored) {
                    Color c = colorChoices[r.Next(0, colorChoices.Length - 1)];
                    SetColor(switchs.Key.ToString(), c); 
                }
            }  
        }

        
        internal void SetLevel(string switchFriendlyName, double level)
        {
            if (switchFriendlyNames.ContainsKey(switchFriendlyName))
            {
                VPort switchPort = switchFriendlyNames[switchFriendlyName];

                if (registeredSwitches.ContainsKey(switchPort))
                {
                    SwitchInfo switchInfo = registeredSwitches[switchPort];

                    IList<VParamType> args = new List<VParamType>();

                    //make sure that the level is between zero and 1
                    if (level < 0) level = 0;
                    if (level > 1) level = 1;

                    if (switchInfo.Type == SwitchType.Multi)
                    {
                        var retVal = Invoke(switchPort, RoleSwitchMultiLevel.Instance, RoleSwitchMultiLevel.OpSetName, new ParamType(level));

                        if (retVal != null && retVal.Count == 1 && retVal[0].Maintype() == (int)ParamType.SimpleType.error)
                        {
                            logger.Log("Error in setting level: {0}", retVal[0].Value().ToString());

                            throw new Exception(retVal[0].Value().ToString());
                        }
                    }
                    else
                    {
                        //interpret all non-zero values as ON
                        bool boolLevel = (level > 0)? true : false; 

                        var retVal = Invoke(switchPort, RoleSwitchBinary.Instance, RoleSwitchBinary.OpSetName, new ParamType(boolLevel));

                        if (retVal != null && retVal.Count == 1 && retVal[0].Maintype() == (int)ParamType.SimpleType.error)
                        {
                            logger.Log("Error in setting level: {0}", retVal[0].Value().ToString());

                            throw new Exception(retVal[0].Value().ToString());
                        }

                    }

                    lock (this)
                    {
                        this.lastSet = DateTime.Now;
                    }

                    switchInfo.Level = level;
                }
            }
            else
            {
                throw new Exception("Switch with friendly name " + switchFriendlyName + " not found");
            }
        }

       
        internal  void SetAllSwitches(double level)
        {
            foreach (KeyValuePair<string, VPort> switchs in switchFriendlyNames)
            {
                SetLevel(switchs.Key.ToString(), level);
            }
        }


        internal void SetColor(string switchFriendlyName, Color color)
        {
            if (switchFriendlyNames.ContainsKey(switchFriendlyName))
            {
                VPort switchPort = switchFriendlyNames[switchFriendlyName];

                if (registeredSwitches.ContainsKey(switchPort))
                {
                    SwitchInfo switchInfo = registeredSwitches[switchPort];

                    if (!switchInfo.IsColored)
                        throw new Exception("SetColor called on non-color switch " + switchFriendlyName);

                    IList<VParamType> args = new List<VParamType>();

                    var retVal = Invoke(switchPort, RoleLightColor.Instance, RoleLightColor.OpSetName,
                                        new ParamType(color.R), new ParamType(color.G), new ParamType(color.B));

                    if (retVal != null && retVal.Count == 1 && retVal[0].Maintype() == (int)ParamType.SimpleType.error)
                    {
                        logger.Log("Error in setting color: {0}", retVal[0].Value().ToString());
                        throw new Exception(retVal[0].Value().ToString());
                    }

                    lock (this)
                    {
                        this.lastSet = DateTime.Now;
                    }

                    switchInfo.Color = color;
                }
                else
                {
                    throw new Exception("Switch with friendly name " + switchFriendlyName + " is not registered");
                }
            }
            else
            {
                throw new Exception("Switch with friendly name " + switchFriendlyName + " not found");
            }
        }

        internal Color GetColor(string switchFriendlyName)
        {
            if (switchFriendlyNames.ContainsKey(switchFriendlyName))
            {
                VPort switchPort = switchFriendlyNames[switchFriendlyName];

                if (registeredSwitches.ContainsKey(switchPort))
                    {
                    SwitchInfo switchInfo = registeredSwitches[switchPort];

                    if (!switchInfo.IsColored)
                        throw new Exception("GetColor called on non-color switch " + switchFriendlyName);

                    return switchInfo.Color;
                    }
                else
                {
                    throw new Exception("Switch with friendly name " + switchFriendlyName + " is not registered");
                }
            }
            else
            {
                throw new Exception("Switch with friendly name " + switchFriendlyName + " not found");
            }
        }



        //returns a 8-tuple for each switch: (name, location, type, level, isColored, red, green, blue)
        internal List<string> GetAllSwitches()
        {
            List<string> retList = new List<string>();

            foreach (string friendlyName in switchFriendlyNames.Keys)
            {
                VPort switchPort = switchFriendlyNames[friendlyName];
                SwitchInfo switchInfo = registeredSwitches[switchPort];

                retList.Add(friendlyName);
                retList.Add(switchPort.GetInfo().GetLocation().ToString());
                retList.Add(switchInfo.Type.ToString());
                retList.Add(switchInfo.Level.ToString());

                retList.Add(switchInfo.IsColored.ToString());
                retList.Add(switchInfo.Color.R.ToString());
                retList.Add(switchInfo.Color.G.ToString());
                retList.Add(switchInfo.Color.B.ToString());
            }

            return retList;
        }

    }
}