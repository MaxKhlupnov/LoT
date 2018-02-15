using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    [DataContract]
    public class IndexInfo
    {
        [DataMember]
        public byte[] indexHash { get; set;}
        [DataMember]
        public byte[] chunkListHash { get; set; }
        [DataMember]
        public long startTime {get; set;}
        [DataMember]
        public long endTime {get; set;}
        [DataMember]
        public bool isSealed { get; set; }

        public IndexInfo()
        { }
    }

    
    [DataContract]
    public class IndexMetaData
    {
        private string FQFilename;

        [DataMember]
        public long startTime;
        [DataMember]
        public long duration;
        [DataMember]
        public Dictionary<int, IndexInfo> index_infos;
        [DataMember]
        public byte[] SignedHash;

        public IndexMetaData(string target_dir, string filename)
        {
            FQFilename = target_dir + "/" + filename;
            startTime = 0;
            duration = 0;
            index_infos = new Dictionary<int, IndexInfo>();
            SignedHash = null;
        }

        public bool LoadIndexMetaData()
        {
            try
            {
                TextReader mdtr = new StreamReader(FQFilename);
                string json = mdtr.ReadToEnd();
                mdtr.Close();

                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IndexMetaData));
                IndexMetaData md = (IndexMetaData)ser.ReadObject(ms);
                ms.Close();

                startTime = md.startTime;
                duration = md.duration;
                index_infos = md.index_infos;
                SignedHash = md.SignedHash;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load metadata file: " + FQFilename);
                Console.WriteLine("{0} Exception caught.", e);
                return false;
            }
        }

        public void FlushIndexMetaData()
        {
            TextWriter mdtw = new StreamWriter(FQFilename, false);
            
            MemoryStream ms = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IndexMetaData));
            ser.WriteObject(ms, this);
            byte[] json = ms.ToArray();
            ms.Close();

            mdtw.Write(Encoding.UTF8.GetString(json, 0, json.Length));
            mdtw.Flush();
            mdtw.Close();
        }

        protected Byte[] GetDataToHash()
        {
            byte[] data = { };
            data = data.Concat(BitConverter.GetBytes(startTime)).Concat(BitConverter.GetBytes(duration)).ToArray();
            for (int i = 0; i < index_infos.Count; ++i)
            {
                IndexInfo ii = index_infos[i];
                data = data.Concat(BitConverter.GetBytes(ii.startTime)).ToArray();
                data = data.Concat(BitConverter.GetBytes(ii.endTime)).ToArray();
                data = data.Concat(ii.indexHash).ToArray();
            }
            return data;
        }

        public void SignMetadata(string prikey)
        {
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            RSA.FromXmlString(prikey);
            startTime = StreamFactory.NowUtc();
            duration = long.MaxValue;
            SignedHash = RSA.SignData(GetDataToHash(), new SHA256CryptoServiceProvider());
            FlushIndexMetaData();
        }

        public bool VerifyMetadata(string pubkey)
        {
            if (LoadIndexMetaData() == false)
                return false;
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            RSA.FromXmlString(pubkey);

            // Integrity check
            if (RSA.VerifyData(GetDataToHash(), new SHA256CryptoServiceProvider(), SignedHash) == false)
                return false;

            // Freshness check
            // TODO: change limit to startTime + Duration
            // Check for overflow
            long limit = long.MaxValue;
            if ((StreamFactory.NowUtc() > limit) || (StreamFactory.NowUtc() < startTime))
                return false;
            return true;
        }

        public void Close()
        {
        }

        public void Dispose() // NOT virtual
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalizer from running.
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Call Dispose() on other objects owned by this instance.
                // You can reference other finalizable objects here.
                // ...
                Close();
            }

            // Release unmanaged resources owned by (just) this object.
            // ...
        }

        ~IndexMetaData()
        {
            Dispose(false);
        }

    }
}
