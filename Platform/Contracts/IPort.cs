using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Contract;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Contracts
{
    public interface IPort : IContract
    {
        bool Subscribe(string roleName, string opName, IPort fromPort, ICapability reqCap, ICapability respCap);
        bool Unsubscribe(string roleName, string opName, IPort fromPort, ICapability respCap);
        IListContract<IParamType> Invoke(string roleName, string opName, IListContract<IParamType> parameters, IPort respPort, ICapability reqCap, ICapability respCap);
        void AsyncReturn(string roleName, string opName, IListContract<IParamType> retVals, IPort srcPort, ICapability respCap);
        IPortInfo GetInfo();

        //IListContract<object> Invoke(string roleName, string opName, IPort respPort, ICapability reqCap, ICapability respCap, params object[] arguments);

    }
}
