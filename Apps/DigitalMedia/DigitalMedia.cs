using System;
using System.Net.Http;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ServiceModel;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common.Bolt.DataStore;
using Microsoft.AspNet.SignalR.Client;

namespace HomeOS.Hub.Apps.DigitalMedia
{

    /// <summary>
    /// A dummy a module that 
    /// 1. sends ping messages to all active dummy ports
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Apps.DigitalMedia")]
    public class DigitalMedia :  ModuleBase
    {
        //list of accessible dummy ports in the system
        List<VPort> accessibleDigitalMediaPorts;
        const string ServerURI = "http://localhost:8080";

        private SafeServiceHost serviceHost;

        private WebFileServer appServer;

        private EventHubSender azureEventHub = null;


        List<string> receivedMessageList  = new List<string>();
        SafeThread worker = null;

        IStream datastream;
        public IDisposable SignalR { get; set; }
        public HubConnection Connection { get; set; }
        public IHubProxy HubProxy { get; set; }

        public override void Start()
        {
            logger.Log("Started: {0} ", ToString());

            DigitalMediaService digitalMediaService = new DigitalMediaService(logger, this);
            serviceHost = new SafeServiceHost(logger,typeof(IDigitalMediaContract), digitalMediaService, 
                this, Constants.AjaxSuffix, moduleInfo.BaseURL());
            serviceHost.Open();

            appServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);

            // Initialize Azure EventHub as protocol
            this.azureEventHub = new EventHubSender();
             
            //........... instantiate the list of other ports that we are interested in
            accessibleDigitalMediaPorts = new List<VPort>();

            //..... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();

            if (allPortsList != null)
                ProcessAllPortsList(allPortsList);

            // remoteSync flag can be set to true, if the Platform Settings has the Cloud storage
            // information i.e., DataStoreAccountName, DataStoreAccountKey values
            datastream = base.CreateValueDataStream<StrKey, StrValue>("test", false /* remoteSync */);

            worker = new SafeThread(delegate()
            {
                Work();
            }, "AppDigitalMedia-worker", logger);
            worker.Start();
        }

        public override void Stop()
        {
            logger.Log("AppDigitalMedia clean up");
            if (worker != null)
                worker.Abort();
            
            if (datastream != null)
                datastream.Close();

            if (serviceHost != null)
                serviceHost.Close();

            if (appServer != null)
                appServer.Dispose();

            if (SignalR != null)
                SignalR.Dispose();
        }


        /// <summary>
        /// Creates and connects the hub connection and hub proxy. 
        /// </summary>
        private async void ConnectAsync()
        {   
          try
            {           

            // Initialize Signal R
            this.Connection = new HubConnection(ServerURI);
            //this.Connection.Closed += Connection_Closed;
            this.Connection.Error += Connection_Error;
            this.Connection.StateChanged += Connection_StateChanged;
            this.HubProxy = this.Connection.CreateHubProxy("DigitalMediaHub");
            this.HubProxy.On<string, string>("AddMessage", (name, message) =>
                    logger.Log((String.Format("Incoming event from DigitalMediaHub server {0}: {1}\r", name, message))
                ));

                await Connection.Start();
            }
            catch (Exception e)
            {
                logger.Log(string.Format("Unable to connect to server: Start server before connecting clients. Error: {0}", e.Message));

                //No connection: Don't enable Send button or show chat UI
                return;
            }

        }

        void Connection_StateChanged(StateChange obj)
        {
            logger.Log(string.Format("SignalR connection status changed. Old State: {0}, NewState: {1}", obj.OldState, obj.NewState));
            if (obj.OldState == ConnectionState.Connected && obj.NewState == ConnectionState.Disconnected)
                ConnectAsync();
        }

        void Connection_Error(Exception obj)
        {
            logger.Log(string.Format("SignalR connection error. Message: {0}", obj.Message));
        }

        /// <summary>
        /// Sit in a loop and spray the Pings to all active ports
        /// </summary>
        public void Work()
        {
            int counter = 0;

            try
            {
                SignalR = Microsoft.Owin.Hosting.WebApp.Start(ServerURI);

            }
            catch (TargetInvocationException)
            {
                logger.Log("A server is already running at {0}", ServerURI);

            }
            logger.Log("Server started at {0}", ServerURI);

            ConnectAsync();

            while (true)
            {
                counter++;

                lock (this)
                {                    
                    /*foreach (VPort port in accessibleDigitalMediaPorts)
                    {
                        SendEchoRequest(port, counter);                       
                    }*/
                }

                WriteToStream();
                System.Threading.Thread.Sleep(1 * 10 * 1000);
            }
        }

        public void WriteToStream()
        {
            StrKey key = new StrKey("DigitalMediaKey");
            datastream.Append(key, new StrValue("DigitalMediaVal"));
            logger.Log("Writing {0} to stream " , datastream.Get(key).ToString());
        }

        public List<string> GetReceivedMessages()
        {
            List<string> retList = new List<string>(this.receivedMessageList);
            retList.Reverse();
            return retList;
        }

           public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        {
            string message = string.Empty;
           
            lock (this)
            {
                bool isSignalR = (this.Connection != null && this.Connection.State == ConnectionState.Connected);

                switch (opName.ToLower() )
                {
                    case RoleSignalDigital.OnDigitalEvent:
                        
                        int slot = (int)retVals[0].Value();
                        int join = (int)retVals[1].Value();
                        bool value = (bool)retVals[2].Value();

                        if (isSignalR && this.HubProxy != null)
                        this.HubProxy.Invoke("OnDigitalEvent", slot, join, value);

                        if (this.azureEventHub != null)
                            this.azureEventHub.SendEvents(RoleSignalDigital.OnDigitalEvent, slot, join, value, string.Empty);


                        message = String.Format("async OnDigitalEvent from {0}. slot = {1} join={2} value = {3}", senderPort.ToString(), slot, join, value);
                       // this.receivedMessageList.Add(message);
                        break;
                    case RoleSignalDigital.OnConnectEvent:

                        if (isSignalR)
                            this.HubProxy.Invoke("OnConnectEvent", retVals);

                        message = String.Format("async OnConnectEvent from {0}.", senderPort.ToString());
                       // this.receivedMessageList.Add(message);
                        if (this.azureEventHub != null)
                            this.azureEventHub.SendEvents(RoleSignalDigital.OnConnectEvent, -1, -1, false, senderPort.ToString());

                        break;
                    case RoleSignalDigital.OnDisconnectEvent:

                        if (isSignalR)
                        this.HubProxy.Invoke("OnDisconnectEvent", retVals);

                        message = String.Format("async OnDisconnectEvent from {0}. DisconnectReasonMessage = {1}", retVals[0].Value());

                        if (this.azureEventHub != null)
                            this.azureEventHub.SendEvents(RoleSignalDigital.OnDisconnectEvent, -1, -1, false, retVals[0].Value().ToString());
                       // this.receivedMessageList.Add(message);
                        break;
                    case RoleSignalDigital.OnErrorEvent:

                        if (isSignalR)
                        this.HubProxy.Invoke("OnErrorEvent", retVals);


                        message = string.Empty;
                        if (retVals != null && retVals.Count > 0)
                            message = String.Format("async OnErrorEvent from {0}. ErrorMessage = {1}", retVals[0].Value());

                        if (this.azureEventHub != null)
                            this.azureEventHub.SendEvents(RoleSignalDigital.OnErrorEvent, -1, -1, false, retVals[0].Value().ToString());

                        break;
                    default:
                        message = String.Format("Invalid async operation return {0} from {1}", opName.ToLower(), senderPort.ToString());

                        if (this.azureEventHub != null)
                            this.azureEventHub.SendEvents(RoleSignalDigital.OnErrorEvent, -1, -1, false, message);

                        break;
                }
            }
            logger.Log("{0} {1}", this.ToString(), message);

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
                if (!accessibleDigitalMediaPorts.Contains(port) && 
                    Role.ContainsRole(port, RoleSignalDigital.RoleName) && 
                    GetCapabilityFromPlatform(port) != null)
                {
                    accessibleDigitalMediaPorts.Add(port);

                    logger.Log("{0} added port {1}", this.ToString(), port.ToString());

                    this.Subscribe(port, RoleSignalDigital.Instance, RoleSignalDigital.OpSetDigitalName);
                    this.Subscribe(port, RoleSignalDigital.Instance, RoleSignalDigital.OnConnectEvent);
                    this.Subscribe(port, RoleSignalDigital.Instance, RoleSignalDigital.OnDigitalEvent);
                    this.Subscribe(port, RoleSignalDigital.Instance, RoleSignalDigital.OnDisconnectEvent);
                    this.Subscribe(port, RoleSignalDigital.Instance, RoleSignalDigital.OnErrorEvent);
                        
                   
                }
            }
        }

        protected new bool Subscribe(VPort port, Role role, string opName)

        {       bool retVal =  false;
                try
                {
                    retVal = base.Subscribe(port, role, opName);
                }
                catch (Exception ex)
                {
                    logger.Log("{0} failed to subscribe to port {1}. Exception: {2}", this.ToString(), port.ToString(), ex.ToString());
                }
            if (retVal)
                logger.Log("{0} subscribed to port {1}", this.ToString(), port.ToString());
            else
                logger.Log("{0} failed to subscribe to port {1}.", this.ToString(), port.ToString());
       
            return retVal;
            
        }

        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (accessibleDigitalMediaPorts.Contains(port))
                {
                    accessibleDigitalMediaPorts.Remove(port);
                    logger.Log("{0} deregistered port {1}", this.ToString(), port.GetInfo().ModuleFacingName());
                }
            }
        }


        public List<string> SetDigitalSignal(int slot, int joint, string value)
        {
            List<string> retList = new List<string>(accessibleDigitalMediaPorts.Capacity);
            foreach (VPort port in accessibleDigitalMediaPorts)
            {
               retList.Add(SetDigitalSignal(port, slot, joint, value));
            }
            return retList;
        }

        public string SetDigitalSignal(VPort port, int slot, int join, string value)
        {
            try
            {
                DateTime requestTime = DateTime.Now;

                ParamType[] args = new ParamType[] { new ParamType(slot), new ParamType(join), new ParamType(value) };

                var retVals = Invoke(port, RoleSignalDigital.Instance, RoleSignalDigital.OpSetDigitalName, args);

                double diffMs = (DateTime.Now - requestTime).TotalMilliseconds;


                if (retVals[0].Maintype() != (int)ParamType.SimpleType.error)
                {

                    bool rcvdNum = (bool)retVals[0].Value();

                    logger.Log(String.Format("set digital success to {0} after {1} ms. sent = slot:{2} join:{3} value:{4} rcvd = {5}", port, diffMs, slot, join, value, rcvdNum));
                }
                else
                {
                    logger.Log(String.Format("set digital failure to {0} after {1} ms. sent = slot:{2} join:{3} value:{4} error = {5}", port, diffMs, slot, join, value, retVals[0].Value()));
                }

                return retVals[0].Value().ToString();
            }
            catch (Exception e)
            {
                string ErrorMessage = String.Format("Error while calling echo request: {0}", e.ToString());
                logger.Log(ErrorMessage);
                return ErrorMessage;
            }
        }
    }
}