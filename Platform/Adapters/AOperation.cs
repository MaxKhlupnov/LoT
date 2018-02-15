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
    public class OperationV2C : ContractBase, IOperation
    {
        #region standard stuff DO NOT TOUCH
        private VOperation _view;

        public OperationV2C(VOperation view)
        {
            _view = view;
        }

        internal VOperation GetSourceView()
        {
            return _view;
        }
        #endregion

        public string Name()
        {
            return _view.Name();
        }

        public IListContract<IParamType> Parameters()
        {
            return CollectionAdapters.ToIListContract<VParamType, IParamType>(_view.Parameters(), BaseTypeAdapter.V2C, BaseTypeAdapter.C2V);
        }
        
        public IListContract<IParamType> ReturnValues()
        {
            return CollectionAdapters.ToIListContract<VParamType, IParamType>(_view.ReturnValues(), BaseTypeAdapter.V2C, BaseTypeAdapter.C2V);
        }

        public bool Subscribeable()
        {
            return _view.Subscribeable();
        }
    }

    public class OperationC2V : VOperation
    {
        #region standard stuff DO NOT TOUCH
        private IOperation _contract;
        private ContractHandle _handle;

        public OperationC2V(IOperation contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal IOperation GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public override string Name()
        {
            return _contract.Name();
        }

        public override IList<VParamType> Parameters()
        {
            return CollectionAdapters.ToIList<IParamType, VParamType>(_contract.Parameters(), BaseTypeAdapter.C2V, BaseTypeAdapter.V2C);
        }

        public override IList<VParamType> ReturnValues()
        {
            return CollectionAdapters.ToIList<IParamType, VParamType>(_contract.ReturnValues(), BaseTypeAdapter.C2V, BaseTypeAdapter.V2C);
        }

        public override bool Subscribeable()
        {
            return _contract.Subscribeable();
        }

    }

    #region standard stuff DO NOT TOUCH
    public class OperationAdapter
    {
        internal static VOperation C2V(IOperation contract)
        {
            if (contract == null)
            {
                return null;
            }

            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(OperationV2C))))
            {
                return ((OperationV2C)(contract)).GetSourceView();
            }
            else
            {
                return new OperationC2V(contract);
            }
        }

        internal static IOperation V2C(VOperation view)
        {
            if (view == null)
            {
                return null;
            }

            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(OperationC2V))))
            {
                return ((OperationC2V)(view)).GetSourceContract();
            }
            else
            {
                return new OperationV2C(view);
            }
        }
    }
    #endregion

}
