using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using System.IO;
using System.ServiceModel;
using System.Configuration;

using HomeOS.Hub.Common.Bolt.DataStoreCommon;


namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public sealed class StreamFactory
    {
        private static volatile StreamFactory instance;
        private static object syncRoot = new Object();

        public enum StreamOp : byte { Read = 0, Write }
        public enum StreamSecurityType : byte { Plain = 0, Secure}
        public enum StreamDataType : byte { Values = 0, Files }

        private StreamFactory() { }

        public static StreamFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new StreamFactory();
                    }

                    if (!Stopwatch.IsHighResolution)
                    {
                        throw new PlatformNotSupportedException("High frequency timer not available!");
                    }
                }

                return instance;
            }
        }

        /* syncIntervalSec:
         *   -ve ==> don't sync on writes;  only sync on close.
         *   0   ==> sync on every write
         *   +ve ==> sync every x seconds
         *   
         * Throws System.Exception e.g., on network disconnection for remote streams. Catch in Caller.
        */
        public IStream openValueDataStream<KeyType, ValType>(FqStreamID FQSID,
                                                        CallerInfo Ci,
                                                        LocationInfo Li,
                                                        StreamSecurityType type,
                                                        CompressionType ctype,
                                                        StreamOp op,
                                                        string mdserveraddress = null, 
                                                        int ChunkSizeForUpload = 4*1024*1024 , 
                                                        int ThreadPoolSize = 1, 
                                                        Logger log = null,
                                                        bool sideload = false,
                                                        int syncIntervalSec = -1)
            where KeyType : IKey, new()
            where ValType : IValue, new()
        {
            if (Li == null)
                Li = new LocationInfo("", "", SynchronizerType.None);
            return new MetaStream<KeyType, ValType>(FQSID, Ci, Li,
                                                    op, type, ctype, StreamDataType.Values, 
                                                    syncIntervalSec, mdserveraddress, 
                                                    ChunkSizeForUpload, ThreadPoolSize, 
                                                    log, sideload);
        }

        /* syncIntervalSec:
         *   -ve ==> don't sync on writes;  only sync on close.
         *   0   ==> sync on every write
         *   +ve ==> sync every x seconds
         *   
         * Throws System.Exception e.g., on network disconnection for remote streams. Catch in Caller.
        */
        public IStream openFileDataStream<KeyType>(FqStreamID FQSID, 
                                                CallerInfo Ci, 
                                                LocationInfo Li,
                                                StreamFactory.StreamSecurityType type,
                                                CompressionType ctype,
                                                StreamFactory.StreamOp op,
                                                string mdserveraddress = null, 
                                                int ChunkSizeForUpload = 4*1024*1024 , 
                                                int ThreadPoolSize = 1,
                                                Logger log = null,
                                                bool sideload = false,
                                                int syncIntervalSec = -1)
            where KeyType : IKey, new()
        {
            if (Li == null)
                Li = new LocationInfo("", "", SynchronizerType.None);
            return new MetaStream<KeyType, ByteValue>(FQSID, Ci, Li,
                                                      op, type, ctype, StreamDataType.Files, 
                                                      syncIntervalSec, mdserveraddress, 
                                                      ChunkSizeForUpload, ThreadPoolSize, 
                                                      log, sideload);
        }


        /* Delete all files in Dir */
        internal void Boom(string DirName)
        {
            try
            {
                if (DirName != null)
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(DirName);

                    // TODO: Don't remove .key
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write("Exception in wiping directory: "+e.Message);
            }
        }

        public bool deleteStream(FqStreamID streamId, CallerInfo Ci, string mdserveraddress = null)
        {
            Logger log = new Logger();
            string BaseDir = Path.GetFullPath((null != Ci.workingDir) ? Ci.workingDir : Directory.GetCurrentDirectory());
            string targetDir = BaseDir + "/" + streamId.ToString();
            
            // string address = ConfigurationManager.AppSettings.Get("MDServerAddress");
            // string address = "http://homelab-vm.cloudapp.net:23456/MetaDataServer/";
            
            string address = mdserveraddress;
            MetaDataService.IMetaDataService clnt = null;
            if (address != null)
            {
                BasicHttpBinding binding = new BasicHttpBinding();
                ChannelFactory<MetaDataService.IMetaDataService> factory = new ChannelFactory<MetaDataService.IMetaDataService>(binding, address);
                clnt = factory.CreateChannel();
            }

            Dictionary<int, MetaDataService.AccountInfo> accounts = null;
            string containerName; ISync synchronizerForDelete;
            if (address != null)
            {
                try
                {
                    MetaDataService.FQStreamID stream = new MetaDataService.FQStreamID();
                    stream.HomeId = streamId.HomeId;
                    stream.AppId = streamId.AppId;
                    stream.StreamId = streamId.StreamId;
                    accounts = clnt.GetAllAccounts(stream);

                    if (accounts == null)
                    {
                        // no stream of this name on the metadata server
                        return false;
                    }

                    // TODO: Authentication for this update call
                    clnt.RemoveAllInfo(stream);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception caught {0}", e);
                    return false;
                }


                foreach (MetaDataService.AccountInfo account in accounts.Values)
                {
                    Boom(targetDir + "/" + account.num);
                    containerName = streamId.ToString().Replace('/', '-').ToLower() + "-" + account.num;
                    synchronizerForDelete = CreateSyncForAccount(account, containerName, log);
                    synchronizerForDelete.Delete();
                }
                
                containerName = streamId.ToString().Replace('/', '-').ToLower();//account info for meta stream = that of seg 0. strange?
                synchronizerForDelete = CreateSyncForAccount(accounts.ElementAt(0).Value, containerName, log);
                synchronizerForDelete.Delete();
                
            }
            else
            {

                LocalMetaDataServer localMdServer = new LocalMetaDataServer(targetDir + "/" + MetaStream<StrKey, StrValue>.StreamMDFileName, log);
                localMdServer.LoadMetaDataServer();

                Dictionary<int, AccountInfo> tmp = localMdServer.GetAllAccounts(new FQStreamID(streamId.HomeId, streamId.AppId, streamId.StreamId));

                if (tmp != null)
                {
                    MetaDataService.AccountInfo ai=null;
                    MetaDataService.AccountInfo segment0Ai =null;
                    foreach (AccountInfo account in tmp.Values)
                    {
                        Boom(targetDir + "/" + account.num);
                        containerName = streamId.ToString().Replace('/', '-').ToLower() + "-" + account.num;
                        ai = new MetaDataService.AccountInfo();
                        ai.accountKey = account.accountKey;
                        ai.accountName = account.accountName;
                        ai.location = account.location;
                        ai.keyVersion = account.keyVersion;
                        ai.num = account.num;
                        synchronizerForDelete = CreateSyncForAccount(ai , containerName, log);
                        if (synchronizerForDelete != null) synchronizerForDelete.Delete();
                        if(segment0Ai==null)
                            segment0Ai = ai;
                    }
                    containerName = streamId.ToString().Replace('/', '-').ToLower();// TODO account info for meta stream = that of seg 0? something is wrong. 
                    synchronizerForDelete = CreateSyncForAccount(segment0Ai, containerName, log);
                    if (synchronizerForDelete != null) synchronizerForDelete.Delete();
                }
            }

            Boom(targetDir);
            return true;
        }


        private static ISync CreateSyncForAccount(MetaDataService.AccountInfo account, string containerName, Logger log, SynchronizeDirection synchronizeDirection = SynchronizeDirection.Upload)
        {
            // Create Synchronizer
            if (account.location == "None")
            {
                return null;
            }
            else
            {
                LocationInfo Li = new LocationInfo(account.accountName, account.accountKey, SyncFactory.GetSynchronizerType(account.location));
                ISync synchronizer = SyncFactory.Instance.CreateSynchronizer(Li, containerName, log, synchronizeDirection);
                return synchronizer;
            }
        }

        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static byte[] GetASCIIBytes(string input)
        {
            return Encoding.ASCII.GetBytes(input);
        }

        public static string GetString(byte[] bytes)
        {
            if (bytes != null)
            {
                char[] chars = new char[bytes.Length / sizeof(char)];
                System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
                return new string(chars);
            }
            else
            {
                return "";
            }
        }

        public static string GetASCIIString(byte[] input)
        {
            return ASCIIEncoding.ASCII.GetString(input);
        }
        
        public static string GetStringAlt(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static long NowUtc()
        {
            return DateTime.UtcNow.Ticks;
        }

        public static long HighResTick()
        {
            return Stopwatch.GetTimestamp(); 
        }
        
        public static string PrettyNowUtc()
        {
            DateTime Date = new DateTime(DateTime.UtcNow.Ticks);
            String ret = Date.ToString("ddMMMHH-mm-ss");
            return ret;
        }
    }
}
