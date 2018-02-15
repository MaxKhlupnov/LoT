using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Platform.Adapters
{
    public class ModuleStateV2C : ContractBase, HomeOS.Hub.Platform.Contracts.IModuleState
    {
        #region standard stuff DO NOT TOUCH
        private VModuleState _view;

        public ModuleStateV2C(VModuleState view)
        {
            _view = view;
        }

        internal VModuleState GetSourceView()
        {
            return _view;
        }
        #endregion

        public int GetSimpleState()
        {
            return _view.GetSimpleState();
        }

        public DateTime GetTimestamp()
        {
            return _view.GetTimestamp();
        }

        public void Update(HomeOS.Hub.Platform.Contracts.IModuleState s)
        {
           _view.Update(ModuleStateAdapter.C2V(s));
        }



    }

    public class ModuleStateC2V : VModuleState
    {

        #region standard stuff DO NOT TOUCH
        private HomeOS.Hub.Platform.Contracts.IModuleState _contract;
        private ContractHandle _handle;

        public ModuleStateC2V(HomeOS.Hub.Platform.Contracts.IModuleState contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal HomeOS.Hub.Platform.Contracts.IModuleState GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public override int GetSimpleState()
        {
            return _contract.GetSimpleState();
        }

        public override DateTime GetTimestamp()
        {
            return _contract.GetTimestamp();
        }

        public override void Update(VModuleState s)
        {
            _contract.Update(ModuleStateAdapter.V2C(s));
        }




    }


    public class ModuleStateAdapter
    {
        internal static VModuleState C2V(HomeOS.Hub.Platform.Contracts.IModuleState contract)
        {
            if (contract == null)
                return null;

            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(ModuleStateV2C))))
            {
                return ((ModuleStateV2C)(contract)).GetSourceView();
            }
            else
            {
                return new ModuleStateC2V(contract);
            }
        }

        internal static HomeOS.Hub.Platform.Contracts.IModuleState V2C(VModuleState view)
        {
            if (view == null)
                return null;

            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(ModuleStateC2V))))
            {
                return ((ModuleStateC2V)(view)).GetSourceContract();
            }
            else
            {
                return new ModuleStateV2C(view);
            }
        }

    }

}
