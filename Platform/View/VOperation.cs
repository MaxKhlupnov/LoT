using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeOS.Hub.Platform.Views
{   
    /// <summary>
    /// The abstract interface for an Operation. Operations are stored inside a PortInfo
    /// class and logically belong to a Port. Within that port, they are uniquely defined
    /// by their name.
    /// </summary>
    public abstract class VOperation: MarshalByRefObject
    {
        public abstract string Name();
        public abstract IList<VParamType> Parameters();
        public abstract IList<VParamType> ReturnValues();
        public abstract bool Subscribeable();

        public override int GetHashCode()
        {
            return Name().ToLower().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is VOperation)
                return Name().Equals(((VOperation)obj).Name(), StringComparison.CurrentCultureIgnoreCase);
            else
                return false;
        }

        public override string ToString()
        {
            return Name();
        }
    }
}
