using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Threading;
using System.Drawing;
using System.Xml;
using System.Net;

namespace HomeOS.Hub.Drivers.HueBridge
{

    /// <summary>
    /// A dummy driver module that 
    /// 1. opens and register a dummy ports
    /// 2. sends periodic notifications  (in Work())
    /// 3. sends back responses to received echo requests (in OnOperationInvoke())
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Drivers.HueBridge")]
    public class HueBridge : Common.ModuleBase
    {
        SafeThread workThread = null;

        //string baseUrl;
        private WebFileServer imageServer;

        LightsManager lightManager = null;

        string bridgeId;
        string bridgeUser;

        IPAddress bridgeIp;

        Dictionary<Port, LightState> portToLightState = new Dictionary<Port, LightState>();

        public override void Start()
        {

            logger.Log("Started: {0}", ToString());

            try
            {
                string[] words = moduleInfo.Args();

                bridgeId = words[0];
                bridgeUser = words[1];
            }
            catch (Exception e)
            {
                logger.Log("{0}: Improper arguments: {1}. Exiting module", this.ToString(), e.ToString());
                return;
            }

            //get the IP address
            bridgeIp = GetBridgeIp(bridgeId);

            if (bridgeIp == null)
                return;

            lightManager = new LightsManager(bridgeIp, bridgeUser, logger);

            
            workThread = new SafeThread(delegate() {InitBridge(); } , "HueBridge init thread" , logger);
            workThread.Start();

            imageServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
        }

        private void InitBridge()
        {

            while (!lightManager.CanReachBridge())
            {
                //sleep, look for the new IP, repeat
                System.Threading.Thread.Sleep(10 * 1000);
                bridgeIp = GetBridgeIp(bridgeId);

                if (bridgeIp == null)
                {
                    logger.Log("Got null IP for hue bridge. Quitting");
                    return;
                }

                lightManager.SetBridgeIp(bridgeIp);
            }

            lightManager.Init();

            var allLights = lightManager.GetAllLights();

            foreach (var lstate in allLights)
            {
                //.................instantiate the port
                var port = InitPort(GetPortInfoFromPlatform("hb:"+lstate.Name));

                //remember the port to lightstate mapping
                portToLightState.Add(port, lstate);

                OperationDelegate handler = delegate(string roleName, string opName, IList<VParamType> list)
                {
                    return OnOperationInvoke(port, roleName, opName, list);
                };

                //..... bind the port to roles and delegates
                List<VRole> listRole = new List<VRole>() { RoleSwitchMultiLevel.Instance, RoleLightColor.Instance };
                BindRoles(port, listRole, handler);
                
                RegisterPortWithPlatform(port);
            }
        }

        public IPAddress GetBridgeIp(string cameraId)
        {

            //if the Id is an IP Address itself, return that.
            //else get the Ip from platform

            IPAddress ipAddress = null;

            try
            {
                ipAddress = IPAddress.Parse(cameraId);
                return ipAddress;
            }
            catch (Exception)
            {
            }

            string ipAddrStr = GetDeviceIpAddress(cameraId);

            try
            {
                ipAddress = IPAddress.Parse(ipAddrStr);
                return ipAddress;
            }
            catch (Exception)
            {
                logger.Log("{0} couldn't get IP address from {1} or {2}", this.ToString(), cameraId, ipAddrStr);
            }

            return null;
        }

        public override void Stop()
        {
            logger.Log("Stop() at {0}", ToString());

            if (workThread != null)
                workThread.Abort();

            if (imageServer != null)
                imageServer.Dispose();
        }

        ///// <summary>
        ///// The demultiplexing routing for incoming operations
        ///// </summary>
        ///// <param name="message"></param>
        //public IList<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> args)
        //{
        //    switch (opName.ToLower())
        //    {
        //        case RoleHueBridge.OpToggleAll:
        //            {
        //                logger.Log("{0} Got {1}", this.ToString(), opName.ToLower());

        //                lightManager.ToggleAllLights();

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpTurnOffAll:
        //            {
        //                logger.Log("{0} Got {1}", this.ToString(), opName.ToLower());

        //                lightManager.TurnAllLightsOff();

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpTurnOnAll:
        //            {
        //                logger.Log("{0} Got {1}", this.ToString(), opName.ToLower());

        //                lightManager.TurnAllLightsOn();

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpResetAll:
        //            {
        //                logger.Log("{0} Got {1}", this.ToString(), opName.ToLower());

        //                lightManager.ResetAllBulbs();

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpUnlockAll:
        //            {
        //                logger.Log("{0} Got {1}", this.ToString(), opName.ToLower());

        //                lightManager.SetAllLockLevel(0);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpSetColorAll:
        //            {
        //                int iRed = (int)args[0].Value();
        //                int iBlue = (int)args[1].Value();
        //                int iGreen = (int)args[2].Value();

        //                logger.Log("{0} Got {1} {2} {3} {4}", this.ToString(), opName.ToLower(), iRed.ToString(), iBlue.ToString(), iGreen.ToString());

        //                System.Drawing.Color color = System.Drawing.Color.FromArgb(iRed, iBlue, iGreen);

        //                lightManager.SetGroupColor("All", color);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpSetBrightnessAll:
        //            {

        //                float fBrightness = ((float)((int)args[0].Value())) / 1000.0f;

        //                logger.Log("{0} Got {1} {2}", this.ToString(), opName.ToLower(), fBrightness.ToString());

        //                lightManager.SetGroupBrightness("All", fBrightness);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpToggleBulb:
        //            {

        //                int bulbID = (int)args[0].Value();
        //                int lockLevel = (int)args[1].Value();
        //                logger.Log("{0} Got {1} {2} {3}", this.ToString(), opName.ToLower(), bulbID.ToString(), lockLevel.ToString());


        //                lightManager.ToggleBulb(bulbID, lockLevel);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpTurnOffBulb:
        //            {
        //                int bulbID = (int)args[0].Value();
        //                int lockLevel = (int)args[1].Value();
        //                logger.Log("{0} Got {1} {2} {3}", this.ToString(), opName.ToLower(), bulbID.ToString(), lockLevel.ToString());

        //                lightManager.TurnOffBulb(bulbID, lockLevel);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpTurnOnBulb:
        //            {

        //                int bulbID = (int)args[0].Value();
        //                int lockLevel = (int)args[1].Value();
        //                logger.Log("{0} Got {1} {2} {3}", this.ToString(), opName.ToLower(), bulbID.ToString(), lockLevel.ToString());


        //                lightManager.TurnOnBulb(bulbID, lockLevel);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpResetBulb:
        //            {
        //                int bulbID = (int)args[0].Value();
        //                logger.Log("{0} Got {1} {2}", this.ToString(), opName.ToLower(), bulbID.ToString());


        //                lightManager.ResetBulb(bulbID);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpUnlockBulb:
        //            {
        //                int bulbID = (int)args[0].Value();
        //                logger.Log("{0} Got {1} {2}", this.ToString(), opName.ToLower(), bulbID.ToString());


        //                lightManager.SetSingleLockLevel(bulbID, 0);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpSetColorBulb:
        //            {
        //                int bulbID = (int)args[0].Value();
        //                int lockLevel = (int)args[1].Value();
        //                int iRed = (int)args[2].Value();
        //                int iBlue = (int)args[3].Value();
        //                int iGreen = (int)args[4].Value();

        //                logger.Log("{0} Got {1} {2} {3} {4} {5} {6}", this.ToString(), opName.ToLower(), bulbID.ToString(), lockLevel.ToString(), iRed.ToString(), iBlue.ToString(), iGreen.ToString());


        //                System.Drawing.Color color = System.Drawing.Color.FromArgb(iRed, iBlue, iGreen);
        //                lightManager.SetLightColor(bulbID, color, lockLevel);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpGetColorBulb:
        //            {
        //                int bulbID = (int)args[0].Value();
        //                logger.Log("{0} Got {1} {2}", this.ToString(), opName.ToLower(), bulbID.ToString());


        //                System.Drawing.Color color = lightManager.GetLightColor(bulbID);

        //                List<VParamType> retVals = new List<VParamType>();
        //                retVals.Add(new ParamType(true));
        //                retVals.Add(new ParamType(ParamType.SimpleType.integer, "red", (int)color.R));
        //                retVals.Add(new ParamType(ParamType.SimpleType.integer, "green", (int)color.G));
        //                retVals.Add(new ParamType(ParamType.SimpleType.integer, "blue", (int)color.B));

        //                return retVals;
        //            }

        //        case RoleHueBridge.OpSetBrightnessBulb:
        //            {
        //                int bulbID = (int)args[0].Value();
        //                int lockLevel = (int)args[1].Value();
        //                float fBrightness = ((float)((int)args[2].Value())) / 1000.0f;

        //                logger.Log("{0} Got {1} {2} {3}", this.ToString(), opName.ToLower(), bulbID.ToString(), lockLevel.ToString(), fBrightness.ToString());

        //                lightManager.SetLightBrightness(bulbID, fBrightness, lockLevel);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }
        //        case RoleHueBridge.OpBumpBulb:
        //            {
        //                int bulbID = (int)args[0].Value();

        //                logger.Log("{0} Got {1} {2}", this.ToString(), opName.ToLower(), bulbID.ToString());

        //                lightManager.BumpBulb(bulbID);

        //                List<VParamType> retVals = new List<VParamType>() { new ParamType(true) };

        //                return retVals;
        //            }
        //        default:
        //            logger.Log("Invalid operation: {0}", opName);
        //            return null;
        //    }
        //}

        /// <summary>
        /// The demultiplexing routing for incoming operations
        /// </summary>
        /// <param name="message"></param>
        public IList<VParamType> OnOperationInvoke(Port targetLightPort, string roleName, String opName, IList<VParamType> args)
        {
            logger.Log("{0} Got {1} {2}", this.ToString(), opName.ToLower(), targetLightPort.ToString());


            //send back an error if we don't know of this port
            if (!portToLightState.ContainsKey(targetLightPort))
            {
                logger.Log("Got request for unknown light!");

                return new List<VParamType>() { new ParamType(ParamType.SimpleType.error, "unknown port " + targetLightPort.GetInfo().GetFriendlyName()) };
            }

            LightState lstate = portToLightState[targetLightPort];

            switch (opName.ToLower())
            {
                case RoleSwitchMultiLevel.OpSetName:
                    {
                        double valToSetDbl = (double)args[0].Value();

                        if (valToSetDbl > 1) valToSetDbl = 1;

                        byte valToSet = (byte) (valToSetDbl * 255);

                        byte currVal = lstate.Brightness;

                        //if (currVal != valToSet)
                        //{
                            lightManager.SetLightBrightness(lstate, valToSet);

                            Notify(targetLightPort, RoleSwitchMultiLevel.Instance, RoleSwitchMultiLevel.OpGetName, new ParamType(valToSetDbl));

                            logger.Log("{0}: issued notification for light {1}, value {2}", ToString(), targetLightPort.ToString(), valToSetDbl.ToString());
                        //}

                        return new List<VParamType>();

                    }
                case RoleSwitchMultiLevel.OpGetName:
                    {
                        byte value = lstate.Brightness;

                        IList<VParamType> retVals = new List<VParamType>() { new ParamType( (double) value / 255.0) };

                        return retVals;
                    }
                case RoleLightColor.OpSetName:
                    {
                        byte red, green, blue;
                        Color color;

                        try
                        {
                            red = (byte)(int)args[0].Value();
                            green = (byte)(int)args[1].Value();
                            blue = (byte)(int)args[2].Value();

                            color = Color.FromArgb(red, green, blue);
                        }
                        catch (Exception ex)
                        {
                            logger.Log("Bad parameters for {0}", opName, ex.ToString());
                            return new List<VParamType>() { new ParamType(ParamType.SimpleType.error, "bad parameters for " + opName) };
                        }

                        if (!color.Equals(lstate.Color))
                        {
                            lightManager.SetLightColor(lstate, color);

                            Notify(targetLightPort, RoleLightColor.Instance, RoleLightColor.OpGetName,
                                new ParamType(red), new ParamType(green), new ParamType(blue));

                            logger.Log("{0}: issued color notification for light {1}, value {2}", ToString(), targetLightPort.ToString(), color.ToString());
                        }

                        return new List<VParamType>();
                    }
                case RoleLightColor.OpGetName:
                    {
                        IList<VParamType> retVals = new List<VParamType>() { new ParamType(lstate.Color.R), new ParamType(lstate.Color.G), new ParamType(lstate.Color.B) };

                        return retVals;
                    }
                default:
                    logger.Log("Unknown operation {0} for role {1}", opName, roleName);
                    return new List<VParamType>() { new ParamType(ParamType.SimpleType.unsupported, "unknown operation " + opName) };
            }
        }

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