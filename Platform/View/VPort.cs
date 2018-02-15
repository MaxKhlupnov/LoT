using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Views
{
    public abstract class VPort: MarshalByRefObject
    {
        /// <summary>
        /// Returns the PortInfo object for the port
        /// </summary>
        /// <returns>The portinfo object</returns>
        public abstract VPortInfo GetInfo();

        /// <summary>
        /// Subscribe to an notifications from this port
        /// </summary>
        /// <param name="roleName">The name of the role that contains the operation</param>
        /// <param name="opName">The name of the operation being subscribed to</param>
        /// <param name="fromPort">The port from which subscription is being issued (usually the ControlPort of the calling module) </param>
        /// <param name="reqCap">The capability for the port to which subscription is being issued</param>
        /// <param name="respCap">The capability that the notifications should use</param>
        /// <returns>Whether the subscription succeeded</returns>
        public abstract bool Subscribe(string roleName, string opName, VPort fromPort, VCapability reqCap, VCapability respCap);

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
        public abstract bool Unsubscribe(string roleName, string opName, VPort fromPort, VCapability respCap);

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
        public abstract IList<VParamType> Invoke(string roleName, string opName, IList<VParamType> parameters, 
                                                      VPort fromPort, VCapability reqCap, VCapability respCap);

        ///// <summary>
        ///// Invoke an operation exported by the port
        ///// </summary>
        ///// <param name="roleName">The name of the role that contains the operation</param>
        ///// <param name="opName">The name of the operation being subscribed to</param>
        ///// <param name="parameters">The list of parameters to call the operation with</param>
        ///// <param name="fromPort">The port from which subscription is being issued (usually the ControlPort of the calling module) </param>
        ///// <param name="reqCap">The capability for the port to which subscription is being issued</param>
        ///// <param name="respCap">The capability that the notifications should use</param>
        ///// <returns>The list of return values</returns>
        //public abstract IList<object> Invoke(string roleName, string opName, 
        //                                              VPort fromPort, VCapability reqCap, VCapability respCap, params object[] parameters);

        /// <summary>
        /// The function that is called when a notification is issued in response to subscriptions
        /// </summary>
        /// <param name="roleName">The name of the role for which the notification is issued</param>
        /// <param name="opName">The name of the operation for which the notification is issued</param>
        /// <param name="retVals">The list of return values that are part of the notification</param>
        /// <param name="srcPort">The port from which the notification is being sent</param>
        /// <param name="respCap">The capability that the notification was sent with</param>
        public abstract void AsyncReturn(string roleName, string opName, IList<VParamType> retVals, VPort srcPort, VCapability respCap);

        /// <summary>
        /// Determines whether the port is equal to another object.
        /// Ports are equal when their portinfo objects are equal.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return ( obj != null &&
                     obj is VPort &&
                     this.GetInfo().Equals(((VPort)obj).GetInfo())
                    );
        }

        public override int GetHashCode()
        {
            return GetInfo().GetHashCode();
        }

        public override string ToString()
        {
            return GetInfo().ModuleFacingName() + "-" + GetInfo().ModuleFriendlyName();
        }

    }
}
