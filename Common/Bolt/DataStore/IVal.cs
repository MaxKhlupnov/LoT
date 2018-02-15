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
    public interface IValue : IJsonSerializer, IBinarySerializer
    {
        byte[] GetBytes();
        int Size();
        void SetBytes(byte[] valBytes);
    }

    [ProtoContract]
    [DataContract]
    [Serializable]
    public class ByteValue : ValueSerializerBase<ByteValue>, IValue
    {
        [ProtoMember(1)]
        [DataMember(Name = "val")]
        public byte[] val { get; set; }

        public ByteValue()
        {
        }
        
        public ByteValue(byte[] v) 
        {
            val = v;
        }

        public override string ToString()
        {
            return  StreamFactory.GetString(val);
        }

        public byte[] GetBytes()
        {
            return val;
        }

        public int Size()
        {
            return val.Length;
        }

        public void SetBytes(byte[] valBytes)
        {
            this.val = valBytes;
        }
    }

    [ProtoContract]
    [DataContract]
    [Serializable]
    public class StrValue : ValueSerializerBase<StrValue>, IValue
    {
        [ProtoMember(1)]
        [DataMember(Name = "val")]
        public string val { get; set; }

        public StrValue()
        {
        }
        
        public StrValue(string v)
        {
            val = v;
        }

        public override string ToString()
        {
            return val;
        }
        
        public byte[] GetBytes()
        {
            return StreamFactory.GetASCIIBytes(val);
        }
        
        public int Size()
        {
            return val.Length;
        }

        public void SetBytes(byte[] valBytes)
        {
            this.val = StreamFactory.GetASCIIString(valBytes);
        }
    }
}
