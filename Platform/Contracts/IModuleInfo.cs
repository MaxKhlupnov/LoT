using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Contract;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Contracts
{
    public interface IModuleInfo : IContract
    {
        string FriendlyName();
        string[] Args();
        string AppName();
        string BinaryDir();
        string BinaryName();
        string WorkingDir();
        string BaseURL();
    }
}
