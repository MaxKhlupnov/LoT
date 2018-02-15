
namespace HomeOS.Hub.Platform.Contracts
{
    using System;
    using System.AddIn.Contract;
    using System.AddIn.Pipeline;

    public interface IParamType : IContract
    {
        int Maintype();
        Object Value();
        string Name();
    }

}