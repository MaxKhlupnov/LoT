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

    public class ParamTypeV2C : ContractBase, IParamType
    {
        #region standard stuff DO NOT TOUCH
        private VParamType _view;

        public ParamTypeV2C(VParamType view)
        {
            _view = view;
        }

        internal VParamType GetSourceView()
        {
            return _view;
        }
        #endregion

        public int Maintype()
        {
            return _view.Maintype();
        }

        public Object Value()
        {
            return _view.Value();
        }

        public string Name()
        {
            return _view.Name();
        }
    }

    public class ParamTypeC2V : VParamType
    {
        #region standard stuff DO NOT TOUCH
        private IParamType _contract;
        private ContractHandle _handle;

        public ParamTypeC2V(IParamType contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal IParamType GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public int Maintype()
        {
            return _contract.Maintype();
        }

        public Object Value()
        {
            return _contract.Value();
        }

        public string Name()
        {
            return _contract.Name();
        }
    }

    #region standard stuff DO NOT TOUCH
    public class BaseTypeAdapter
    {
        internal static VParamType C2V(IParamType contract)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(ParamTypeV2C))))
            {
                return ((ParamTypeV2C)(contract)).GetSourceView();
            }
            else
            {
                return new ParamTypeC2V(contract);
            }
        }

        internal static IParamType V2C(VParamType view)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(ParamTypeC2V))))
            {
                return ((ParamTypeC2V)(view)).GetSourceContract();
            }
            else
            {
                return new ParamTypeV2C(view);
            }
        }
    }
    #endregion
}