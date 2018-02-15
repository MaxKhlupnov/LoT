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
    public class PortInfoV2C : ContractBase, IPortInfo
    {
        #region standard stuff DO NOT TOUCH
        private VPortInfo _view;

        public PortInfoV2C(VPortInfo view)
        {
            _view = view;
        }

        internal VPortInfo GetSourceView()
        {
            return _view;
        }
        #endregion

        public string ModuleFriendlyName()
        {
            return _view.ModuleFriendlyName();
        }

        public string GetFriendlyName()
        {
            return _view.GetFriendlyName();
        }

        public string ModuleFacingName()
        {
            return _view.ModuleFacingName();
        }

        public ILocation GetLocation()
        {
            return LocationAdapter.V2C(_view.GetLocation());
        }

        public bool IsSecure()
        {
            return _view.IsSecure();
        }

        public IListContract<IRole> GetRoles()
        {
            return CollectionAdapters.ToIListContract<VRole, IRole>(_view.GetRoles(), RoleAdapter.V2C, RoleAdapter.C2V);
        }       
    }

    public class PortInfoC2V : VPortInfo
    {
        #region standard stuff DO NOT TOUCH
        private IPortInfo _contract;
        private ContractHandle _handle;

        public PortInfoC2V(IPortInfo contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal IPortInfo GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public override string GetFriendlyName()
        {
            return _contract.GetFriendlyName();
        }

        public override string ModuleFriendlyName()
        {
            return _contract.ModuleFriendlyName();
        }

        public override string ModuleFacingName()
        {
            return _contract.ModuleFacingName();
        }

        public override VLocation GetLocation()
        {
            return LocationAdapter.C2V(_contract.GetLocation());
        }

        public override bool IsSecure()
        {
            return _contract.IsSecure();
        }

        public override IList<VRole> GetRoles()
        {
            return CollectionAdapters.ToIList<IRole, VRole>(_contract.GetRoles(), RoleAdapter.C2V, RoleAdapter.V2C);
        }       
    }

    #region standard stuff DO NOT TOUCH
    public class PortInfoAdapter
    {
        internal static VPortInfo C2V(IPortInfo contract)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(PortInfoV2C))))
            {
                return ((PortInfoV2C)(contract)).GetSourceView();
            }
            else
            {
                return new PortInfoC2V(contract);
            }
        }

        internal static IPortInfo V2C(VPortInfo view)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(PortInfoC2V))))
            {
                return ((PortInfoC2V)(view)).GetSourceContract();
            }
            else
            {
                return new PortInfoV2C(view);
            }
        }
    }
    #endregion

}
