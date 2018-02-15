using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeOS.Hub.Platform.Views
{
    public abstract class VLocation: MarshalByRefObject
    {
        public abstract int ID();
        public abstract string Name();

        public override string ToString()
        {
            return this.Name();
        }
    }
}
