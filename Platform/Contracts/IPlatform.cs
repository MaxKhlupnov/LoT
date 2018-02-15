using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Contract;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Contracts
{
    public interface IPlatform : IContract
    {
        int RegisterPort(IPort port, IModule module);
        int DeregisterPort(IPort port, IModule module);
        
        IPortInfo GetPortInfo(string moduleFacingName, IModule module);
        void SetRoles(IPortInfo portInfo, IListContract<IRole> roles, IModule module);

        IListContract<IPort> GetAllPorts();
        ICapability GetCapability(IModule module, IPort targetPort, string username, string password);
        int ModuleFinished(IModule module);
        //IRole GetRole(string roleName);

        bool IsValidUser(string username, string password);
        
        string GetConfSetting(string paramName);
        string GetPrivateConfSetting(string paramName);
        string GetDeviceIpAddress(string deviceId);
        void UpdateState(IModule module, IModuleState state);
        void CancelAllSubscriptions(IModule module , IPort controlPort, ICapability controlportcap);
        
        int IsValidAccess(string accessedModule, string domainOfAccess, string privilegeLevel, string userIdentifier);
        bool SafeToGoOnline();
    }
}
