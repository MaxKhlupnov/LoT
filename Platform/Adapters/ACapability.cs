using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.Contracts;

namespace HomeOS.Hub.Platform.Adapters
{
    public class CapabilityV2C : ContractBase, ICapability
    {
        #region standard stuff DO NOT TOUCH
        private VCapability _view;

        public CapabilityV2C(VCapability view)
        {
            _view = view;
        }

        internal VCapability GetSourceView()
        {
            return _view;
        }
        #endregion

        public int RandomVal()
        {
            return _view.RandomVal();
        }

        public string IssuerId()
        {
            return _view.IssuerId();
        }

        public DateTime ExpiryTime()
        {
            return _view.ExpiryTime();
        }

    }

    public class CapabilityC2V : VCapability
    {
        #region standard stuff DO NOT TOUCH
        private ICapability _contract;
        private ContractHandle _handle;

        public CapabilityC2V(ICapability contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal ICapability GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public override int RandomVal()
        {
            return _contract.RandomVal();
        }

        public override string IssuerId()
        {
            return _contract.IssuerId();
        }

        public override DateTime ExpiryTime()
        {
            return _contract.ExpiryTime();
        }

    }

    public class CapabilityAdapter
    {
        internal static VCapability C2V(ICapability contract)
        {
            if (contract == null)
                return null;

            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(CapabilityV2C))))
            {
                return ((CapabilityV2C)(contract)).GetSourceView();
            }
            else
            {
                return new CapabilityC2V(contract);
            }
        }

        internal static ICapability V2C(VCapability view)
        {
            if (view == null)
                return null;

            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(CapabilityC2V))))
            {
                return ((CapabilityC2V)(view)).GetSourceContract();
            }
            else
            {
                return new CapabilityV2C(view);
            }
        }
    }

}
