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
    [AddInAdapter()]
    public class ModuleV2C : ContractBase, IModule
    {
        #region  Standard stuff for contract to view conversion DO NOT TOUCH
        private VModule _view;

        public ModuleV2C(VModule view)
        {
            _view = view;
        }

        internal VModule GetSourceView()
        {
            return _view;
        }
        #endregion

        public IModuleInfo GetInfo() { 
            return ModuleInfoAdapter.V2C(_view.GetInfo());
        }

        public void Initialize(IPlatform platform, ILogger logger, IModuleInfo moduleInfo, int secret)
        {
            _view.Initialize(PlatformAdapter.C2V(platform), LoggerAdapter.C2V(logger), ModuleInfoAdapter.C2V(moduleInfo), secret);
        }

        //*** These four methods now DO NOT point to same named methods in VModule. Instead they point to ___WithHooks virtual methods 
        // which are implemented in ModuleBase
        public void Start()
        {
            _view.StartWithHooks();
        }


        public void Stop()
        {
            _view.StopWithHooks();
        }

        public void PortRegistered(HomeOS.Hub.Platform.Contracts.IPort port)
        {
            _view.PortRegisteredWithHooks(PortAdapter.C2V(port));
        }

        public void PortDeregistered(HomeOS.Hub.Platform.Contracts.IPort port)
        {
            _view.PortDeregisteredWithHooks(PortAdapter.C2V(port));
        }
        //***

        public int InstallCapability(HomeOS.Hub.Platform.Contracts.ICapability capability, HomeOS.Hub.Platform.Contracts.IPort targetPort)
        {
            return _view.InstallCapability(CapabilityAdapter.C2V(capability), PortAdapter.C2V(targetPort));
        }

        public int Secret()
        {
            return _view.Secret();
        }

        public object OpaqueCall(string callName, params object[] args)
        {
            return _view.OpaqueCall(callName, args);
        }

        public IListContract<long> GetResourceUsage()
        {
            return CollectionAdapters.ToIListContract<long>(_view.GetResourceUsage());
        }

        public string GetImageUrl(string hint)
        {
            return _view.GetImageUrl(hint);
        }

        public string GetDescription(string hint)
        {
            return _view.GetDescription(hint);
        }

        public void OnlineStatusChanged(bool newStatus)
        {
            _view.OnlineStatusChanged(newStatus);
        }
    }

    [HostAdapter()]
    public class ModuleC2V : VModule
    {
        #region  Standard stuff for contract to view conversion DO NOT TOUCH
        private IModule _contract;
        private ContractHandle _handle;

        public ModuleC2V(IModule contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal IModule GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public override VModuleInfo GetInfo()
        {
            return ModuleInfoAdapter.C2V(_contract.GetInfo());
        }
        
        public override void Initialize(VPlatform platform, VLogger logger, VModuleInfo moduleInfo, int secret)
        {
            _contract.Initialize(PlatformAdapter.V2C(platform), LoggerAdapter.V2C(logger), ModuleInfoAdapter.V2C(moduleInfo), secret);

            //IPlatform iPlatform = PlatformAdapter.V2C(platform);
            //ILogger iLogger = LoggerAdapter.V2C(logger);
            //IModuleInfo iModuleInfo = ModuleInfoAdapter.V2C(moduleInfo);

            //_contract.Initialize(iPlatform, iLogger, iModuleInfo);
        }

        public override int Secret()
        {
            return _contract.Secret();
        }

        public override void Start()
        {
            _contract.Start();
        }

        public override void Stop()
        {
            _contract.Stop();
        }

        public override int InstallCapability(VCapability capability, VPort targetPort)
        {
            return _contract.InstallCapability(CapabilityAdapter.V2C(capability), PortAdapter.V2C(targetPort));
        }

        public override void PortRegistered(VPort port)
        {
            _contract.PortRegistered(PortAdapter.V2C(port));
        }

        public override void PortDeregistered(VPort port)
        {
            _contract.PortDeregistered(PortAdapter.V2C(port));
        }

        public override object OpaqueCall(string callName, params object[] args)
        {
            return _contract.OpaqueCall(callName, args);
        }

        public override IList<long> GetResourceUsage()
        {
            return CollectionAdapters.ToIList<long>(_contract.GetResourceUsage());
        }

        public override string GetImageUrl(string hint)
        {
            return _contract.GetImageUrl(hint);
        }

        public override string GetDescription(string hint)
        {
            return _contract.GetDescription(hint);
        }

        public override void OnlineStatusChanged(bool newStatus)
        {
            _contract.OnlineStatusChanged(newStatus);
        }
    }

    #region ModuleAdapter DO NOT TOUCH
    public class ModuleAdapter
    {
        internal static VModule C2V(IModule contract)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(ModuleV2C))))
            {
                return ((ModuleV2C)(contract)).GetSourceView();
            }
            else
            {
                return new ModuleC2V(contract);
            }
        }

        internal static IModule V2C(VModule view)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(ModuleC2V))))
            {
                return ((ModuleC2V)(view)).GetSourceContract();
            }
            else
            {
                return new ModuleV2C(view);
            }
        }
    }
    #endregion 

}
