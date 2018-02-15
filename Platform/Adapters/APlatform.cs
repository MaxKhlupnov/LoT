using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;
using System.AddIn.Contract;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.Contracts;

namespace HomeOS.Hub.Platform.Adapters
{
    public class PlatformV2C : ContractBase, IPlatform
    {
        #region standard stuff DO NOT TOUCH
        private VPlatform _view;

        public PlatformV2C(VPlatform view)
        {
            _view = view;
        }

        internal VPlatform GetSourceView()
        {
            return _view;
        }
        #endregion

        //***
        public void UpdateState(HomeOS.Hub.Platform.Contracts.IModule module, HomeOS.Hub.Platform.Contracts.IModuleState state)
        {
             _view.UpdateState(ModuleAdapter.C2V(module), ModuleStateAdapter.C2V(state));
        }

        public void CancelAllSubscriptions(HomeOS.Hub.Platform.Contracts.IModule module , HomeOS.Hub.Platform.Contracts.IPort controlPort, HomeOS.Hub.Platform.Contracts.ICapability controlportcap)
        {
            _view.CancelAllSubscriptions( ModuleAdapter.C2V(module), PortAdapter.C2V(controlPort), CapabilityAdapter.C2V(controlportcap));
        }

        public int IsValidAccess(string accessedModule, string domainOfAccess, string privilegeLevel, string userIdentifier)
        {
            return _view.IsValidAccess(accessedModule, domainOfAccess, privilegeLevel, userIdentifier);
        }
        
        //***

        public int RegisterPort(HomeOS.Hub.Platform.Contracts.IPort port, HomeOS.Hub.Platform.Contracts.IModule module)
        {
            return _view.RegisterPort(PortAdapter.C2V(port), ModuleAdapter.C2V(module));
        }

       

        public int DeregisterPort(HomeOS.Hub.Platform.Contracts.IPort port, HomeOS.Hub.Platform.Contracts.IModule module)
        {
            return _view.DeregisterPort(PortAdapter.C2V(port), ModuleAdapter.C2V(module));
        }

        public HomeOS.Hub.Platform.Contracts.IPortInfo GetPortInfo(string moduleFacingName, HomeOS.Hub.Platform.Contracts.IModule module)
        {
            return PortInfoAdapter.V2C(_view.GetPortInfo(moduleFacingName, ModuleAdapter.C2V(module)));
        }

        public void SetRoles(IPortInfo portInfo, IListContract<IRole> roles, IModule module)
        {
            _view.SetRoles(PortInfoAdapter.C2V(portInfo),
                                               CollectionAdapters.ToIList<IRole, VRole>(roles, RoleAdapter.C2V, RoleAdapter.V2C),
                                               ModuleAdapter.C2V(module));
        }

        public IListContract<IPort> GetAllPorts()
        {
            return CollectionAdapters.ToIListContract<VPort, IPort>(_view.GetAllPorts(), PortAdapter.V2C, PortAdapter.C2V);
        }

        public ICapability GetCapability(IModule module, IPort targetPort, string username, string password)
        {
            return CapabilityAdapter.V2C(_view.GetCapability(ModuleAdapter.C2V(module), PortAdapter.C2V(targetPort), username, password));
        }

        public int ModuleFinished(IModule module)
        {
            return _view.ModuleFinished(ModuleAdapter.C2V(module));
        }

        public bool IsValidUser(string username, string password)
        {
            return _view.IsValidUser(username, password);
        }

        public string GetConfSetting(string paramName)
        {
            return _view.GetConfSetting(paramName);
        }

        public string GetPrivateConfSetting(string paramName)
        {
            return _view.GetPrivateConfSetting(paramName);
        }

        public string GetDeviceIpAddress(string deviceId)
        {
            return _view.GetDeviceIpAddress(deviceId);
        }

        public bool SafeToGoOnline()
        {
            return _view.SafeToGoOnline();
        }

    }

    public class PlatformC2V : VPlatform
    {
        #region standard stuff DO NOT TOUCH
        private IPlatform _contract;
        private ContractHandle _handle;

        public PlatformC2V(IPlatform contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal IPlatform GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public int RegisterPort(VPort port, VModule module) 
        {
            return _contract.RegisterPort(PortAdapter.V2C(port), ModuleAdapter.V2C(module));
        }
        //***
        public void UpdateState(VModule module, VModuleState state)
        {
            _contract.UpdateState(ModuleAdapter.V2C(module), ModuleStateAdapter.V2C(state));
        }
        public void CancelAllSubscriptions(VModule module ,  VPort controlPort, VCapability controlportcap)
        {
            _contract.CancelAllSubscriptions(ModuleAdapter.V2C(module), PortAdapter.V2C(controlPort), CapabilityAdapter.V2C(controlportcap));
        }

        
        public int IsValidAccess(string accessedModule, string domainOfAccess, string privilegeLevel, string userIdentifier)
        {
            return _contract.IsValidAccess(accessedModule, domainOfAccess, privilegeLevel , userIdentifier);
        }

        //***

        public int DeregisterPort(VPort port, VModule module)
        {
            return _contract.DeregisterPort(PortAdapter.V2C(port), ModuleAdapter.V2C(module));
        }

        public VPortInfo GetPortInfo(string moduleFacingName, VModule module)
        {
            return PortInfoAdapter.C2V(_contract.GetPortInfo(moduleFacingName, ModuleAdapter.V2C(module)));
        }

        public IList<VPort> GetAllPorts()
        {
            return CollectionAdapters.ToIList<IPort, VPort>(_contract.GetAllPorts(), PortAdapter.C2V, PortAdapter.V2C);
        }

        public void SetRoles(VPortInfo portInfo, IList<VRole> roles, VModule module) {
            _contract.SetRoles(PortInfoAdapter.V2C(portInfo),
                                                   CollectionAdapters.ToIListContract<VRole, IRole>(roles, RoleAdapter.V2C, RoleAdapter.C2V),
                                                   ModuleAdapter.V2C(module));
        }

        public VCapability GetCapability(VModule module, VPort targetPort, string username, string password)
        {
            return CapabilityAdapter.C2V(_contract.GetCapability(ModuleAdapter.V2C(module), PortAdapter.V2C(targetPort), username, password));
        }

        public int ModuleFinished(VModule module)
        {
            return _contract.ModuleFinished(ModuleAdapter.V2C(module));
        }

        public bool IsValidUser(string username, string password)
        {
            return _contract.IsValidUser(username, password);
        }

        public string GetConfSetting(string paramName)
        {
            return _contract.GetConfSetting(paramName);
        }

        public string GetPrivateConfSetting(string paramName)
        {
            return _contract.GetPrivateConfSetting(paramName);
        }

        public string GetDeviceIpAddress(string deviceId)
        {
            return _contract.GetDeviceIpAddress(deviceId);
        }

        public bool SafeToGoOnline()
        {
            return _contract.SafeToGoOnline();
        }
    }

    #region standard stuff DO NOT TOUCH
    public class PlatformAdapter
    {
        internal static VPlatform C2V(IPlatform contract)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(PlatformV2C))))
            {
                return ((PlatformV2C)(contract)).GetSourceView();
            }
            else
            {
                return new PlatformC2V(contract);
            }
        }

        internal static IPlatform V2C(VPlatform view)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(PlatformC2V))))
            {
                return ((PlatformC2V)(view)).GetSourceContract();
            }
            else
            {
                return new PlatformV2C(view);
            }
        }
    }
    #endregion

}
