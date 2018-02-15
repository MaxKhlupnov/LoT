using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;
using System.Diagnostics;
using System.Reflection;

namespace HomeOS.Hub.Platform.Views
{
    [AddInBase()]
    public abstract class VModule : MarshalByRefObject
    {
 
        public abstract VModuleInfo GetInfo();
        public abstract void Initialize(VPlatform platform, VLogger logger, VModuleInfo info, int secret);
        public abstract void Start();
        public abstract void Stop();
        public abstract void PortRegistered(VPort port);
        public abstract void PortDeregistered(VPort port);
        public abstract int InstallCapability(VCapability capability, VPort targetPort);
        public abstract object OpaqueCall(string callName, params object[] args);
        public abstract int Secret();
        public abstract IList<long> GetResourceUsage();
        public abstract string GetImageUrl(string hint);
        public abstract string GetDescription(string hint);
        public abstract void OnlineStatusChanged(bool newStatus);
        public override bool Equals(object obj)
        {

            if (obj == null)
                return false;
            else if (!(obj is VModule))
                return base.Equals(obj);

            //*** Adding this hack for making the AddIn shutdown() work. 
            // Peek at the stack trace to see if the caller is looking for the AddIn controller. -rayman 
            StackTrace stackTrace = new StackTrace();
            MethodBase methodBase = stackTrace.GetFrame(1).GetMethod();
            if (methodBase.Name.Equals("FindController"))
                return base.Equals(obj);
            //*** 

            else
                   return this.Equals((VModule)obj);
        }

        public bool Equals(VModule otherModule)
        {
            int secret = this.Secret();
            int otherModuleSecret = otherModule.Secret();
            if (secret == otherModuleSecret)
                return true;
            else
                return false; 
            //return (Secret().Equals(otherModule.Secret()));
        }

        public override int GetHashCode()
        {
            return Secret().GetHashCode();
        }

        public override string ToString()
        {
            return GetInfo().FriendlyName();
        }
       

        //*** Adding virtual methods for the four abstract methods modules HAVE to implement
        public virtual void StartWithHooks() { }
        public virtual void StopWithHooks() { }
        public virtual void PortRegisteredWithHooks(VPort port) { }
        public virtual void PortDeregisteredWithHooks(VPort port) { }
        //***        
    }
}
