using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using ProtoBuf;

namespace HomeOS.Hub.Common.Bolt.DataStore
{   
    public interface IKey : IJsonSerializer, IEquatable<IKey>, IComparable<IKey>, IBinarySerializer
    {
        /* returns true if startKey >= key <= endKey */
        bool Between(IKey startKey, IKey endKey);
        int Size();
        void SetBytes(byte[] keyBytes);
        byte[] GetBytes();
    }

    [ProtoContract]
    [DataContract]
    [Serializable]
    public class StrKey : ValueSerializerBase<StrKey>, IKey
    {
        [ProtoMember(1)]
        [DataMember(Name = "key")]
        public string key { get; set; }

        public StrKey()
        {
        }

        public StrKey(string k) 
        {
            key = k;
        }

        public bool Equals(IKey other)
        {
            if (other == null)
                return false;

            StrKey sk = other as StrKey;
            if (this.key == sk.key)
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            StrKey sk = obj as StrKey;
            if (sk == null)
                return false;
            else
                return Equals(sk);
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }

        public int CompareTo(IKey other)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (other == null) return 1;

            StrKey sk = other as StrKey;
            return key.CompareTo(sk.key);
        }

        public bool Between(IKey startKey, IKey endKey)
        {
            if ((startKey == null) || (endKey == null)) return false;

            StrKey sk = startKey as StrKey;
            StrKey ek = endKey as StrKey;

            return ((key.CompareTo(sk.key) >= 0) && (key.CompareTo(ek.key) <= 0)) ? true : false;
        }
        
        public override string ToString()
        {
            return key;
        }

        public int Size()
        {
            return key.Length;
        }

        public byte[] GetBytes()
        {
            return StreamFactory.GetBytes(this.key);
        }

        public void SetBytes(byte[] keyBytes)
        {
            this.key = StreamFactory.GetString(keyBytes);
        }
    }

    [ProtoContract]
    [DataContract]
    [Serializable]
    public class DoubleKey : ValueSerializerBase<DoubleKey>, IKey
    {
        [ProtoMember(1)]
        [DataMember(Name = "key")]
        public double key { get; set; }

        public DoubleKey()
        {
        }

        public DoubleKey(double k)
        {
            key = k;
        }

        public bool Equals(IKey other)
        {
            if (other == null)
                return false;

            DoubleKey sk = other as DoubleKey;
            if (this.key == sk.key)
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            DoubleKey sk = obj as DoubleKey;
            if (sk == null)
                return false;
            else
                return Equals(sk);
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }

        public int CompareTo(IKey other)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (other == null) return 1;

            DoubleKey sk = other as DoubleKey;
            return key.CompareTo(sk.key);
        }

        public bool Between(IKey startKey, IKey endKey)
        {
            if ((startKey == null) || (endKey == null)) return false;

            DoubleKey sk = startKey as DoubleKey;
            DoubleKey ek = endKey as DoubleKey;

            return ( key >= sk.key && key <= ek.key) ? true : false;
        }

        public override string ToString()
        {
            return key.ToString();
        }

        public int Size()
        {
            return sizeof(double);
        }

        public byte[] GetBytes()
        {
            return BitConverter.GetBytes(this.key);
        }

        public void SetBytes(byte[] keyBytes)
        {
            this.key = BitConverter.ToDouble(keyBytes,0);
        }
    }

}
