using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Security.Cryptography;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using HomeOS.Hub.Common.Bolt.DataStoreCommon;

namespace HomeOS.Hub.Common.MDServer
{
    public class program
    {
        static void Main(string[] args)
        {
            // string address = "http://localhost:23456/TrustedServer/";


            if(args==null || args.Count() <1 )
            {
                Usage();
                return;
            }


            string address = args[0];
            BasicHttpBinding binding = new BasicHttpBinding();
            ServiceHost host = new ServiceHost(typeof(MetaDataServer), new Uri(address));
            host.AddServiceEndpoint(typeof(IMetaDataService), binding, address);

            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            host.Description.Behaviors.Add(smb);
            host.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
            // host.AddDefaultEndpoints();

            MetaDataServer.initializeFromFile();
            host.Open();

            Console.WriteLine("Host is {0}.  Press enter to close.", host.State);
            Console.ReadLine();
            MetaDataServer.writeToFile();
            host.Close();
        }

        private static void Usage()
        {
            Console.WriteLine("Usage: "+Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)+" http://<IP or localhost>:<Port>/MetaDataServer/");
        }

    }

    [DataContract]
    public class MetaDataServer : IMetaDataService
    {
        [DataMember]
        static private Dictionary<string, string> keytable;
        [DataMember]
        static private Dictionary<string, StreamInfo> mdtable;
        static private HomeOS.Hub.Common.MDServer.Logger logger = new HomeOS.Hub.Common.MDServer.Logger();

        private static string KEYTABLEFILE = "keytable.dat";
        private static string MDTABLEFILE = "mdtable.dat";

        public static bool initializeFromFile()
        {
            try
            {
                //keytable = new Dictionary<string, string>();
                //mdtable = new Dictionary<string, StreamInfo>();
                using (Stream stream = File.Open(KEYTABLEFILE, FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    keytable = (Dictionary<string, string>)bin.Deserialize(stream);
                }

                using (Stream stream = File.Open(MDTABLEFILE, FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    mdtable = (Dictionary<string, StreamInfo>)bin.Deserialize(stream);
                }

                foreach (string key in keytable.Keys)
                {
                    logger.Log("Key read for principal: " + key);
                }

                foreach (string key in mdtable.Keys)
                {
                    logger.Log("MD read for stream: " + key);
                }


                return true;
            }
            catch (FileNotFoundException e)
            {
                logger.Log("File(s) not found. Initializing empty data structures. " + e.Message);
                keytable = new Dictionary<string, string>();
                mdtable = new Dictionary<string, StreamInfo>();
                return true;
            }
            catch (Exception e)
            {
                logger.Log("Exception in initializing from file: " + e);
                return false;
            }
        }

        public static bool writeToFile()
        {
            try
            {
                using (Stream stream = File.Open(KEYTABLEFILE, FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, keytable);
                }

                using (Stream stream = File.Open(MDTABLEFILE, FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, mdtable);
                }
                return true;

            }
            catch (Exception e)
            {
                logger.Log("Exception in writing tables to file: " + e);
                return false;
            }
        }

        public bool RegisterPubKey(Principal prin, string key)
        {
            logger.Log("RegisterPubKey request for " + prin.ToString());
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
            logger.Log("GetPubKey request for " + prin.ToString());
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
            logger.Log("UpdateReaderKey request from caller " + caller.ToString() + " for stream "
                + stream.ToString() + " and principal " + entry.readerName.ToString()
                + " key version " + entry.keyVersion);
            // TODO(trinabh): Authenticate caller
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
                logger.Log("Verification of request failed");
                return false;
            }
            //

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
            logger.Log("GetReaderKey from caller " + p.ToString() + " for stream "
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

        public bool AddAccount(FQStreamID stream, AccountInfo accinfo)
        {
            logger.Log("Adding account info for stream "
                + stream.ToString());
            if (!mdtable.ContainsKey(stream.ToString()))
            {
                mdtable[stream.ToString()] = new StreamInfo(stream);
            }
            return mdtable[stream.ToString()].AddAccount(accinfo);
        }

        public Dictionary<int, AccountInfo> GetAllAccounts(FQStreamID stream)
        {
            logger.Log("Serving account info for stream "
                + stream.ToString());
            if (mdtable.ContainsKey(stream.ToString()))
            {
                return mdtable[stream.ToString()].GetAllAccounts();
            }
            return null;
        }

        public bool AddMdAccount(FQStreamID stream, AccountInfo accinfo)
        {
            logger.Log("Adding md account info for stream "
                + stream.ToString());
            if (!mdtable.ContainsKey(stream.ToString()))
            {
                mdtable[stream.ToString()] = new StreamInfo(stream);
            }
            return mdtable[stream.ToString()].AddMdAccount(accinfo);
        }

        public AccountInfo GetMdAccount(FQStreamID stream)
        {
            AccountInfo retVal = null;
            
            if (mdtable.ContainsKey(stream.ToString()))
            {
                retVal=mdtable[stream.ToString()].GetMdAccount();
            }

            logger.Log("Serving md account info for stream "+ stream.ToString()+" as "+retVal);
            return retVal;
        }

        public void RemoveAllInfo(FQStreamID stream)
        {
            logger.Log("Removing all info for stream "
                + stream.ToString());
            if (mdtable.ContainsKey(stream.ToString()))
            {
                mdtable.Remove(stream.ToString());
            }
        }
    }
}