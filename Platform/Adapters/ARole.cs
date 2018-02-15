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
    public class RoleV2C : ContractBase, IRole
    {
        #region standard stuff DO NOT TOUCH
        private VRole _view;

        public RoleV2C(VRole view)
        {
            _view = view;
        }

        internal VRole GetSourceView()
        {
            return _view;
        }
        #endregion

        public string Name()
        {
            return _view.Name();
        }

        public IListContract<IOperation> GetOperations()
        {
            return CollectionAdapters.ToIListContract<VOperation, IOperation>(_view.GetOperations(), 
                                                                                            OperationAdapter.V2C, OperationAdapter.C2V);
        }
    }

    public class RoleC2V : VRole
    {
        #region standard stuff DO NOT TOUCH
        private IRole _contract;
        private ContractHandle _handle;

        public RoleC2V(IRole contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal IRole GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public override string Name()
        {
            return _contract.Name();
        }

        public override IList<VOperation> GetOperations()
        {
            return CollectionAdapters.ToIList<IOperation, VOperation>(_contract.GetOperations(), 
                                                                                    OperationAdapter.C2V, OperationAdapter.V2C);
        }
    }

    #region standard stuff DO NOT TOUCH
    public class RoleAdapter
    {
        internal static VRole C2V(IRole contract)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(RoleV2C))))
            {
                return ((RoleV2C)(contract)).GetSourceView();
            }
            else
            {
                return new RoleC2V(contract);
            }
        }

        internal static IRole V2C(VRole view)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(RoleC2V))))
            {
                return ((RoleC2V)(view)).GetSourceContract();
            }
            else
            {
                return new RoleV2C(view);
            }
        }
    }
    #endregion

}
