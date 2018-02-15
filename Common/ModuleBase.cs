using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common.Bolt.DataStore;
using HomeOS.Shared;

namespace HomeOS.Hub.Common
{
    /// <summary>
    /// The base class that should be inherited by all modules
    /// </summary>
    public abstract class ModuleBase : VModule, SafeServicePolicyDecider
    {
        private VPlatform platform;
        private int secret;

        protected VLogger logger;
        protected VModuleInfo moduleInfo;

        private List<Port> myPorts;
        Dictionary<Port, Capability> defaultPortCapabilities;

        private StreamFactory streamFactory;

        private bool safeToGoOnline;

        private Dictionary<VPort, VCapability> capabilityStore;

        /// <summary>
        /// Returns the ModuleInfo object of the module
        /// </summary>
        /// <returns>The ModuleInfo object</returns>
        public sealed override VModuleInfo GetInfo()
        {
            return moduleInfo;
        }

        /// <summary>
        /// Called by the platform for initialize the module
        /// </summary>
        /// <param name="platform">A backwards pointer to the platform</param>
        /// <param name="logger">The default logging object</param>
        /// <param name="moduleInfo">ModuleInfo for the module</param>
        /// <param name="secret">Secret</param>
        public sealed override void Initialize(VPlatform platform, VLogger logger, VModuleInfo moduleInfo, int secret)
        {            
            this.platform = platform;
            this.logger = logger;
            this.moduleInfo = moduleInfo;
            this.secret = secret;
            this.streamFactory = StreamFactory.Instance;
            this.safeToGoOnline = platform.SafeToGoOnline();

            myPorts = new List<Port>();
            defaultPortCapabilities = new Dictionary<Port, Capability>();
            this.capabilityStore = new Dictionary<VPort, VCapability>();

            InitPort(new PortInfo("ctrlport", moduleInfo));

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(LogUnhandledException);

            logger.Log("Initialized {0}", moduleInfo.ToString());
        }

        public sealed override int Secret()
        {
            return secret;
        }

        /// <summary>
        /// Obtains the portinfo object from the platform
        /// </summary>
        /// <param name="moduleFacingName">The local name of the port</param>
        /// <returns>The portinfo object</returns>
        protected VPortInfo GetPortInfoFromPlatform(string moduleFacingName)
        {
            return platform.GetPortInfo(moduleFacingName, this);
        }

        /// <summary>
        /// Binds the port to the list of roles, by informing the Platform of the binding
        /// </summary>
        /// <param name="port">The port to bind</param>
        /// <param name="roles">The list of roles to bind</param>
        protected void BindRoles(Port port, List<VRole> roles)
        {
            BindRoles(port, roles, OnInvoke);
        }

        /// <summary>
        /// Binds the port to the list of roles and sets the delegate for all operations in each role
        /// </summary>
        /// <param name="port">The port to bind</param>
        /// <param name="roles">The list of roles to bind</param>
        /// <param name="opDelegate">The delegate for all operations</param>
        protected void BindRoles(Port port, List<VRole> roles, OperationDelegate opDelegate)
        {
            platform.SetRoles(port.GetInfo(), roles, this);

            foreach (VRole role in roles)
            {
                foreach (VOperation operation in role.GetOperations())
                {
                    port.SetOperationDelegate(role.Name(), operation.Name(), opDelegate);
                }
            }
        }

        /// <summary>
        /// Initializes the port given the portinfo
        /// </summary>
        /// <param name="portInfo">The portinfo for the port</param>
        /// <returns>The initialized port</returns>
        protected Port InitPort(VPortInfo portInfo)
        {
            Port port = new Port(portInfo, this, PortStatus.Available, logger, OnNotification);
            Capability capability = new Capability(moduleInfo.FriendlyName(), DateTime.MaxValue);

            lock (myPorts)
            {
                myPorts.Add(port);
            }

            defaultPortCapabilities[port] = capability;

            port.AddCapability(capability);

            return port;
        }

        /// <summary>
        /// Registers the port with the platform to declare it open for business from other modules
        /// </summary>
        /// <param name="port">The port to register</param>
        /// <returns>The result code of registration</returns>
        protected ResultCode RegisterPortWithPlatform(Port port)
        {
            ResultCode resultCode = (ResultCode) platform.RegisterPort(port, this);

            if (resultCode == ResultCode.Success)
            {
                logger.Log("Successfully registered {0}", port.ToString());
            }
            else
            {
                logger.Log("Failed to register {0}. result = {1}", port.ToString(), resultCode.ToString());
            }

            return resultCode;
        }

        /// <summary>
        /// Deregisters the port with the platform, thus making it closed forr business
        /// </summary>
        /// <param name="port"></param>
        /// <returns>The result code of deregisteration</returns>
        protected ResultCode DeregisterPortWithPlatform(Port port)
        {
            ResultCode resultCode = (ResultCode)platform.DeregisterPort(port, this);

            if (resultCode == ResultCode.Success)
            {
                logger.Log("Successfully deregistered {0}", port.ToString());
            }
            else
            {
                logger.Log("Failed to deregister {0}. result = {1}", port.ToString(), resultCode.ToString());
            }

            return resultCode;
        }


        /// <summary>
        /// Obtains the list of all ports that are currently registered with the platform
        /// </summary>
        /// <returns>The list of registered ports</returns>
        protected IList<VPort> GetAllPortsFromPlatform()
        {
            IList<VPort> portList = platform.GetAllPorts();

            if (portList == null)
            {
                logger.Log("{0} failed to get allportslist", this.ToString());
            }
            else {
               logger.Log("{0} successfully received allportslist with {1} ports", this.ToString(), portList.Count().ToString());
            }

            return portList;
        }


        /// <summary>
        /// Obtains the capability for sending requests to a port
        /// </summary>
        /// <param name="targetPort">The port for which the capability is desired</param>
        /// <param name="userInfo">The user on whose behalf the capability is desired</param>
        /// <returns></returns>
        protected VCapability GetCapability(VPort targetPort, UserInfo userInfo)
        {

            VCapability capability = platform.GetCapability(this, targetPort, userInfo.Name, userInfo.Password);

            if (capability == null)
            {
                logger.Log("{0} failed to get capability for {1}", this.ToString(), targetPort.ToString());
            }
            else {
               logger.Log("{0} got capability for {1}", this.ToString(), targetPort.ToString());
            }

            return capability;
        }

        /// <summary>
        /// Checks whether we have access to the port by asking for a capability
        /// If we get a valid capability, it has the side effect of storing that capability in the store
        /// </summary>
        /// <param name="targetPort"></param>
        /// <returns></returns>
        protected VCapability GetCapabilityFromPlatform(VPort targetPort)
        {

            VCapability capability = GetCapability(targetPort, Constants.UserSystem);

            if (capability == null)
                return null;

            lock (capabilityStore)
            {
                if (capabilityStore.ContainsKey(targetPort))
                    capabilityStore[targetPort] = capability;
                else
                    capabilityStore.Add(targetPort, capability);
            }

            return capability;
        }

        /// <summary>
        /// Called by (app) modules to invoke operations
        /// </summary>
        /// <param name="toPort"></param>
        /// <param name="role"></param>
        /// <param name="opName"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public IList<VParamType> Invoke(VPort toPort, Role role, string opName, params ParamType[] values)
        {
            //first check the store if we have the capability
            VCapability capability = GetCapabilityFromStore(toPort);

            //if not, then try to get it from the platform
            if (capability == null)
                capability = GetCapabilityFromPlatform(toPort);

            //if capability is still null, throw exception
            if (capability == null)
                throw new AccessViolationException("This module does not have access to this port");


            var operation = role.GetOperation(opName);

            if (operation == null)
                throw new InvalidOperationException(opName + " is not a valid operation for Role " + role.Name());

            //check the number of parameters
            if (values.Length != operation.Parameters().Count)
                throw new ArgumentException("Incorrect number of arguments for operation " + opName);

            //check the types of arguments
            for (int index = 0; index < values.Length; index++)
            {
                if ((values[index].Maintype() != operation.Parameters()[index].Maintype()))
                    throw new ArgumentException(String.Format("Argument {0} is of invalid type. Expected {1}. Got {2}.", index, (ParamType.SimpleType)operation.Parameters()[index].Maintype(), (ParamType.SimpleType)values[index].Maintype()));
            }

            //now invoke the operation
            IList<VParamType> retVals = toPort.Invoke(role.Name(), opName, values, ControlPort, capability, ControlPortCapability);

            //if we got an error, pass it along
            if (retVals.Count >= 1 && retVals[0].Maintype() == (int)ParamType.SimpleType.error)
            {
                return retVals;
            }

            //if we didn't get an error, sanity check the return values

            //check the number of elements
            if (retVals.Count != operation.ReturnValues().Count)
                throw new ArgumentException("Incorrect number of return values for operation " + opName);

            //check the types of elements in return values
            for (int index = 0; index < retVals.Count; index++)
            {
                if ((retVals[index].Maintype() != operation.ReturnValues()[index].Maintype()))
                    throw new ArgumentException(String.Format("Return value {0} is of invalid type. Expected {1}. Got {2}.", index, (ParamType.SimpleType)operation.ReturnValues()[index].Maintype(), (ParamType.SimpleType)retVals[index].Maintype()));
            }

            //otherwise, lets return these values
            return retVals;

        }

        /// <summary>
        /// called by (driver) modules to send notifications to modules that have subscribed to operationName on fromPort
        /// </summary>
        /// <param name="fromPort"></param>
        /// <param name="role"></param>
        /// <param name="opName"></param>
        /// <param name="values"></param>
        protected void Notify(Port fromPort, Role role, string opName, params ParamType[] values)
        {
            var operation = role.GetOperation(opName);

            if (operation == null)
                throw new InvalidOperationException(opName + " is not a valid operation for Role " + role.Name());

            if (!operation.Subscribeable())
                throw new InvalidOperationException(opName + " is not a subscribable operation for Role " + role.Name());

            //check the number of elements
            if (values.Length != operation.ReturnValues().Count)
                throw new ArgumentException("Incorrect number of return values for operation " + opName);

            //check the types of elements in return values
            for (int index = 0; index < values.Length; index++)
            {
                if ((values[index].Maintype() != operation.ReturnValues()[index].Maintype()))
                    throw new ArgumentException(String.Format("Return value {0} is of invalid type. Expected {1}. Got {2}.", index, (ParamType.SimpleType)operation.ReturnValues()[index].Maintype(), (ParamType.SimpleType) values[index].Maintype()));
            }

            fromPort.Notify(role.Name(), opName, values);
        }
        
        /// <summary>
        /// called by (app) modules to subscribe to another port's role and operation pair
        /// </summary>
        /// <param name="port"></param>
        /// <param name="role"></param>
        /// <param name="opName"></param>
        /// <returns></returns>
        protected bool Subscribe(VPort port, Role role, string opName)
        {
            //first check the store if we have the capability
            VCapability capability = GetCapabilityFromStore(port);

            //if not, then try to get it from the platform
            if (capability == null)
                capability = GetCapabilityFromPlatform(port);

            //if capability is still null, throw exception
            if (capability == null)
                throw new AccessViolationException("This module does not have access to this port");

            var operation = role.GetOperation(opName);

            if (operation == null)
                throw new InvalidOperationException(opName + " is not a valid operation for Role " + role.Name());

            if (!operation.Subscribeable())
                throw new InvalidOperationException(opName + " is not a subscribable operation for Role " + role.Name());


            return port.Subscribe(role.Name(), opName, ControlPort, capability, ControlPortCapability);
        }

        VCapability GetCapabilityFromStore(VPort targetPort)
        {
            lock (capabilityStore)
            {
                if (capabilityStore.ContainsKey(targetPort))
                    return capabilityStore[targetPort];
                else
                    return null;
            }

        }

        public int IsValidAccess(string moduleFriendlyName, string domainOfAccess, string privilegeLevel, string userIdentifier)
        {
            if (!moduleFriendlyName.Equals(moduleInfo.FriendlyName(), StringComparison.CurrentCultureIgnoreCase))
                logger.Log("Error: {0} is being asked about {1}", moduleInfo.FriendlyName(), moduleFriendlyName);

            return platform.IsValidAccess(moduleFriendlyName, domainOfAccess, privilegeLevel, userIdentifier); 
        }

        public string GetConfSetting(string paramName)
        {
            return platform.GetConfSetting(paramName);
        }

        public string GetPrivateConfSetting(string paramName)
        {
            return platform.GetPrivateConfSetting(paramName);
        }

        public string GetDeviceIpAddress(string deviceId)
        {
            return platform.GetDeviceIpAddress(deviceId);
        }

        /// <summary>
        /// Send email using local SMTP Client, if that fails, send using cloud relay
        /// </summary>
        /// <param name="dst">to:</param>
        /// <param name="subject">subject</param>
        /// <param name="body">body of the message</param>
        /// <returns>A tuple with true/false success and string exception message (if any)</returns>
        public Tuple<bool, string> SendEmail(string dst, string subject, string body, List<Attachment> attachmentList)
        {
            return Utils.SendEmail(dst, subject, body, attachmentList, platform, logger);
        }

        /// <summary>
        /// Send email using local SMTP Client
        /// </summary>
        /// <param name="dst">to:</param>
        /// <param name="subject">subject</param>
        /// <param name="body">body of the message</param>
        /// <returns>A tuple with true/false success and string exception message (if any)</returns>
        public Tuple<bool, string> SendHubEmail(string dst, string subject, string body, List<Attachment> attachmentList)
        {
            return Utils.SendHubEmail(dst, subject, body, attachmentList, platform, logger);
        }

        /// <summary>
        /// Send email by using Cloud Relay Service Host
        /// </summary>
        /// <param name="dst">to:</param>
        /// <param name="subject">subject</param>
        /// <param name="body">body of the message</param>
        /// <returns>A tuple with true/false success and string exception message (if any)</returns>
        public Tuple<bool, string> SendCloudEmail(string dst, string subject, string body, List<Attachment> attachmentList)
        {
            return Utils.SendCloudEmail(dst, subject, body, attachmentList, platform, logger);
        }

        /// <summary>
        /// Signals that the module is finished
        /// </summary>
        protected void Finished()
        {
            lock (myPorts)
            {
                foreach (Port port in myPorts)
                {
                    if (!port.Equals(ControlPort)) //since we do not register controlports
                        DeregisterPortWithPlatform(port);
                }
            }

            ResultCode finishResult = (ResultCode) platform.ModuleFinished(this);

            logger.Log(this + "Result for modulefinished: {0}", finishResult.ToString());
        }

        /// <summary>
        /// Called from the platform to install a new capability for a port that is owned by this module
        /// </summary>
        /// <param name="capability">The capability to install</param>
        /// <param name="targetPort">The port on which to install the capability</param>
        /// <returns></returns>
        public sealed override int InstallCapability(VCapability capability, VPort targetPort)
        {

            ResultCode resultCode;

            if (!IsMyPort(targetPort))
            {
                logger.Log("{0} got InstallCapability request for somemeone else's port {1}", targetPort.ToString());

                resultCode = ResultCode.PortNotFound;
            }
            else
            {
                Port port = (Port)targetPort;
                port.AddCapability(capability);

                resultCode = ResultCode.Success;
            }

            return (int)resultCode;
        }

        /// <summary>
        /// Checks if the port belongs to this module
        /// </summary>
        /// <param name="port">The port whose ownership is being checked</param>
        /// <returns></returns>
        protected bool IsMyPort(VPort port)
        {
            lock (myPorts)
            {
                foreach (VPort myport in myPorts)
                    if (port.Equals(myport))
                        return true;
            }

            return false;
        }

        /// <summary>
        /// ControlPort of the module is the special port from which operation invokations are sent to other ports. 
        /// Operation responses, including notifications, arrive at this port as well.
        /// </summary>
        protected Common.Port ControlPort
        {
            get { return myPorts[0]; }
        }

        /// <summary>
        /// The capability of the ControlPort
        /// </summary>
        protected Capability ControlPortCapability
        {
            get { return defaultPortCapabilities[ControlPort]; }
        }

        /* syncIntervalSec:
         *   -ve ==> don't sync on writes;  only sync on close.
         *   0   ==> sync on every write
         *   +ve ==> sync every x seconds
        */
        protected IStream CreateValueDataStream<KeyType, ValType>(string streamId, bool remoteSync, int syncIntervalSec = -1)
            where KeyType : IKey, new()
            where ValType : IValue, new()
        {
            CallerInfo ci = new CallerInfo(this.moduleInfo.WorkingDir(), this.moduleInfo.FriendlyName(), this.moduleInfo.AppName(), this.Secret());
            FqStreamID fq_sid = new FqStreamID(GetConfSetting("HomeId"), this.moduleInfo.FriendlyName(), streamId);
            if (remoteSync)
            {
                LocationInfo li = new LocationInfo(GetConfSetting("DataStoreAccountName"), GetConfSetting("DataStoreAccountKey"), SynchronizerType.Azure);
                return this.streamFactory.openValueDataStream<KeyType, ValType>
                    (fq_sid, ci, li, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Write, syncIntervalSec: syncIntervalSec);
            }
            else
            {
                return this.streamFactory.openValueDataStream<KeyType, ValType>
                    (fq_sid, ci, null, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Write, syncIntervalSec: syncIntervalSec);
            }
        }

        protected IStream CreateFileDataStream<KeyType, ValType>(string streamId, bool remoteSync, int syncIntervalSec = -1)
            where KeyType : IKey, new()
        {
            CallerInfo ci = new CallerInfo(this.moduleInfo.WorkingDir(), this.moduleInfo.FriendlyName(), this.moduleInfo.AppName(), this.Secret());
            FqStreamID fq_sid = new FqStreamID(GetConfSetting("HomeId"), this.moduleInfo.FriendlyName(), streamId);
            if (remoteSync)
            {
                LocationInfo Li = new LocationInfo(GetConfSetting("DataStoreAccountName"), GetConfSetting("DataStoreAccountKey"), SynchronizerType.Azure);
                return this.streamFactory.openFileDataStream<KeyType>(
                    fq_sid, ci, Li, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Write, syncIntervalSec: syncIntervalSec);
            }
            else
            {
                return this.streamFactory.openFileDataStream<KeyType>(
                    fq_sid, ci, null, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Write, syncIntervalSec: syncIntervalSec);
            }
        }

        public override string ToString()
        {
            return moduleInfo.FriendlyName();
        }

        /// <summary>
        /// The handler for operation notifications.
        /// The module class should override this function if operation subscriptions are being used.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="opName"></param>
        /// <param name="retVals"></param>
        /// <param name="senderPort"></param>
        public virtual void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        {
            throw new NotImplementedException("The module is using subscription without implementing an OnNotification that overrides this function in the parent class");
        }

        /// <summary>
        /// The default handler for operation notifications.
        /// The module class should override this function if operation invocations are being used
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="opName"></param>
        /// <param name="retVals"></param>
        /// <param name="senderPort"></param>
        public virtual IList<VParamType> OnInvoke(string roleName, string opName, IList<VParamType> retVals)
        {
            throw new NotImplementedException("The module is using invocation without overriding OnInvoke.");
        }

        // These methods should be implemented by inheriting modules
        public abstract override void Start();
        public abstract override void Stop();
        public abstract override void PortRegistered(VPort port);
        public abstract override void PortDeregistered(VPort port);

        //*** These methods hook around the 4 abstract method inherting modules implement (start,stop, portregistered, portderegistered) 
        public sealed override void StartWithHooks()
        {
            platform.UpdateState(this, new HomeOS.Hub.Common.ModuleState(ModuleState.SimpleState.EnterStart, DateTime.Now));
            Start();
            platform.UpdateState(this, new HomeOS.Hub.Common.ModuleState(ModuleState.SimpleState.ExitStart, DateTime.Now));
          
        }

        public sealed override void StopWithHooks()
        {
            platform.UpdateState(this, new HomeOS.Hub.Common.ModuleState(ModuleState.SimpleState.EnterStop, DateTime.Now));
            Stop();
            platform.UpdateState(this, new HomeOS.Hub.Common.ModuleState(ModuleState.SimpleState.ExitStop, DateTime.Now));
            //***
            platform.CancelAllSubscriptions(this , ControlPort, ControlPortCapability);
            //***
        }

        public sealed override void PortRegisteredWithHooks(HomeOS.Hub.Platform.Views.VPort port)
        {
            platform.UpdateState(this, new HomeOS.Hub.Common.ModuleState(ModuleState.SimpleState.EnterPortRegistered, DateTime.Now));
            PortRegistered(port);
            platform.UpdateState(this, new HomeOS.Hub.Common.ModuleState(ModuleState.SimpleState.ExitPortRegistered, DateTime.Now));

        }

        public sealed override void PortDeregisteredWithHooks(HomeOS.Hub.Platform.Views.VPort port)
        {
            lock (capabilityStore)
            {
                if (capabilityStore.ContainsKey(port))
                    capabilityStore.Remove(port);
            }

            platform.UpdateState(this, new HomeOS.Hub.Common.ModuleState(ModuleState.SimpleState.EnterPortDeregistered, DateTime.Now));
            PortDeregistered(port);
            platform.UpdateState(this, new HomeOS.Hub.Common.ModuleState(ModuleState.SimpleState.ExitPortDeregistered, DateTime.Now));
        }
        
        public override object OpaqueCall(string callName, params object[] args)
        {
            throw new NotImplementedException("Error: OpaqueCall is not implemented by " + this.ToString());
        }

        public override IList<long> GetResourceUsage()
        {
            long cpuTime = (long) AppDomain.CurrentDomain.MonitoringTotalProcessorTime.TotalMilliseconds;
            long totalAllocationMemory = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
            long totalSurvivedMemory = AppDomain.CurrentDomain.MonitoringSurvivedMemorySize;
            long totalSurvivedMemoryProcess = AppDomain.MonitoringSurvivedProcessMemorySize; //this number should be the same for everyone in the process

            return new List<long> () {cpuTime, totalAllocationMemory, totalSurvivedMemory, totalSurvivedMemoryProcess};
        }

        /// <summary>
        /// By default, we return a null string. 
        /// To return something meaningful, override this function in the module's implementation.
        /// </summary>
        /// <param name="hint"></param>
        /// <returns></returns>
        public override string GetImageUrl(string hint)
        {
            return moduleInfo.BaseURL() + "/icon.png";
        }

        /// <summary>
        /// By default, we return unknown 
        /// To return something meaningful, override this function in the module's implementation.
        /// </summary>
        /// <param name="hint"></param>
        /// <returns></returns>
        public override string GetDescription(string hint)
        {
            return "Unknown (GetDescription function should be overriden)";
        }

        /// <summary>
        /// A handler for all uncaught exceptions. We log, but we'll still die
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            logger.Log("Got unhandled exception from {0}: {1}\nException: {2}", sender.ToString(), args.ToString(), e.ToString());
        }

        public override void OnlineStatusChanged(bool newStatus)
        {
            safeToGoOnline = newStatus;

            //todo: if we need to inform others that online status changed
        }
    }
}
