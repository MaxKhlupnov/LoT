using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Contract;
using System.AddIn.Pipeline;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.Contracts;

namespace HomeOS.Hub.Platform.Adapters
{
    public class PortV2C : ContractBase, IPort
    {
        #region standard stuff DO NOT TOUCH
        private VPort _view;

        public PortV2C(VPort view)
        {
            _view = view;
        }

        internal VPort GetSourceView()
        {
            return _view;
        }
        #endregion

        public bool Subscribe(string roleName, string opName, IPort fromPort, ICapability reqCap, ICapability respCap)
        {
            return _view.Subscribe(roleName, opName, PortAdapter.C2V(fromPort), CapabilityAdapter.C2V(reqCap), CapabilityAdapter.C2V(respCap));
        }

        public bool Unsubscribe(string roleName, string opName, IPort fromPort, ICapability respCap)
        {
            return _view.Unsubscribe(roleName, opName, PortAdapter.C2V(fromPort), CapabilityAdapter.C2V(respCap));
        }

        public IListContract<IParamType> Invoke(string roleName, string opName, IListContract<IParamType> parameters, IPort p, ICapability reqCap, ICapability respCap)
        {
            return CollectionAdapters.ToIListContract<VParamType, IParamType>(_view.Invoke(roleName, opName, 
                                                                                                       CollectionAdapters.ToIList<IParamType, VParamType>(parameters, BaseTypeAdapter.C2V, BaseTypeAdapter.V2C),
                                                                                                       PortAdapter.C2V(p), 
                                                                                                       CapabilityAdapter.C2V(reqCap), 
                                                                                                       CapabilityAdapter.C2V(respCap)),
                                                                                           BaseTypeAdapter.V2C, BaseTypeAdapter.C2V);
        }

        public void AsyncReturn(string roleName, string opName, IListContract<IParamType> retVals, IPort p, ICapability respCap)
        {
            _view.AsyncReturn(roleName, opName,
                              CollectionAdapters.ToIList<IParamType, VParamType>(retVals, BaseTypeAdapter.C2V, BaseTypeAdapter.V2C), 
                              PortAdapter.C2V(p), CapabilityAdapter.C2V(respCap));
        }

        //public int Receive(IMessage message)
        //{
        //    return _view.Receive(MessageAdapter.C2V(message));
        //}

        public IPortInfo GetInfo()
        {
            return PortInfoAdapter.V2C(_view.GetInfo());
        }

        //public IListContract<object> Invoke(string roleName, string opName, IPort respPort, ICapability reqCap, ICapability respCap, params object[] arguments)
        //{
        //    var retVals = _view.Invoke(roleName, opName,
        //                                                                           PortAdapter.C2V(respPort),
        //                                                                           CapabilityAdapter.C2V(reqCap),
        //                                                                           CapabilityAdapter.C2V(respCap),
        //                                                                           arguments);
        //    return retVals;

        //    //return CollectionAdapters.ToIListContract<object, object>(retVals, BaseTypeAdapter.V2C, BaseTypeAdapter.C2V);
        //}

    }

    public class PortC2V : VPort
    {
        #region standard stuff DO NOT TOUCH
        private IPort _contract;
        private ContractHandle _handle;

        public PortC2V(IPort contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal IPort GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public override bool Subscribe(string roleName, string opName, VPort fromPort, VCapability reqCap, VCapability respCap)
        {
            return _contract.Subscribe(roleName, opName, PortAdapter.V2C(fromPort), CapabilityAdapter.V2C(reqCap), CapabilityAdapter.V2C(respCap));
        }

        public override bool Unsubscribe(string roleName, string opName, VPort fromPort, VCapability respCap)
        {
            return _contract.Unsubscribe(roleName, opName, PortAdapter.V2C(fromPort), CapabilityAdapter.V2C(respCap));
        }

        public override IList<VParamType> Invoke(string roleName, string opName, IList<VParamType> parameters, VPort p, VCapability reqCap, VCapability respCap)
        {
            return CollectionAdapters.ToIList<IParamType, VParamType>(_contract.Invoke(roleName, opName, 
                                                                                                   CollectionAdapters.ToIListContract<VParamType, IParamType>(parameters, BaseTypeAdapter.V2C, BaseTypeAdapter.C2V),
                                                                                                   PortAdapter.V2C(p), 
                                                                                                   CapabilityAdapter.V2C(reqCap), 
                                                                                                   CapabilityAdapter.V2C(respCap)),
                                                                                  BaseTypeAdapter.C2V, BaseTypeAdapter.V2C);
        }
        
        public override void AsyncReturn(string roleName, string opName, IList<VParamType> retVals, VPort p, VCapability respCap)
        {
            _contract.AsyncReturn(roleName, opName,
                                  CollectionAdapters.ToIListContract<VParamType, IParamType>(retVals, BaseTypeAdapter.V2C, BaseTypeAdapter.C2V), 
                                  PortAdapter.V2C(p), CapabilityAdapter.V2C(respCap));
        }

        public override VPortInfo GetInfo()
        {
            return PortInfoAdapter.C2V(_contract.GetInfo());
        }

        //public override IList<object> Invoke(string roleName, string opName, VPort respPort, VCapability reqCap, VCapability respCap, params object[] arguments)
        //{
        //    var retVals = _contract.Invoke(roleName, opName,
        //                                                                      PortAdapter.V2C(respPort),
        //                                                                      CapabilityAdapter.V2C(reqCap),
        //                                                                      CapabilityAdapter.V2C(respCap),
        //                                                                      arguments);

        //    //return retVals;
        //    return CollectionAdapters.ToIList<object,object>(retVals, BaseTypeAdapter.C2V, BaseTypeAdapter.V2C);

        //}

    }

    #region standard stuff DO NOT TOUCH
    public class PortAdapter
    {
        internal static VPort C2V(IPort contract)
        {
            if (contract == null)
            {
                return null;
            }

            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(PortV2C))))
            {
                return ((PortV2C)(contract)).GetSourceView();
            }
            else
            {
                return new PortC2V(contract);
            }
        }

        internal static IPort V2C(VPort view)
        {
            if (view == null)
            {
                return null;
            }

            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(PortC2V))))
            {
                return ((PortC2V)(view)).GetSourceContract();
            }
            else
            {
                return new PortV2C(view);
            }
        }
    }
    #endregion

}
