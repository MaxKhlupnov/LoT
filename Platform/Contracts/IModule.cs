using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Contract;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Contracts
{
    [AddInContract]
    public interface IModule : IContract
    {
        IModuleInfo GetInfo();
        void Initialize(IPlatform platform, ILogger logger, IModuleInfo moduleInfo, int secret);
        void Start();
        void Stop();

        int InstallCapability(ICapability capability, IPort port);

        void PortRegistered(IPort port);
        void PortDeregistered(IPort port);

        object OpaqueCall(string callName, params object[] args);

        IListContract<long> GetResourceUsage(); 

        int Secret();

        string GetImageUrl(string hint);
        string GetDescription(string hint);

        void OnlineStatusChanged(bool newStatus);
    }
}
