using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Views
{
    public abstract class VModuleState : MarshalByRefObject
    {
        public abstract int GetSimpleState();
        public abstract DateTime GetTimestamp();
        public abstract void Update(VModuleState s);
    }
}
