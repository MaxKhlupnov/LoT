using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.Contracts;

namespace HomeOS.Hub.Platform.Adapters
{
    public class LoggerV2C : ContractBase, ILogger
    {
        #region standard stuff DO NOT TOUCH
        private VLogger _view;

        public LoggerV2C(VLogger view)
        {
            _view = view;
        }

        internal VLogger GetSourceView()
        {
            return _view;
        }
        #endregion

        public void Log(string format, params string[] args)
        {
            _view.Log(format, args);
        }
    }

    public class LoggerC2V : VLogger
    {
        #region standard stuff DO NOT TOUCH
        private ILogger _contract;
        private ContractHandle _handle;

        public LoggerC2V(ILogger contract)
        {
            _contract = contract;
            _handle = new ContractHandle(contract);
        }

        internal ILogger GetSourceContract()
        {
            return _contract;
        }
        #endregion

        public void Log(string format, params string[] args)
        {
            _contract.Log(format, args);
        }
    }

    #region standard stuff for Adapter DO NOT TOUCH
    public class LoggerAdapter
    {
        internal static VLogger C2V(ILogger contract)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(contract) &&
                (contract.GetType().Equals(typeof(LoggerV2C))))
            {
                return ((LoggerV2C)(contract)).GetSourceView();
            }
            else
            {
                return new LoggerC2V(contract);
            }
        }

        internal static ILogger V2C(VLogger view)
        {
            if (!System.Runtime.Remoting.RemotingServices.IsObjectOutOfAppDomain(view) &&
                (view.GetType().Equals(typeof(LoggerC2V))))
            {
                return ((LoggerC2V)(view)).GetSourceContract();
            }
            else
            {
                return new LoggerV2C(view);
            }
        }
    }
    #endregion


}
