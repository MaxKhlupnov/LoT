using System;
using System.Threading;
using System.Collections.Generic;

namespace HomeOS.Hub.Common
{
    
    public enum PortStatus { Unavailable, Available, Busy };

    public delegate void OperationReturnHandler(string roleName, string opName, IList<HomeOS.Hub.Platform.Views.VParamType> retVals, HomeOS.Hub.Platform.Views.VPort from);
    public delegate IList<HomeOS.Hub.Platform.Views.VParamType> OperationDelegate(string roleName, string opName, IList<HomeOS.Hub.Platform.Views.VParamType> args);

    public sealed class Port : HomeOS.Hub.Platform.Views.VPort
    {
        /// <summary>
        /// The class that is the key to the dictionaries that contain information about operation handlers and subscribers
        /// </summary>
        private class OperationKey : IEquatable<OperationKey>
        {
            public string OperationName { get; private set; }

            public OperationKey(string opName)
            {
                OperationName = opName;
            }

            public override int GetHashCode()
            {
                return OperationName.ToLower().GetHashCode();
            }

            public bool Equals(OperationKey otherPair)
            {
                return OperationName.Equals(otherPair.OperationName, StringComparison.CurrentCultureIgnoreCase);
            }

            public override string ToString()
            {
                return OperationName;
            }
        }

        /// <summary>
        /// The class that represents the subscription information (e.g., which ports are subscribed and their capabilities)
        /// </summary>
        private class SubscriptionInfo : IEquatable<SubscriptionInfo>
        {
            public HomeOS.Hub.Platform.Views.VPort subscribedPort;
            public HomeOS.Hub.Platform.Views.VCapability subscriptionCapbility;
            public HomeOS.Hub.Platform.Views.VCapability notifyCapability;

            public SubscriptionInfo(HomeOS.Hub.Platform.Views.VPort p, HomeOS.Hub.Platform.Views.VCapability subCap, HomeOS.Hub.Platform.Views.VCapability notCap)
            {
                this.subscriptionCapbility = subCap;
                this.subscribedPort = p;
                this.notifyCapability = notCap;
            }

            /// <summary>
            /// compare only the subscribed port
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public bool Equals(SubscriptionInfo o)
            {
                return subscribedPort.Equals(o.subscribedPort);
            }
        }

        /// <summary>
        /// The status of the port (mostly unused at the moment)
        /// </summary>
        public PortStatus Status { get; private set; } 

        /// <summary>
        /// The logging object to which log messages are sent
        /// </summary>
        HomeOS.Hub.Platform.Views.VLogger logger;

        /// <summary>
        /// list of capabilities that are currently valid for this port
        /// </summary>
        Dictionary<HomeOS.Hub.Platform.Views.VCapability, bool> currentCapabilities;

        /// <summary>
        /// The portinfo object for the port
        /// </summary>
        HomeOS.Hub.Platform.Views.VPortInfo portInfo;

        /// <summary>
        /// This dictionary contains handlers to call when an operation is invoked
        /// </summary>
        Dictionary<OperationKey, OperationDelegate> operationDelegates;

        /// <summary>
        /// This dictionary contains active subscriptions for operations
        /// </summary>
        Dictionary<OperationKey, List<SubscriptionInfo>> subscribedPorts;

        /// <summary>
        /// The function to call when notifications arrive at this port from other ports
        /// </summary>
        OperationReturnHandler handler;

        /// <summary>
        /// Constructor for the Port object
        /// </summary>
        /// <param name="info">The PortInfo object that this port corresponds to</param>
        /// <param name="owner">The owning module that is initializing the port</param>
        /// <param name="status">The status of the port (mostly unused right now)</param>
        /// <param name="logger">The default logging object for log messages from this port</param>
        /// <param name="handler">The function to call when notifications are received by the port</param>
        public Port(HomeOS.Hub.Platform.Views.VPortInfo info, HomeOS.Hub.Platform.Views.VModule owner, PortStatus status, HomeOS.Hub.Platform.Views.VLogger logger, OperationReturnHandler handler)
        {
            this.portInfo = info;
            this.Status = status;
            this.logger = logger;
            this.handler = handler;

            this.subscribedPorts = new Dictionary<OperationKey, List<SubscriptionInfo>>();
            this.operationDelegates = new Dictionary<OperationKey,OperationDelegate>();

            foreach (HomeOS.Hub.Platform.Views.VRole role in info.GetRoles())
            {
                foreach (HomeOS.Hub.Platform.Views.VOperation operation in role.GetOperations())
                {
                    this.operationDelegates[new OperationKey(operation.Name())] = null;
                }
            }

            currentCapabilities = new Dictionary<HomeOS.Hub.Platform.Views.VCapability, bool>();
        }

        /// <summary>
        /// Sets the handler that should be called when this operation is invoked
        /// </summary>
        /// <param name="roleName">The name of the role that contains the operation</param>
        /// <param name="opName">The name of the operation whose handler is being set</param>
        /// <param name="handler">The handler to call</param>
        public void SetOperationDelegate(string roleName, string opName, OperationDelegate handler)
        {

            OperationKey roleOpPair = new OperationKey(opName);

            bool operationFound = false;

            //check if this role/op pair exists for the port
            foreach (HomeOS.Hub.Platform.Views.VRole role in portInfo.GetRoles())
            {
                if (roleName.Equals(role.Name(), StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (HomeOS.Hub.Platform.Views.VOperation operation in role.GetOperations())
                    {
                        if (opName.Equals(operation.Name(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            operationFound = true;
                            break;
                        }
                    }
                }
            }

            if (!operationFound)
            {
                throw new InvalidOperationException("Assigning delegate "+handler+" to non-existent role/operation " + roleOpPair.ToString());
            }

            this.operationDelegates[roleOpPair] = handler;
        }

        /// <summary>
        /// Returns the PortInfo object for this port
        /// </summary>
        /// <returns></returns>
        public override HomeOS.Hub.Platform.Views.VPortInfo GetInfo() 
        {
            return portInfo;
        }

        /// <summary>
        /// Subscribe to an notifications from this port
        /// </summary>
        /// <param name="roleName">The name of the role that contains the operation</param>
        /// <param name="opName">The name of the operation being subscribed to</param>
        /// <param name="fromPort">The port from which subscription is being issued (usually the ControlPort of the calling module) </param>
        /// <param name="reqCap">The capability for the port to which subscription is being issued</param>
        /// <param name="respCap">The capability that the notifications should use</param>
        /// <returns>Whether the subscription succeeded</returns>
        public override bool Subscribe(string roleName, string opName, HomeOS.Hub.Platform.Views.VPort fromPort, HomeOS.Hub.Platform.Views.VCapability reqCap, HomeOS.Hub.Platform.Views.VCapability respCap)
        {
            SubscriptionInfo tmp = new SubscriptionInfo(fromPort, reqCap, respCap);

            lock (this.subscribedPorts)
            {
                OperationKey roleOpPair = new OperationKey(opName);

                //if we don't have anyone subscribed create the list
                if (!this.subscribedPorts.ContainsKey(roleOpPair))
                {
                    this.subscribedPorts[roleOpPair] = new List<SubscriptionInfo>();
                }

                if (this.ValidateCapability(reqCap))
                {
                    //if a subscription already exists remove it first
                    if (this.subscribedPorts[roleOpPair].Contains(tmp))
                        this.subscribedPorts[roleOpPair].Remove(tmp);

                    this.subscribedPorts[roleOpPair].Add(tmp);
                    return true;
                }
                else
                {
                    logger.Log("WARNING: Capability check failed in Subscribe");
                    return false;
                }
            }
        }

        /// <summary>
        /// Unsubscribe from an operation exported by this port. Somebody else cannot unsubscribe 
        /// you unless they have the same response capability that was used to create the subscription.
        /// </summary>
        /// <param name="roleName">The name of the role that contains the operation</param>
        /// <param name="opName">The name of the operation being subscribed to</param>
        /// <param name="fromPort">The port to unsubscribe</param>
        /// <param name="respCap">The response capability that was used in the subscription</param>
        /// <returns>true if the port was subscribed and is now unsubscribed,
        /// false if the port was not subscribed</returns>
        public override bool Unsubscribe(string roleName, string opName, HomeOS.Hub.Platform.Views.VPort fromPort, HomeOS.Hub.Platform.Views.VCapability respCap)
        {
            lock (this.subscribedPorts)
            {
                OperationKey roleOpPair = new OperationKey(opName);

                if (!this.subscribedPorts.ContainsKey(roleOpPair)) 
                {
                    return true;
                }

                foreach (SubscriptionInfo sub in subscribedPorts[roleOpPair])
                    {
                        if (sub.subscribedPort.Equals(fromPort) && sub.notifyCapability.Equals(respCap))
                        {
                            this.subscribedPorts[roleOpPair].Remove(sub);
                            if (subscribedPorts[roleOpPair].Count == 0)
                            {
                                subscribedPorts.Remove(roleOpPair);
                            }
                            logger.Log("Role Op pair: (" + roleName + "," + opName + ") removed from subscribed ports");
                            return true;
                        }
                    }
                return false;
            }
        }

        /// <summary>
        /// Invoke an operation exported by the port
        /// </summary>
        /// <param name="roleName">The name of the role that contains the operation</param>
        /// <param name="opName">The name of the operation being subscribed to</param>
        /// <param name="parameters">The list of parameters to call the operation with</param>
        /// <param name="fromPort">The port from which subscription is being issued (usually the ControlPort of the calling module) </param>
        /// <param name="reqCap">The capability for the port to which subscription is being issued</param>
        /// <param name="respCap">The capability that the notifications should use</param>
        /// <returns>The list of return values</returns>
        public override IList<HomeOS.Hub.Platform.Views.VParamType> Invoke(string roleName, string opName, IList<HomeOS.Hub.Platform.Views.VParamType> parameters, 
                                                     HomeOS.Hub.Platform.Views.VPort p, HomeOS.Hub.Platform.Views.VCapability reqCap, HomeOS.Hub.Platform.Views.VCapability respCap)
        {
            TimeSpan timeout = Constants.nominalTimeout;
            IList<HomeOS.Hub.Platform.Views.VParamType> retval = null;
            OperationKey roleOpPair = new OperationKey(opName);

            if (!operationDelegates.ContainsKey(roleOpPair) || operationDelegates[roleOpPair] == null)
            {
                retval = new List<HomeOS.Hub.Platform.Views.VParamType>();
                retval.Add(new ParamType(ParamType.SimpleType.error, Constants.OpDoesNotExistName));
            }
            else
            {

                SafeThread call = new SafeThread(delegate() { retval = operationDelegates[roleOpPair](roleName, opName, parameters); },
                                                 this + "." + opName,
                                                 logger);
                //call.Name = this + "." + opName;
                call.Start();

                call.Join(timeout);

                if (retval == null)
                {
                    retval = new List<HomeOS.Hub.Platform.Views.VParamType>();
                    retval.Add(new ParamType(ParamType.SimpleType.error,  Constants.OpNullResponse));
                }
            }

            return retval;

        }

        /// <summary>
        /// The function that is called when a notification is issued in response to subscriptions
        /// </summary>
        /// <param name="roleName">The name of the role for which the notification is issued</param>
        /// <param name="opName">The name of the operation for which the notification is issued</param>
        /// <param name="retVals">The list of return values that are part of the notification</param>
        /// <param name="srcPort">The port from which the notification is being sent</param>
        /// <param name="respCap">The capability that the notification was sent with</param>
        public override void AsyncReturn(string roleName, string opName, IList<HomeOS.Hub.Platform.Views.VParamType> retVals, HomeOS.Hub.Platform.Views.VPort srcPort, HomeOS.Hub.Platform.Views.VCapability respCap)
        {
            if (ValidateCapability(respCap))
            {
                handler(roleName, opName, retVals, srcPort);
            }
            else
            {
                logger.Log("WARNING: Capability check failed in AsynReturn");
            }
        }

        /// <summary>
        /// Sends a message with the specified payoad from the specified control
        /// to all ports subscribed to this port.
        /// </summary>
        /// <param name="srcControl">The control which originated the notification,
        /// can be optionally null</param>
        /// <param name="payload">The message payload</param>
        public void Notify(string roleName, string opName, IList<HomeOS.Hub.Platform.Views.VParamType> retVals)
        {
            lock (this.subscribedPorts)
            {
                OperationKey roleOpPair = new OperationKey(opName);

                if (subscribedPorts.ContainsKey(roleOpPair))
                {
                    foreach (SubscriptionInfo sub in subscribedPorts[roleOpPair])
                    {
                        SubscriptionInfo tmpSub = sub;

                        if (this.ValidateCapability(tmpSub.subscriptionCapbility))
                        {
                            SafeThread call = new SafeThread(delegate()
                            {
                                tmpSub.subscribedPort.AsyncReturn(roleName, opName, retVals,
                                 this, tmpSub.notifyCapability);
                            },
                                                             this + "." + opName + "." + tmpSub.ToString(),
                                                             logger);
                            //call.Name = this + "." + opName + "." + sub.ToString();
                            call.Start();
                        }
                        else
                        {
                            logger.Log("WARNING: capability check failed in notify");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Verify if a capability is contained in the current list of active capabilities
        /// </summary>
        /// <param name="cap">The capability to check</param>
        /// <returns>The verification result</returns>
        private bool ValidateCapability(HomeOS.Hub.Platform.Views.VCapability cap)
        {
            bool ret = false;
            lock (currentCapabilities)
            {
                if (cap != null && currentCapabilities.ContainsKey(cap))
                {
                    if (!Capability.Expired(cap))
                    {
                        ret = true;
                    }
                    else
                    {
                        currentCapabilities.Remove(cap);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Add another capability to the list of active capabilities
        /// </summary>
        /// <param name="capability"></param>
        public void AddCapability(HomeOS.Hub.Platform.Views.VCapability capability)
        {
            lock (currentCapabilities)
            {
                logger.Log("{0} adding capability: {1}", this.ToString(), capability.ToString());

                currentCapabilities[capability] = true;
            }
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool verbose)
        {
            if (verbose)
            {
                return string.Format("{0} status:{1}", GetInfo().ModuleFacingName(), Status);
            }
            else
            {
                return GetInfo().ModuleFacingName();
            }
        }
    }
}