using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.Contracts;

namespace HomeOS.Hub.Platform.Adapters
{
    public class LocationV2C : ContractBase, ILocation
    {
        #region standard stuff DO NOT TOUCH
        private VLocation _view;

        public LocationV2C(VLocation view)
        {
            _view = view;
        }

        internal VLocation GetSourceView()
        {
            return _view;
        }
        #endregion

        public int ID()
        {
            return _view.ID();
        }

        public string Name()
        {
            return _view.Name();
        }
    }

    public class LocationC2V : VLocation
    {
        #region standard stuff DO NOT TOUCH
        private ILocation _contract;
        private ContractHandle _handle;

        public LocationC2V(ILocation contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal ILocation GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public override int ID()
        {
            return _contract.ID();
        }

        public override string Name()
        {
            return _contract.Name();
        }
    }

    #region standard stuff DO NOT TOUCH
    public class LocationAdapter
    {
        internal static VLocation C2V(ILocation contract)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(LocationV2C))))
            {
                return ((LocationV2C)(contract)).GetSourceView();
            }
            else
            {
                return new LocationC2V(contract);
            }
        }

        internal static ILocation V2C(VLocation view)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(LocationC2V))))
            {
                return ((LocationC2V)(view)).GetSourceContract();
            }
            else
            {
                return new LocationV2C(view);
            }
        }
    }
    #endregion

}
