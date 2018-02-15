using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Contract;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Contracts
{
    public interface IPortInfo : IContract
    {
        string GetFriendlyName();
        string ModuleFriendlyName();
        string ModuleFacingName();
        IListContract<IRole> GetRoles();
        ILocation GetLocation();
        bool IsSecure();
    }
}
