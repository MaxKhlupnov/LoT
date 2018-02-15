using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using ProtoBuf;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public interface IBinarySerializer
    {
        MemoryStream SerializeToByteStream();
        object DeserializeFromByteStream(MemoryStream memStream);
    }

    public interface IJsonSerializer
    {
        string SerializeToJsonStream();
        object DeserializeFromJsonStream(string json);
    }

    [DataContract]
    public class SerializerHelper<T>
    {
        // Deserialize a JSON stream to a ModuleMonitorInfo object.
        public static T DeserializeFromJsonStream(string json)
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            object objInst = ser.ReadObject(ms);
            ms.Close();
            return (T)objInst;
        }

        public static T DeserializeFromByteStream(MemoryStream memStream)
        {
            BinaryFormatter binFormatter = new BinaryFormatter();
            memStream.Seek(0, SeekOrigin.Begin);
            return (T) binFormatter.Deserialize(memStream);
        }

        public static T DeserializeFromProtoStream(MemoryStream memStream)
        {
            memStream.Seek(0, SeekOrigin.Begin);
            return (T)Serializer.Deserialize<T>(memStream);
        }

        public static MemoryStream SerializeToProtoStream(T obj)
        {
            MemoryStream memStream = new MemoryStream();
            Serializer.Serialize<T>(memStream, obj);
            return memStream;
        }
    }

    [DataContract]
    [Serializable]
    public class ValueSerializerBase<T> : IBinarySerializer, IJsonSerializer
    {
        public MemoryStream SerializeToByteStream()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binFormatter = new BinaryFormatter();
            binFormatter.Serialize(memStream, this);
            return memStream;
        }

        public object DeserializeFromByteStream(MemoryStream memStream)
        {
            return SerializerHelper<T>.DeserializeFromByteStream(memStream);
        }

        public string SerializeToJsonStream()
        {
            //Create a stream to serialize the object to.
            MemoryStream ms = new MemoryStream();

            // Serializer the User object to the stream.
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            ser.WriteObject(ms, this);
            byte[] json = ms.ToArray();
            ms.Close();
            return Encoding.UTF8.GetString(json, 0, json.Length);

        }

        public object DeserializeFromJsonStream(string json)
        {
            return SerializerHelper<T>.DeserializeFromJsonStream(json);
        }
    }

    [DataContract]
    public class JsonSerializerBase<T> : IJsonSerializer
    {
        public string SerializeToJsonStream()
        {
            //Create a stream to serialize the object to.
            MemoryStream ms = new MemoryStream();

            // Serializer the User object to the stream.
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            ser.WriteObject(ms, this);
            byte[] json = ms.ToArray();
            ms.Close();
            return Encoding.UTF8.GetString(json, 0, json.Length);

        }

        public object DeserializeFromJsonStream(string json)
        {
            return SerializerHelper<T>.DeserializeFromJsonStream(json);
        }
    }
}
