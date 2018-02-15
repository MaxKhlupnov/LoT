using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.Contracts;

namespace HomeOS.Hub.Platform.Adapters
{
    public class ModuleInfoV2C : ContractBase, IModuleInfo
    {
        #region standard stuff DO NOT TOUCH
        private VModuleInfo _view;

        public ModuleInfoV2C(VModuleInfo view)
        {
            _view = view;
        }

        internal VModuleInfo GetSourceView()
        {
            return _view;
        }
        #endregion

        public string FriendlyName()
        {
            return _view.FriendlyName();
        }

        public string AppName()
        {
            return _view.AppName();
        }

        public string BinaryDir()
        {
            return _view.BinaryDir();
        }

        public string BinaryName()
        {
            return _view.BinaryName();
        }

        public string[] Args()
        {
            return _view.Args();
        }

        public string WorkingDir()
        {
            return _view.WorkingDir();
        }

        public string BaseURL()
        {
            return _view.BaseURL();
        }
    }

    public class ModuleInfoC2V : VModuleInfo
    {
        #region standard stuff DO NOT TOUCH
        private IModuleInfo _contract;
        private ContractHandle _handle;

        public ModuleInfoC2V(IModuleInfo contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal IModuleInfo GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public override string FriendlyName()
        {
            return _contract.FriendlyName();
        }

        public override string AppName()
        {
            return _contract.AppName();
        }

        public override string BinaryDir()
        {
            return _contract.BinaryDir();
        }

        public override string BinaryName()
        {
            return _contract.BinaryName();
        }

        public override string[] Args()
        {
            return _contract.Args();
        }

        public override string WorkingDir()
        {
            return _contract.WorkingDir();
        }

        public override string BaseURL()
        {
            return _contract.BaseURL();
        }

    }

    public class ModuleInfoAdapter
    {
        internal static VModuleInfo C2V(IModuleInfo contract)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(ModuleInfoV2C))))
            {
                return ((ModuleInfoV2C)(contract)).GetSourceView();
            }
            else
            {
                return new ModuleInfoC2V(contract);
            }
        }

        internal static IModuleInfo V2C(VModuleInfo view)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(ModuleInfoC2V))))
            {
                return ((ModuleInfoC2V)(view)).GetSourceContract();
            }
            else
            {
                return new ModuleInfoV2C(view);
            }
        }
    }

}
