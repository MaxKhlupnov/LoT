using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class DataBlockInfo : IEquatable<DataBlockInfo>, IComparable<DataBlockInfo>
    {
        public long ts;
        public long offset;
        public byte[] hashValue;
        //public uint key_version;

        public DataBlockInfo(long t, long o)
        {
            ts = t;
            offset = o;
            hashValue = null;
       //     key_version = 0;
        }

        public bool Equals(DataBlockInfo other)
        {
            if (other == null)
                return false;

            if (this.ts == other.ts && this.offset==other.offset ) // this.hashValue == other.hashValue && this.key_version == other.key_version)
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            DataBlockInfo dbi = obj as DataBlockInfo;
            if (dbi == null)
                return false;
            else
                return Equals(dbi);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int CompareTo(DataBlockInfo other)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (other == null) return 1;

            return ts.CompareTo(other.ts);
        }

        /* returns true if startTime >= ts <= endTime */
        public bool Between(long startTime, long endTime)
        {
            return ((ts >= startTime) && (ts <= endTime)) ? true : false;
        }

        public override string ToString()
        {
            return String.Format("TS {0} Offset {1} ", ts, offset); 
        }
    }

    public class DataBlockInfoComparer : IComparer<DataBlockInfo>
    {
        public int Compare(DataBlockInfo a, DataBlockInfo b)
        {
            return a.ts.CompareTo(b.ts);
        }
    }
}
