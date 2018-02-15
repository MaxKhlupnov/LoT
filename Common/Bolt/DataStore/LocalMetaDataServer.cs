using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.IO;

//using HomeOS.Hub.Common.MDServer;
using HomeOS.Hub.Common.Bolt.DataStoreCommon;


namespace HomeOS.Hub.Common.Bolt.DataStore
{
    [DataContract]
    public class LocalMetaDataServer : IMetaDataService
    {
        private string FQFilename;
        
        [DataMember]
        private Dictionary<string, string> keytable = new Dictionary<string, string>();
        [DataMember]
        private Dictionary<string, StreamInfo> mdtable = new Dictionary<string, StreamInfo>();

        private Logger logger;

        public bool RegisterPubKey(Principal prin, string key)
        {
            if (logger != null) logger.Log("RegisterPubKey request for " + prin.ToString());
            if (keytable.ContainsKey(prin.ToString()))
            {
                return false;
            }
            else
            {
                keytable[prin.ToString()] = key;
            }
            return true;
        }

        public string GetPubKey(Principal prin)
        {
            if (logger != null) logger.Log("GetPubKey request for " + prin.ToString());
            // TODO(trinabh): return should be signed
            if (keytable.ContainsKey(prin.ToString()))
            {
                return keytable[prin.ToString()];
            }
            else
            {
                return null;
            }
        }

        public byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public bool UpdateReaderKey(Principal caller, FQStreamID stream, ACLEntry entry)
        {
            if (logger != null) logger.Log("UpdateReaderKey request from caller " + caller.ToString() + " for stream "
                + stream.ToString() + " and principal " + entry.readerName.ToString()
                + " key version " + entry.keyVersion);
            
            // Authentication is not required for unlisted streams
            /*
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            string callerpubkey = GetPubKey(caller);
            if (callerpubkey == null)
                return false;
            RSA.FromXmlString(callerpubkey);

            Byte[] data = { };
            data = data.Concat(this.GetBytes(caller.HomeId)).ToArray();
            data = data.Concat(this.GetBytes(caller.AppId)).ToArray();
            data = data.Concat(this.GetBytes(stream.HomeId)).ToArray();
            data = data.Concat(this.GetBytes(stream.AppId)).ToArray();
            data = data.Concat(this.GetBytes(stream.StreamId)).ToArray();
            data = data.Concat(this.GetBytes(entry.readerName.HomeId)).ToArray();
            data = data.Concat(this.GetBytes(entry.readerName.AppId)).ToArray();
            data = data.Concat(entry.encKey).ToArray();
            data = data.Concat(entry.IV).ToArray();
            data = data.Concat(this.GetBytes("" + entry.keyVersion)).ToArray();

            if (RSA.VerifyData(data, new SHA256CryptoServiceProvider(), caller.Auth) == false)
            {
                if (logger != null) logger.Log("Verification of request failed");
                return false;
            }
            //
            */

            if (caller.HomeId == stream.HomeId && caller.AppId == stream.AppId)
            {
                if (!mdtable.ContainsKey(stream.ToString()))
                    mdtable[stream.ToString()] = new StreamInfo(stream);
                mdtable[stream.ToString()].UpdateReader(entry);
                return true;
            }
            else
            {
                return false;
            }
        }

        public ACLEntry GetReaderKey(FQStreamID stream, Principal p)
        {
            if (logger != null) logger.Log("GetReaderKey from caller " + p.ToString() + " for stream "
                + stream.ToString());
            // TODO(trinabh): Return should be signed
            if (mdtable.ContainsKey(stream.ToString()))
            {
                return mdtable[stream.ToString()].GetReader(p);
            }
            return null;
        }

        public List<Principal> GetAllReaders(FQStreamID stream)
        {
            if (mdtable.ContainsKey(stream.ToString()))
            {
                return mdtable[stream.ToString()].GetAllReaders();
            }
            return null;
        }

        public bool AddMdAccount(FQStreamID stream, AccountInfo accinfo)
        {
            if (logger != null) logger.Log("Adding md account info for stream "
                + stream.ToString());
            if (!mdtable.ContainsKey(stream.ToString()))
            {
                mdtable[stream.ToString()] = new StreamInfo(stream);
            }
            return mdtable[stream.ToString()].AddMdAccount(accinfo);
        }


        public bool AddAccount(FQStreamID stream, AccountInfo accinfo)
        {
            if (logger != null) logger.Log("Adding account info for stream "
                + stream.ToString());
            if (!mdtable.ContainsKey(stream.ToString()))
            {
                mdtable[stream.ToString()] = new StreamInfo(stream);
            }
            return mdtable[stream.ToString()].AddAccount(accinfo);
        }

        public Dictionary<int, AccountInfo> GetAllAccounts(FQStreamID stream)
        {
            if (logger != null) logger.Log("Serving account info for stream "
                + stream.ToString());
            if (mdtable.ContainsKey(stream.ToString()))
            {
                return mdtable[stream.ToString()].GetAllAccounts();
            }
            return null;
        }

        public AccountInfo GetMdAccount(FQStreamID stream)
        {
            if (logger != null) logger.Log("Serving md account info for stream "
                + stream.ToString());
            if (mdtable.ContainsKey(stream.ToString()))
            {
                return mdtable[stream.ToString()].GetMdAccount();
            }
            return null;
        }

        public void RemoveAllInfo(FQStreamID stream)
        {
            if (logger != null) logger.Log("Removing all info for stream "
                + stream.ToString());
            if (mdtable.ContainsKey(stream.ToString()))
            {
                mdtable.Remove(stream.ToString());
            }
        }

        public LocalMetaDataServer(string filename, Logger log)
        {
            FQFilename = filename;
            logger = log;
            keytable = new Dictionary<string, string>();
            mdtable = new Dictionary<string, StreamInfo>();
        }
        
        public bool LoadMetaDataServer()
        {
            if (!File.Exists(FQFilename))
                return false;
            
            try
            {
                TextReader mdtr = new StreamReader(FQFilename);
                string json = mdtr.ReadToEnd();
                mdtr.Close();

                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(LocalMetaDataServer));
                LocalMetaDataServer ts = (LocalMetaDataServer)ser.ReadObject(ms);
                keytable = ts.keytable;
                mdtable = ts.mdtable;
                return true;
            }
            // catch (Exception e)
            catch
            {
                Console.WriteLine("Failed to load metadata file: " + FQFilename);
                // Console.WriteLine("{0} Exception caught.", e);
                return false;
            }
        }

        public void FlushMetaDataServer()
        {
            TextWriter mdtw = new StreamWriter(FQFilename, false);
            
            MemoryStream ms = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(LocalMetaDataServer));
            ser.WriteObject(ms, this);
            byte[] json = ms.ToArray();
            ms.Close();

            mdtw.Write(Encoding.UTF8.GetString(json, 0, json.Length));
            mdtw.Flush();
            mdtw.Close();
        }
    }
}
