
namespace HomeOS.Hub.Platform.Views
{
    using System;

    /// <summary>
    /// The list of base types supported by HomeOS Port operations, these are the actual
    /// types used for the inputs and outputs of operations.
    /// </summary>
    public interface VParamType
    {
        int Maintype();
        object Value();
        string Name();
    }
}