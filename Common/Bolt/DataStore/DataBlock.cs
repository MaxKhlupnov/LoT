using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    [ProtoContract]
    [DataContract]
    [KnownType("GetKnownTypes")]
    [Serializable]
    class DataBlock<KeyType, ValType> : ValueSerializerBase<DataBlock<KeyType, ValType>>
        where KeyType : IKey, new()
        where ValType : IValue, new()
    {
        public DataBlock()
        {
            if (!typeof(IKey).IsAssignableFrom(typeof(KeyType)))
            {
                throw new InvalidDataException("KeyType must implement IKey");
            }
            if (!typeof(IValue).IsAssignableFrom(typeof(ValType)))
            {
                throw new InvalidDataException("ValType must implement IValue");
            }
        }

        public static Type[] knownTypes = new Type[] {typeof(KeyType), typeof(ValType)};
        public static Type[] GetKnownTypes()
        {
            return knownTypes;
        }

        [ProtoMember(1)]
        [DataMember(Name = "op")]
        public byte op { get; set; }

        /*
        [ProtoMember(2)]
        [DataMember(Name = "timestamp")]
        public long timestamp { get; set; }
         */

        /*
        [ProtoMember(3)]
        public byte[] key_bytes;

        [DataMember(Name = "key")]
        private IKey key;

        public IKey getKey()
        {
            if ((this.key == null) && (this.key_bytes != null))
            {
                this.key = new KeyType();
                this.key.SetBytes(key_bytes);
            }
            return this.key;
        }

        public void setKey(IKey k)
        {
            this.key = k;
            this.key_bytes = k.GetBytes();
        }
        */

        [ProtoMember(4)]
        private byte[] val_bytes;

        [DataMember(Name = "value")]
        private IValue value;

        public IValue getValue()
        {
            if ((this.value == null) && (this.val_bytes != null))
            {
                this.value = new ValType();
                this.value.SetBytes(val_bytes);
            }
            return this.value;
        }

        public void setValue(IValue v)
        {
            this.value = v; 
            this.val_bytes = v.GetBytes(); 
        }
    }
}
