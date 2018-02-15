using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeOS.Hub.Platform.Views
{
    public abstract class VRole: MarshalByRefObject
    {
        public abstract string Name();
        public abstract IList<VOperation> GetOperations();

        public override string ToString()
        {
            return this.Name();
        }

        public override bool Equals(object obj)
        {
            if (obj is VRole)
                return this.Name().Equals(((VRole)obj).Name(), StringComparison.CurrentCultureIgnoreCase);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return Name().ToLower().GetHashCode();
        }
    }
}
