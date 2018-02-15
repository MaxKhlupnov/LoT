using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Views
{
    public abstract class VCapability: MarshalByRefObject
    {
        public abstract string IssuerId();
        public abstract DateTime ExpiryTime();
        public abstract int RandomVal();

        /// <summary>
        /// Get hash code of this capability by taking the bitwise XOR of constituent hash codes
        /// </summary>
        public override int GetHashCode()
        {
            return IssuerId().GetHashCode() ^ ExpiryTime().GetHashCode() ^ RandomVal().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj != null &&
                    obj is VCapability &&
                    this.Equals((VCapability)obj));
        }

        public bool Equals(VCapability otherCap)
        {
            return (RandomVal() == otherCap.RandomVal() &&
                    IssuerId().Equals(otherCap.IssuerId()) &&
                    ExpiryTime().Equals(otherCap.ExpiryTime()));
        }
    }
}
