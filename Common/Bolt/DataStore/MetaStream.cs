using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.ServiceModel;
using System.Configuration;

//using HomeOS.Hub.Common.MDServer;
using HomeOS.Hub.Common.Bolt.DataStoreCommon;
using System.Threading;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class MetaStream<KeyType, ValType> : IStream, IDisposable
        where KeyType : IKey, new()
        where ValType : IValue, new()
    {
        // Logging related
        protected Logger logger;

        // Ownership related and fixed fields
        // Naming
        protected FqStreamID streamid;
        protected MetaDataService.FQStreamID stream;
        protected string targetDir;
        protected string BaseDir;
        
        // Ownership
        protected MetaDataService.Principal caller;
        protected CallerInfo ci;
        protected MetaDataService.Principal owner;
        protected string OwnerPriKey;
        protected string OwnerPubKey;

        // Location
        protected ISync synchronizer;
        protected MetaDataService.AccountInfo indexMetaDataLocationInfo;
        
        // Security related
        protected MetaDataService.ACLEntry acl_md;
        protected IndexMetaData indexMetaData;
        
        // Low level helpers
        protected MetaDataService.IMetaDataService metadataClient;
        protected string mdserver;
        protected LocalMetaDataServer localMdServer;
        protected Dictionary<int, ValueDataStream<KeyType, ValType>> vstreams;
        protected Dictionary<int, FileDataStream<KeyType>> fstreams;
        
        // State related
        protected bool disposed;
        protected bool isClosed;
        protected StreamFactory.StreamOp streamop;
        protected StreamFactory.StreamSecurityType streamtype;
        protected StreamFactory.StreamDataType streamptype;
        protected CompressionType streamcompressiontype;
        protected bool sideload;

        protected int StreamChunkSizeForUpload ;
        protected int StreamThreadPoolSize ;

        public enum WriteOp : byte { AppendOp = 0, UpdateOp }
        //constants 
        public static readonly string IndexMDFileName = "index_md.dat";
        public static readonly string StreamMDFileName = "stream_md.dat";
        public static readonly string PriKeyFileName = ".key";
        public static readonly string  PubKeyFileName = ".pubkey";

        private static long IndexSizeThreshold = 1 * 1024 * 1024; // in bytes; index size is calculated in filestream.getIndexSize()

        protected DateTime lastSyncTime;
        protected int syncIntervalSec = -1;
        Thread syncWorker = null;
        private bool isSyncWorkerRunning = false; // flag to let the syncWorker Thread exit safely

        public void DumpLogs(string file)
        {
            if (logger != null)
                logger.Dump(file);
        }

        protected void CreateSync(SynchronizeDirection dir)
        {
            // Create Synchronizer
            if (indexMetaDataLocationInfo.location == "None")
            {
                synchronizer = null;
            }
            else
            {
                LocationInfo Li = new LocationInfo(indexMetaDataLocationInfo.accountName,indexMetaDataLocationInfo.accountKey, SyncFactory.GetSynchronizerType(indexMetaDataLocationInfo.location));
                synchronizer = SyncFactory.Instance.CreateSynchronizer(Li, streamid.ToString().Replace('/', '-').ToLower(), logger, dir);
                synchronizer.SetLocalSource(targetDir);
                synchronizer.SetIndexFileName(IndexMDFileName);
                synchronizer.SetDataFileName(StreamMDFileName);
            }
        }

        

        protected void Initialize(FqStreamID FQSID, CallerInfo Ci,
            StreamFactory.StreamOp op, StreamFactory.StreamSecurityType type,
            CompressionType ctype,
            StreamFactory.StreamDataType ptype,
            string mdserveraddress, int ChunkSizeForUpload, int ThreadPoolSize, Logger log)
        {
            // Logging related
            logger = log;
            if (logger != null) logger.Log("Start Stream Init DataStructures");
            
            // Naming related
            streamid = FQSID;
            stream = new MetaDataService.FQStreamID();
            stream.HomeId = FQSID.HomeId;
            stream.AppId = FQSID.AppId;
            stream.StreamId = FQSID.StreamId;
            BaseDir = Path.GetFullPath((null != Ci.workingDir) ? Ci.workingDir : Directory.GetCurrentDirectory());
            targetDir = BaseDir + "/" + streamid.ToString();
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
            
            // Ownership related
            ci = Ci;
            caller = new MetaDataService.Principal();
            caller.HomeId = streamid.HomeId;
            caller.AppId = Ci.appName;
            owner = new MetaDataService.Principal();
            owner.HomeId = streamid.HomeId;
            owner.AppId = streamid.AppId;

            // Location
            indexMetaDataLocationInfo = new MetaDataService.AccountInfo();

            // Security related
            indexMetaData = new IndexMetaData(targetDir, IndexMDFileName);

            // Low level
            // string address = ConfigurationManager.AppSettings.Get("MDServerAddress");
            // string address = "http://homelab-vm.cloudapp.net:23456/TrustedServer/";
            mdserver = mdserveraddress;
            if (mdserver != null)
            {
                // listed stream
                BasicHttpBinding binding = new BasicHttpBinding();
                ChannelFactory<MetaDataService.IMetaDataService> factory = new ChannelFactory<MetaDataService.IMetaDataService>(binding, mdserver);
                metadataClient = factory.CreateChannel();
                localMdServer = null;
                // clnt = new MetaDataService.MetaDataServiceClient();
            }
            else
            {
                // unlisted stream
                localMdServer = new LocalMetaDataServer(targetDir + "/" + StreamMDFileName, logger);
                localMdServer.LoadMetaDataServer();
                metadataClient = null;
            }

            if (ptype == StreamFactory.StreamDataType.Values)
            {
                vstreams = new Dictionary<int, ValueDataStream<KeyType, ValType>>();
            }
            else
            {
                fstreams = new Dictionary<int, FileDataStream<KeyType>>();
            }

            // State maint
            streamop = op;
            streamtype = type;
            streamptype = ptype;
            streamcompressiontype = ctype;
            StreamChunkSizeForUpload = ChunkSizeForUpload;
            StreamThreadPoolSize = ThreadPoolSize;
            if (logger != null) logger.Log("End Stream Init DataStructures");
        }

        protected bool IsCallerOwner()
        {
            if (caller == null || owner == null)
                return false;
            if (caller.HomeId == owner.HomeId && caller.AppId == owner.AppId)
                return true;
            return false;
        }

        protected string ReadKeyLocally(string HomeId, string AppId, string keytype)
        {
            string key = null;
            if (File.Exists(BaseDir + "/" + HomeId + "/" + AppId +  "/" + keytype))
            {
                try
                {
                    key = File.ReadAllText(BaseDir + "/" + streamid.DirName() + "/.key");
                    if (logger != null) logger.Log(String.Format("Got key locally for prin {0}", HomeId + "/" + AppId));
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} Exception caught.", e);
                    Console.WriteLine("Failed to locally get key for prin {0}", HomeId + "/" + AppId);
                }
            }
            return key;
        }

        protected string GetPubKey(string HomeId, string AppId)
        {

            string key = null;
            MetaDataService.Principal prin = new MetaDataService.Principal();
            prin.HomeId = HomeId;
            prin.AppId = AppId;
            try
            {
                if (metadataClient != null)
                {
                    key = metadataClient.GetPubKey(prin);
                }
                else
                {
                    key = ReadKeyLocally(HomeId, AppId, PubKeyFileName);
                }
               if (logger != null) logger.Log(String.Format("Got public key for prin {0}", HomeId + "/" + AppId));
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                Console.WriteLine("Failed to get public key for prin {0}", HomeId + "/" + AppId);
            }
            return key;
        }

        protected bool GenKeyPair()
        {
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            OwnerPriKey = RSA.ToXmlString(true);
            OwnerPubKey = RSA.ToXmlString(false);
            // Save public key
            try
            {
                if (metadataClient != null)
                {
                    metadataClient.RegisterPubKey(owner, OwnerPubKey);
                }
                else
                // Save public key locally for unlisted streams
                {
                    File.WriteAllText(BaseDir + "/" + streamid.DirName() + "/.pubkey", OwnerPubKey);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                Console.WriteLine("Failed to register public key for owner {0}", owner.HomeId + "/" + owner.AppId);
                return false;
            }
            // Save private key
            try
            {
                File.WriteAllText(BaseDir + "/" + streamid.DirName() + "/.key", OwnerPriKey);
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't write fresh private key for owner");
                if (logger != null) logger.Log(e.ToString());
                return false;
            }
            if (logger != null) logger.Log("Generated a new key-pair for owner");
            return true;
        }

        protected bool OpenPart(MetaDataService.AccountInfo ai)
        {
            if (logger != null) logger.Log("Start Segment Open");


            // if we do not have the indexinfo for this account, return
            if (ai.num > indexMetaData.index_infos.Count - 1)
                return false;

            IndexInfo ii = indexMetaData.index_infos[ai.num];
            LocationInfo li = new LocationInfo(ai.accountName, ai.accountKey, SyncFactory.GetSynchronizerType(ai.location));
            
            try
            {
                MetaDataService.ACLEntry segment_acl_md = new MetaDataService.ACLEntry(); ;
                if (streamtype == StreamFactory.StreamSecurityType.Secure)
                {
                    segment_acl_md = acl_md;
                    segment_acl_md.encKey = Crypto.GetSpecificKey(acl_md.encKey, acl_md.keyVersion, ai.keyVersion);
                    segment_acl_md.keyVersion = ai.keyVersion;
                }
                if (streamptype == StreamFactory.StreamDataType.Values)
                {
                    ValueDataStream<KeyType, ValType> stream = new ValueDataStream<KeyType, ValType>(logger, streamid, ai.num, ci, li, streamop, streamtype, streamcompressiontype, this.StreamChunkSizeForUpload, this.StreamThreadPoolSize, OwnerPriKey, OwnerPubKey, segment_acl_md, ii, alreadyExists: true);
                    vstreams[ai.num] = stream;
                }
                else
                {
                    FileDataStream<KeyType> dstream = new FileDataStream<KeyType>(logger, streamid, ai.num, ci, li, streamop, streamtype, streamcompressiontype, this.StreamChunkSizeForUpload, this.StreamThreadPoolSize, OwnerPriKey, OwnerPubKey, segment_acl_md, ii, alreadyExists: true);
                    fstreams[ai.num] = dstream;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in OpenPart: "+e);
                if (logger != null) logger.Log(e.ToString());
                return false;
            }
            if (logger != null) logger.Log("End Segment Open");
            
            return true;
        }

        protected bool CreatePart(int num, CallerInfo Ci, LocationInfo Li)
        {
            if (logger != null) logger.Log("Start Segment Create");
            try
            {
                if (streamptype == StreamFactory.StreamDataType.Values)
                {
                    ValueDataStream<KeyType, ValType> stream =
                        new ValueDataStream<KeyType, ValType>(logger, streamid, num, Ci, Li,
                                                            streamop, streamtype, streamcompressiontype, this.StreamChunkSizeForUpload, this.StreamThreadPoolSize, 
                                                            OwnerPriKey, OwnerPubKey, acl_md, 
                                                            null, alreadyExists:false);
                    vstreams[num] = stream;
                }
                else
                {
                    FileDataStream<KeyType> dstream =
                        new FileDataStream<KeyType>(logger, streamid, num, Ci, Li,
                                                            streamop, streamtype, streamcompressiontype, this.StreamChunkSizeForUpload, this.StreamThreadPoolSize,
                                                            OwnerPriKey, OwnerPubKey, acl_md,
                                                            null, alreadyExists:false);
                    fstreams[num] = dstream;
                }
            }
            catch (Exception e)
            {
                if (logger != null) logger.Log(e.ToString());
                return false;
            }

            // Store location info on metadata server
            if (logger != null) logger.Log("Start Segment Store Location Info");
            if (StoreSegmentLocationInfo(num, Li) == false)
            {
                return false;
            }
            if (logger != null) logger.Log("End Segment Store Location Info");
            if (logger != null) logger.Log("End Segment Create");
            return true;
        }

        protected MetaDataService.AccountInfo AlreadyExists()
        {
            return GetMdIndexLocationInfo();
        }

        protected void InitACLMd()
        {
            acl_md = new MetaDataService.ACLEntry();
            acl_md.readerName = owner;
            Crypto.GenFreshKeychain(targetDir);
            acl_md.encKey = Crypto.GetNewKey(targetDir);
            acl_md.IV = Crypto.GenFreshAESIV();
            acl_md.keyVersion = Crypto.GetKeyVersion(targetDir);
        }
            
        protected bool CreateStream(CallerInfo Ci, LocationInfo Li,
                                StreamFactory.StreamSecurityType type,
                                StreamFactory.StreamDataType ptype)
        {
            // Store index_md location info on metadata server
            if (logger != null) logger.Log("Start Stream Create Stream");
            if (logger != null) logger.Log("Start Stream Store IndexMD Location");
            if (!StoreMDIndexLocationInfo(Li))
            {
                return false;
            }
            
            // cache location info for index_metadata locally
            indexMetaDataLocationInfo = new MetaDataService.AccountInfo();
            indexMetaDataLocationInfo.accountKey = Li.accountKey;
            indexMetaDataLocationInfo.accountName = Li.accountName;
            indexMetaDataLocationInfo.location = Li.st + "";
            
            if (logger != null) logger.Log("End Stream Store IndexMD Location");
            
            if (type == StreamFactory.StreamSecurityType.Secure)
            {
                // Must have public and private key for owner for creating
                if (logger != null) logger.Log("Start Stream Generate Pub-Pri Key Pair");
                if ((OwnerPriKey = ReadKeyLocally(owner.HomeId, owner.AppId, PriKeyFileName)) == null)
                {
                    if (GenKeyPair() == false)
                        throw new InvalidDataException("Couldn't get keys for meta stream owner");
                }
                if (logger != null) logger.Log("End Stream Generate Pub-Pri Key Pair");

                // Generate content keys
                if (logger != null) logger.Log("Start Stream Generate Key Regresion Chain");
                InitACLMd();
                if (logger != null) logger.Log("End Stream Generate Key Regresion Chain");

                // Store content key on metadata server
                if (logger != null) logger.Log("Start Stream Store Content Key");
                if (PutContentKey(owner.HomeId, owner.AppId) == false)
                {
                    return false;
                }
                if (logger != null) logger.Log("End Stream Store Content Key");
            }

            bool ret = CreatePart(0, Ci, Li);
            if (logger != null) logger.Log("End Stream Create Stream");
            return ret;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Seal(bool checkMemPressure = false)
        {
            // Cannot seal if closed or not writing
            if (isClosed == true || streamop != StreamFactory.StreamOp.Write)
                return;

            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                int last = vstreams.Values.Count - 1;
                long currentStreamIndexSize = vstreams[last].GetIndexSize();
                if (checkMemPressure && currentStreamIndexSize < IndexSizeThreshold)
                    return;
                
                string accountName = vstreams[last].GetAccountInfo().accountName;
                string accountKey = vstreams[last].GetAccountInfo().accountKey;
                string accountLocation = vstreams[last].GetAccountInfo().location;

                Console.WriteLine("Creating new segment");

                vstreams[last].Seal(false);
                CreatePart(last + 1, ci, new LocationInfo(accountName, accountKey, SyncFactory.GetSynchronizerType(accountLocation)));
                indexMetaData.index_infos[last + 1] = vstreams[last + 1].GetIndexInfo();
            }
            else
            {
                int last = fstreams.Values.Count - 1;
                long currentStreamIndexSize = fstreams[last].GetIndexSize();
                if (checkMemPressure && currentStreamIndexSize < IndexSizeThreshold)
                    return;
                
                string accountName = fstreams[last].GetAccountInfo().accountName;
                string accountKey = fstreams[last].GetAccountInfo().accountKey;
                string accountLocation = fstreams[last].GetAccountInfo().location;

                fstreams[last].Seal(false);
                CreatePart(last + 1, ci, new LocationInfo(accountName, accountKey, SyncFactory.GetSynchronizerType(accountLocation)));
                indexMetaData.index_infos[last + 1] = fstreams[last + 1].GetIndexInfo();
            }
            GC.Collect();
        }
        
        public MetaStream(FqStreamID FQSID, CallerInfo Ci, LocationInfo Li,
            StreamFactory.StreamOp op, StreamFactory.StreamSecurityType type,
            CompressionType ctype,
            StreamFactory.StreamDataType ptype, int sync_interval_sec,
            string mdserveraddress, int ChunkSizeForUpload, int UploadThreadPoolSize, Logger log,
            bool sideLoad)
        {
            bool justCreated = false;
            Initialize(FQSID, Ci, op, type, ctype, ptype, mdserveraddress, ChunkSizeForUpload, UploadThreadPoolSize, log);
            this.sideload = sideLoad;
            
            if (op == StreamFactory.StreamOp.Write && !IsCallerOwner())
                throw new InvalidDataException("Cannot write to meta stream with other's name!");

            if (logger != null) logger.Log("Start Stream Check Exists, Fill IndexMD Location");
            this.indexMetaDataLocationInfo = AlreadyExists();
            if (logger != null) logger.Log("End Stream Check Exists, Fill IndexMD Location");

            /* this.indexMetaDataLocationInfo == null
             * implies that this is a stream that we do not know about
             * A) for writers, this means that it is a NEW stream - create it
             * b) for readers and sideload is enabled (only readers can sideload) - LocationInfo is provided - go fetch stream from there
             */

            if ( streamop==StreamFactory.StreamOp.Read && (this.indexMetaDataLocationInfo == null) && sideload)
            {
                this.indexMetaDataLocationInfo = new MetaDataService.AccountInfo();
                this.indexMetaDataLocationInfo.accountName = Li.accountName;
                this.indexMetaDataLocationInfo.accountKey = Li.accountKey;
                this.indexMetaDataLocationInfo.location = Li.st + "";
                //this.indexMetaDataLocationInfo.num = ai.num;
            }
            else if ((this.indexMetaDataLocationInfo == null))
            {
                // stream does not exist.  create new stream.
                if (op == StreamFactory.StreamOp.Read)
                    throw new InvalidDataException("Stream does not exist!");
                if (CreateStream(Ci, Li, type, ptype) == false)
                    throw new InvalidDataException("Couldn't create stream!");
                justCreated = true;
            }

        

            LoadStream(Li, justCreated);

            syncIntervalSec = sync_interval_sec;
            if (syncIntervalSec > 0)
            {
                lastSyncTime = DateTime.Now;
                syncWorker = new Thread(delegate()
                {
                    try
                    {
                        while (!disposed)
                        {
                            DateTime now = DateTime.Now;
                            if (now.Subtract(lastSyncTime).TotalSeconds > syncIntervalSec)
                            {
                                Console.WriteLine("[" + now + "] periodic-sync-worker for {0}: syncing ...", FQSID.ToString());
                                Flush();
                                lastSyncTime = DateTime.Now;
                                Console.WriteLine("[" + lastSyncTime + "] periodic-sync-worker for {0}: syncing done", FQSID.ToString());
                            }

                            if (isSyncWorkerRunning)
                                System.Threading.Thread.Sleep(syncIntervalSec * 1000);
                            else
                                break;
                        }
                    }
                   catch (Exception exception)
                    {
                        string message = "periodic-sync-worker raised exception: " + exception.ToString();
                        //if (logger != null) logger.Log(message);
                        Console.Error.WriteLine(message);
                    }
                }
                );
                isSyncWorkerRunning = true;
                syncWorker.Start();
                
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void LoadStream(LocationInfo Li, bool justCreated)
        {
            // Download index metadata if reqd.
            if (logger != null) logger.Log("Start Stream Download IndexMD");
            CreateSync(SynchronizeDirection.Download);
            bool syncSuccess=true;
            Dictionary<int, MetaDataService.AccountInfo> accounts;
            if (!justCreated)
            {   // for prexisting streams, if local, Sync() returns true (always), if remote, sync() return true or false(if disconnected)
                syncSuccess = Sync();
                if (!syncSuccess)
                    throw new Exception("MetaStream Sync Failed.");
            }

            if (logger != null) logger.Log("End Stream Download IndexMD");

            // Restore synchronizer to upload if writing
            if (streamop == StreamFactory.StreamOp.Write)
            {
                CreateSync(SynchronizeDirection.Upload);
            }

            if (streamtype == StreamFactory.StreamSecurityType.Secure)
            {
                // Have private key if owner is the caller
                if (logger != null) logger.Log("Start Stream Get Pub-Pri Keys");
                if (!justCreated && IsCallerOwner() 
                    && (OwnerPriKey = ReadKeyLocally(owner.HomeId, owner.AppId, PriKeyFileName)) == null)
                {
                    throw new InvalidDataException("Couldn't get private key for owner");
                }

                // Get public key for owner of stream
                if ((OwnerPubKey = GetPubKey(owner.HomeId, owner.AppId)) == null)
                {
                    throw new InvalidDataException("Couldn't get public key for owner");
                }
                if (logger != null) logger.Log("End Stream Get Pub-Pri Keys");

                // Fetch content key form metadata server
                if (logger != null) logger.Log("Start Stream Get Content Key");
                if (!justCreated && GetContentKey() == false)
                {
                    throw new InvalidDataException("Couldn't get content key for meta stream");
                }
                if (logger != null) logger.Log("End Stream Get Content Key");

                // Verify freshness and integrity of the index metadata
                if (logger != null) logger.Log("Start Stream Verify, Fill IndexMD");
                if (!justCreated && indexMetaData.VerifyMetadata(OwnerPubKey) == false)
                {
                    throw new InvalidDataException("Couldn't verify metadata for meta stream");
                }
                if (logger != null) logger.Log("End Stream Verify, Fill IndexMD");
            }
            else
            {
                if (logger != null) logger.Log("Start Stream Verify, Fill IndexMD");
                if (!justCreated && indexMetaData.LoadIndexMetaData() == false)
                    throw new InvalidDataException("Couldn't load metadata");
                if (logger != null) logger.Log("End Stream Verify, Fill IndexMD");
            }

            // Populate individual indices
            if (!justCreated)
            {
                // Fetch accounts info for the individual indices
                if (logger != null) logger.Log("Start Stream Get Segment Location Infos");

                if (mdserver == null) // if this is not a remote-listed stream then re-load localmetadataserver
                {
                    localMdServer.LoadMetaDataServer();
                }

                accounts = GetSegmentsLocationInfo();

                if ((accounts==null && streamop==StreamFactory.StreamOp.Write) || (accounts==null &&  streamop==StreamFactory.StreamOp.Read && !sideload))
                {
                    throw new InvalidDataException("Couldn't get segment LI's for meta stream");
                }
                else if (sideload && streamop==StreamFactory.StreamOp.Read)
                {
                    accounts = SideloadSegmentsLocationInfo(Li, indexMetaData.index_infos.Count());
                }
                

                if (logger != null) logger.Log("End Stream Get Segment Location Infos");
                
                foreach (MetaDataService.AccountInfo ai in accounts.Values)
                {
                    /*
                     * Writer's sync path
                     * 1. syncs all segments. For each segment:
                     *      a. Upload stream.dat 
                     *      b. ChunkMD-stream.dat
                     *      c. Upload index.dat
                     * 2. syncs stream_md.dat (for unlisted streams)
                     * 3. sync index_md.dat
                     * 4. update md server (listed streams, currently being done in createpart by calling StoreMDIndexLocationInfo, and by calling StoreSegmentLocationInfo)
                     * TODO: merge StoreSegmentLocationInfo and StoreMDIndexLocationInfo
                     * 
                     * Because 4. is being done in createpart (i.e. writer updates accountinfo at md_server before uploading the segment) , reader might see more account infos than indexinfos (read from index_md.dat) 
                     * So, open part checks if we have the indexinfo for a given accountinfo, else it returns
                     */
                    OpenPart(ai);
                }
            }

            isClosed = false;
            disposed = false;
        }

        protected Dictionary<int, MetaDataService.AccountInfo> SideloadSegmentsLocationInfo(LocationInfo li, int numSegments)
        {
            Dictionary<int, MetaDataService.AccountInfo> accounts = new Dictionary<int, MetaDataService.AccountInfo>();
            for (int i = 0; i < numSegments; i++ )
            {
                MetaDataService.AccountInfo ai = new MetaDataService.AccountInfo();
                ai.accountKey = li.accountKey;
                ai.accountName = li.accountName;
                ai.location = li.st + "";
                ai.keyVersion = 0;
                ai.num = i;
                accounts[i] = ai;
            }
            return accounts;
        }

        protected Dictionary<int, MetaDataService.AccountInfo> GetSegmentsLocationInfo()
        {
            Dictionary<int, MetaDataService.AccountInfo> accounts;

            if (metadataClient == null)
            {
                Dictionary<int, AccountInfo> tmp = localMdServer.GetAllAccounts(new FQStreamID(stream.HomeId, stream.AppId, stream.StreamId));
                if (tmp == null)
                    return null;
                else
                {
                    accounts = new Dictionary<int, MetaDataService.AccountInfo>();
                    foreach (KeyValuePair<int, AccountInfo> kv in tmp)
                    {
                        MetaDataService.AccountInfo ai = new MetaDataService.AccountInfo();
                        ai.accountKey = kv.Value.accountKey;
                        ai.accountName = kv.Value.accountName;
                        ai.location = kv.Value.location;
                        ai.keyVersion = kv.Value.keyVersion;
                        ai.num = kv.Value.num;
                        accounts[kv.Key] = ai;
                    }
                }
                return accounts;
            }

            try
            {
                accounts = metadataClient.GetAllAccounts(stream);
                if (accounts == null)
                {
                    if (logger != null) logger.Log("Got null remote info for meta stream  " + streamid.ToString());
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get remote info for meta stream {0} ", streamid.ToString());
                Console.WriteLine("{0} Exception caught.", e);
                return null;
            }
            return accounts;
        }

        protected bool StoreSegmentLocationInfo(int num, LocationInfo Li)
        {
            uint key_version = 0;
            if (this.streamtype == StreamFactory.StreamSecurityType.Secure)
            {
                key_version = acl_md.keyVersion;
            }

            if (metadataClient == null)
            {
                AccountInfo account = new AccountInfo(Li.accountName, Li.accountKey, "" + Li.st, key_version);
                account.num = num;
                return localMdServer.AddAccount(new FQStreamID(stream.HomeId, stream.AppId, stream.StreamId), account);
            }
            
            try
            {
                MetaDataService.AccountInfo account = new MetaDataService.AccountInfo();
                account.num = num;
                account.accountName = Li.accountName;
                account.accountKey = Li.accountKey;
                account.location = "" + Li.st;
                account.keyVersion = key_version;
                if (metadataClient.AddAccount(stream, account) == true)
                {
                    // if (logger != null) logger.Log(string.Format("Put account info for meta stream {0} location {1}", streamid.ToString(), account.location));
                }
                else
                {
                    // if (logger != null) logger.Log(string.Format("Failed to put account info for meta stream {0} location {1}", streamid.ToString(), account.location));
                    return false;
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("Exception caught." +  exp);
                return false;
            }
            return true;
        }
        
        protected MetaDataService.AccountInfo GetMdIndexLocationInfo()
        {
            MetaDataService.AccountInfo tmp = new MetaDataService.AccountInfo();

            if (metadataClient == null)
            {
                AccountInfo ai = localMdServer.GetMdAccount(new FQStreamID(stream.HomeId, stream.AppId, stream.StreamId));
                if (ai == null)
                    return null;
                tmp.accountName = ai.accountName;
                tmp.accountKey = ai.accountKey;
                tmp.location = ai.location;
                tmp.num = ai.num;
                return tmp;
            }

            try
            {
                tmp = metadataClient.GetMdAccount(stream);
                if (tmp == null)
                {
                    if (logger != null) logger.Log("Got null indexMetaData account info for meta stream  " + streamid.ToString());
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get indexMetaData account info for meta stream {0} ", streamid.ToString());
                Console.WriteLine("{0} Exception caught.", e);
                return null;
            }
            return tmp;
        }
        
        protected bool StoreMDIndexLocationInfo(LocationInfo Li)
        {
            if (metadataClient == null)
            {
                AccountInfo ai = new AccountInfo(Li.accountName, Li.accountKey, Li.st + "", 1);
                return localMdServer.AddMdAccount(new FQStreamID(stream.HomeId, stream.AppId, stream.StreamId), ai);
            }
            
            try
            {
                MetaDataService.AccountInfo acc_info = new MetaDataService.AccountInfo();
                acc_info.accountKey = Li.accountKey;
                acc_info.accountName = Li.accountName;
                acc_info.location = Li.st + "";
                return metadataClient.AddMdAccount(stream, acc_info);
            }
            catch (Exception exp)
            {
                Console.WriteLine("Exception caught." +  exp);
                return false;
            }
        }

        protected bool GetContentKey()
        {
            try
            {
                if (metadataClient != null)
                {
                    acl_md = metadataClient.GetReaderKey(stream, caller);
                }
                else
                {
                    // Content key is stored locally for unlisted stream
                    ACLEntry tmp = localMdServer.GetReaderKey(new FQStreamID(streamid.HomeId, streamid.AppId, streamid.StreamId),
                                                        new Principal(caller.HomeId, caller.AppId));

                    if (tmp != null)
                    {
                        acl_md = new MetaDataService.ACLEntry();
                        acl_md.encKey = tmp.encKey;
                        acl_md.IV = tmp.IV;
                        acl_md.keyVersion = tmp.keyVersion;
                        acl_md.readerName = new MetaDataService.Principal();
                        acl_md.readerName.HomeId = tmp.readerName.HomeId;
                        acl_md.readerName.AppId = tmp.readerName.AppId;
                    }
                    else
                    {
                        acl_md = null;
                    }
                }
                if (acl_md == null)
                {
                    // if (logger != null) logger.Log("Got null content key for meta stream  " + streamid.ToString());
                    return false;
                }

                // Decrypt key
                string key = ReadKeyLocally(caller.HomeId, caller.AppId, PriKeyFileName);
                if (key == null)
                    return false;
                else
                {
                    RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                    RSA.FromXmlString(key);
                    acl_md.encKey = Crypto.RSADecrypt(acl_md.encKey, RSA.ExportParameters(true), false);
                }
                if (acl_md.encKey == null)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get content key for meta stream {0} ", streamid.ToString());
                Console.WriteLine("{0} Exception caught.", e);
                return false;
            }
            return true;
        }

        protected bool PutContentKey(string HomeId, string AppId)
        {
            MetaDataService.Principal prin = new MetaDataService.Principal();
            prin.HomeId = HomeId;
            prin.AppId = AppId;
            
            MetaDataService.ACLEntry t = new MetaDataService.ACLEntry();
            t.readerName = prin;

            // Encrypt key
            // t.encKey = acl_md.encKey;
            string key = GetPubKey(HomeId, AppId);
            if (key == null)
                return false;
            else
            {
                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                RSA.FromXmlString(key);
                t.encKey = Crypto.RSAEncrypt(acl_md.encKey, RSA.ExportParameters(false), false);
            }

            if (t.encKey == null)
            {
                return false;
            }

            t.IV = acl_md.IV;
            t.keyVersion = acl_md.keyVersion;

            Byte[] data = {};
            data = data.Concat(StreamFactory.GetBytes(caller.HomeId)).ToArray();
            data = data.Concat(StreamFactory.GetBytes(caller.AppId)).ToArray();
            data = data.Concat(StreamFactory.GetBytes(stream.HomeId)).ToArray();
            data = data.Concat(StreamFactory.GetBytes(stream.AppId)).ToArray();
            data = data.Concat(StreamFactory.GetBytes(stream.StreamId)).ToArray();
            data = data.Concat(StreamFactory.GetBytes(t.readerName.HomeId)).ToArray();
            data = data.Concat(StreamFactory.GetBytes(t.readerName.AppId)).ToArray();
            data = data.Concat(t.encKey).ToArray();
            data = data.Concat(t.IV).ToArray();
            data = data.Concat(StreamFactory.GetBytes("" + t.keyVersion)).ToArray();

            RSACryptoServiceProvider RSA2 = new RSACryptoServiceProvider();
            RSA2.FromXmlString(OwnerPriKey);
            caller.Auth = RSA2.SignData(data, new SHA256CryptoServiceProvider());
            
            try
            {
                if (metadataClient != null)
                {
                    if (metadataClient.UpdateReaderKey(caller, stream, t) == true)
                    {
                        if (logger != null) logger.Log(string.Format("Put content key version {2} for meta stream {0} principal {1}", streamid.ToString(), prin.HomeId + "/" + prin.AppId, t.keyVersion));
                    }
                    else
                    {
                        if (logger != null) logger.Log(string.Format("Failed to put content key version {2} for meta stream {0} principal {1}", streamid.ToString(), prin.HomeId + "/" + prin.AppId, t.keyVersion));
                        return false;
                    }
                }
                else
                {
                    if (localMdServer.UpdateReaderKey(new Principal(caller.HomeId, caller.AppId),
                               new FQStreamID(streamid.HomeId, streamid.AppId, streamid.StreamId),
                               new ACLEntry(new Principal(HomeId, AppId), t.encKey, t.IV, t.keyVersion)) != true)
                    {
                        return false;
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("Failed to put content key for meta stream {0} principal {1}", streamid.ToString(), owner.HomeId + "/" + owner.AppId);
                Console.WriteLine("{0} Exception caught.", exp);
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool GrantReadAccess(string AppId)
        {
            return GrantReadAccess(caller.HomeId, AppId);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool GrantReadAccess(string HomeId, string AppId)
        {
            if (logger != null) logger.Log("Start Stream GrantReadAccess to " + HomeId + "/" + AppId);
            // Only an owner can change ACL and it is meaningful only for secure streams
            if (!IsCallerOwner() || this.streamtype == StreamFactory.StreamSecurityType.Plain)
                return false;
            
            // Fetch content key
            if (GetContentKey() == false)
            {
                return false;
            }

            bool ret = PutContentKey(HomeId, AppId);
            if (logger != null) logger.Log("End Stream GrantReadAccess to " + HomeId + "/" + AppId);
            return ret;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool RevokeReadAccess(string AppId)
        {
            return RevokeReadAccess(caller.HomeId, AppId);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool RevokeReadAccess(string HomeId, string AppId)
        {
            if (logger != null) logger.Log("Start Stream RevokeReadAccess to " + HomeId + "/" + AppId);
            // Only an owner can change ACL and it is meaningful only for secure streams
            if (!IsCallerOwner() || this.streamtype == StreamFactory.StreamSecurityType.Plain)
                return false;
            
            // Fetch content key
            if (GetContentKey() == false)
            {
                return false;
            }
            
            // Create entry for stream on metadata server
            // Generate and store content key
            acl_md.readerName = owner;
            if (logger != null) logger.Log("Start Stream Roll Forward Key");
            acl_md.encKey = Crypto.GetNewKey(targetDir);
            acl_md.keyVersion = acl_md.keyVersion + 1;
            if (logger != null) logger.Log("End Stream Roll Forward key");

            // Seal the old segment
            Seal();

            // Upload encryption keys for the new segment
            if (PutContentKey(owner.HomeId, owner.AppId) == false)
            {
                return false;
            }
            
            // Grant access to the new segment to all but revoked reader
            try
            {
                foreach (MetaDataService.Principal prin in metadataClient.GetAllReaders(stream))
                {
                    if (!(prin.HomeId == HomeId && prin.AppId == AppId))
                    {
                        GrantReadAccess(prin.HomeId, prin.AppId);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
            }
            if (logger != null) logger.Log("End Stream RevokeReadAccess to " + HomeId + "/" + AppId);
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IValue Get(IKey key)
        {
            return Get(key, false);
        }
        
        protected IValue Get(IKey key, bool ignoreIntegrity = false)
        {
            if (logger != null) logger.Log("Start Stream Get");
            IValue ret = null;
            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                for (int i = vstreams.Keys.Count - 1; i >= 0; i--)// iterate backwards over streams to find the latest val for the given key
                {
                    ret = vstreams[i].Get(key); 
                    if (ret != null)// if found 
                        break;
                }
            }
            else if (streamptype == StreamFactory.StreamDataType.Files)
            {
                for (int i = fstreams.Keys.Count - 1; i >= 0; i--)// iterate backwards over dstreams to find the latest val for the given key
                {
                    ret = fstreams[i].Get(key);
                    if (ret != null)// if found 
                        break;
                }
                
            }
            if (logger != null) logger.Log("End Stream Get");
            return ret;
        }

        /*
        * return null if key not found
        */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<IDataItem> GetAll(IKey key)
        {
            IEnumerable<IDataItem> retVal = null;

            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                foreach (int streamNum in vstreams.Keys)
                {
                    IEnumerable<IDataItem> temp = vstreams[streamNum].GetAll(key);
                    if(temp!=null)
                        retVal = retVal == null ? temp : retVal.Concat(temp);
                }
            }
            else if (streamptype == StreamFactory.StreamDataType.Files)
            {
                foreach (int streamNum in fstreams.Keys)
                {
                    IEnumerable<IDataItem> temp = fstreams[streamNum].GetAll(key);
                    if (temp != null)
                        retVal = retVal == null ? temp : retVal.Concat(temp);
                }
            }

            return retVal;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<IDataItem> GetAll(IKey key, long startTimeStamp, long endTimeStamp)
        {
            if (logger != null) logger.Log("Start Stream GetAll");
            IEnumerable<IDataItem> retVal = null;

            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                foreach (int streamNum in vstreams.Keys)
                {
                    IEnumerable<IDataItem> temp = vstreams[streamNum].GetAll(key, startTimeStamp, endTimeStamp);
                    if ((temp != null) && (temp.Count() > 0))
                    {
                        retVal = retVal == null ? temp : retVal.Concat(temp);
                    }
                }
            }
            else if (streamptype == StreamFactory.StreamDataType.Files)
            {
                foreach (int streamNum in fstreams.Keys)
                {
                    IEnumerable<IDataItem> temp = fstreams[streamNum].GetAll(key, startTimeStamp, endTimeStamp);
                    if ((temp != null) && (temp.Count() > 0))
                    {
                        retVal = retVal == null ? temp : retVal.Concat(temp);
                    }
                }
            }

            if (logger != null) logger.Log("End Stream GetAll");
            return retVal;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<IDataItem> GetAllWithSkip(IKey key, long startTimeStamp, long endTimeStamp, long skip)
        {
            IEnumerable<IDataItem> retVal = null;
            if (logger != null) logger.Log("Start Stream GetWithSkip");
            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                long startTimeStampInSegment = startTimeStamp; 
                foreach (int streamNum in vstreams.Keys)
                {
                    IEnumerable<IDataItem> temp = vstreams[streamNum].GetAllWithSkip(key, startTimeStampInSegment, endTimeStamp, skip);
                    if (temp != null && temp.Count() > 0)
                    {
                        retVal = retVal == null ? temp : retVal.Concat(temp);
                        startTimeStampInSegment = temp.Last().GetTimestamp() + skip;
                    }
                }
            }
            else if (streamptype == StreamFactory.StreamDataType.Files)
            {
                long startTimeStampInSegment = startTimeStamp;
                foreach (int streamNum in vstreams.Keys)
                {
                    IEnumerable<IDataItem> temp = vstreams[streamNum].GetAllWithSkip(key, startTimeStampInSegment, endTimeStamp, skip);
                    if (temp != null && temp.Count() > 0)
                    {
                        retVal = retVal == null ? temp : retVal.Concat(temp);
                        startTimeStampInSegment = temp.Last().GetTimestamp() + skip;
                    }
                }
            }


            if (logger != null) logger.Log("End Stream GetWithSkip");
            return retVal;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public HashSet<IKey> GetKeys(IKey startKey, IKey endKey)
        {
            HashSet<IKey> retVal = null;

            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                foreach (int streamNum in vstreams.Keys)
                {
                    HashSet<IKey> temp = vstreams[streamNum].GetKeys(startKey, endKey);
                    if (temp != null && temp.Count() > 0)
                    {
                        if (retVal == null)
                        {
                            retVal = temp;
                        }
                        else
                        {
                            retVal.UnionWith(temp);
                        }
                    }
                }
            }
            else if (streamptype == StreamFactory.StreamDataType.Files)
            {
                foreach (int streamNum in fstreams.Keys)
                {
                    HashSet<IKey> temp = fstreams[streamNum].GetKeys(startKey, endKey);
                    if (temp != null && temp.Count() > 0)
                    {
                        if (retVal == null)
                        {
                            retVal = temp;
                        }
                        else
                        {
                            retVal.UnionWith(temp);
                        }
                    }
                }
            }

            return retVal;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Tuple<IKey, IValue> GetLatest()
        {
            Tuple<IKey,IValue> retVal = null;
            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                for (int i = vstreams.Keys.Count - 1; i >= 0; i--)// iterate backwards over streams to find the latest val 
                {
                    retVal = vstreams[i].GetLatest();
                    if (retVal != null)// if found 
                        break;
                }
            }
            else if (streamptype == StreamFactory.StreamDataType.Files)
            {
                for (int i = fstreams.Keys.Count - 1; i >= 0; i--)// iterate backwards over dstreams to find the latest val
                {
                    retVal = fstreams[i].GetLatest();
                    if (retVal != null)// if found 
                        break;
                }
            }

            return retVal;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Tuple<IValue, long> GetLatest(IKey tag)
        {
            Tuple<IValue, long> retVal = null;
            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                for (int i = vstreams.Keys.Count - 1; i >= 0; i--)// iterate backwards over streams to find the latest val 
                {
                    retVal = vstreams[i].GetLatest(tag);
                    if (retVal != null)// if found 
                        break;
                }
            }
            else if (streamptype == StreamFactory.StreamDataType.Files)
            {
                for (int i = fstreams.Keys.Count - 1; i >= 0; i--)// iterate backwards over dstreams to find the latest val
                {
                    retVal = fstreams[i].GetLatest(tag);
                    if (retVal != null)// if found 
                        break;
                }
            }

            return retVal;
        }
        
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void UpdateHelper()
        {
            if (streamop != StreamFactory.StreamOp.Write)
                throw new InvalidDataException("Cannot write to a stream not opened for write");
        }
            
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Append(IKey key, IValue value, long timestamp = -1 )
        {
            if (logger != null) logger.Log("Start Stream Append");
            UpdateHelper();
            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                vstreams[vstreams.Count - 1].Append(key, value, timestamp);
            }
            else
            {
                fstreams[fstreams.Count - 1].Append(key, value, timestamp);
            }
            Seal(checkMemPressure:true);
            if (syncIntervalSec == 0) Flush();
            if (logger != null) logger.Log("End Stream Append");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Append(List<Tuple<IKey,IValue>> list)
        {
            if (logger != null) logger.Log("Start Stream Append");
            UpdateHelper();
            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                vstreams[vstreams.Count - 1].Append(list);
            }
            else
            {
                fstreams[fstreams.Count - 1].Append(list);
            }
            Seal(checkMemPressure: true);
            if (syncIntervalSec == 0) Flush();
            if (logger != null) logger.Log("End Stream Append");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Append(List<IKey> listOfKeys, IValue value)
        {
            if (logger != null) logger.Log("Start Stream Append");
            UpdateHelper();
            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                vstreams[vstreams.Count - 1].Append(listOfKeys, value);
            }
            else
            {
                fstreams[fstreams.Count - 1].Append(listOfKeys, value);
            }
            Seal(checkMemPressure: true);
            if (syncIntervalSec == 0) Flush();
            if (logger != null) logger.Log("End Stream Append");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Update(IKey key, IValue value)
        {
            UpdateHelper();
            if (logger != null) logger.Log("Start Stream Update");
            if (streamptype == StreamFactory.StreamDataType.Values)
            {
                vstreams[vstreams.Count - 1].Update(key, value);
            }
            else
            {
                fstreams[fstreams.Count - 1].Update(key, value);
            }
            Seal(checkMemPressure:true);
            if (syncIntervalSec == 0) Flush();
            if (logger != null) logger.Log("End Stream Update");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void Flush()
        {
            if (!isClosed && streamop == StreamFactory.StreamOp.Write)
            {
                Close(true);
                Reopen();
            }
            else if (!isClosed && streamop == StreamFactory.StreamOp.Read)
            {
                Close(false);
                try
                {
                    LoadStream(new LocationInfo(this.indexMetaDataLocationInfo.accountName,
                                                this.indexMetaDataLocationInfo.accountKey,
                                                SyncFactory.GetSynchronizerType(indexMetaDataLocationInfo.location)),
                               false);
                }
                catch(Exception e)
                {
                    if(logger!=null) logger.Log(e.StackTrace);
                }

            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void Reopen()
        {
            if (logger != null) logger.Log("Start Stream Reopen");
            if (isClosed)
            {
                if (streamop == StreamFactory.StreamOp.Write)
                {
                    if (streamptype == StreamFactory.StreamDataType.Values)
                    {
                        foreach (ValueDataStream<KeyType, ValType> stream in vstreams.Values)
                        {
                            stream.OpenStream();
                        }
                    }
                    else
                    {
                        foreach (FileDataStream<KeyType> dstream in fstreams.Values)
                        {
                            dstream.OpenStream();
                        }
                    }
                }

            }
            isClosed = false;
            if (logger != null) logger.Log("End Stream Reopen");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal bool Sync()
        {
            bool retVal= true;
            if (logger != null) logger.Log("Start Stream Sync");
            if (null != synchronizer)
            {
                retVal = synchronizer.Sync();
                if (retVal)
                {
                    lastSyncTime = DateTime.Now;
                }
            }
            if (logger != null) logger.Log("End Stream Sync");
            return retVal;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Close()
        {
            isSyncWorkerRunning = false;
            return Close(false);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected bool Close(bool retainIndex)
        {
            if (logger != null) logger.Log("Start Stream Close");
            if (!isClosed)
            {
                if (streamptype == StreamFactory.StreamDataType.Values)
                {
                    foreach (ValueDataStream<KeyType, ValType> stream in vstreams.Values)
                    {
                        stream.Close(retainIndex);
                    }
                }
                else
                {
                    foreach (FileDataStream<KeyType> dstream in fstreams.Values)
                    {
                        dstream.Close(retainIndex);
                    }
                }

                // Flush IndexMD if stream is opened for writing
                if (streamop == StreamFactory.StreamOp.Write)
                {
                    if (logger != null) logger.Log("Start IndexMD Flush");
                    if (streamptype == StreamFactory.StreamDataType.Values)
                    {
                        foreach (ValueDataStream<KeyType, ValType> stream in vstreams.Values)
                        {
                            IndexInfo ii = stream.GetIndexInfo();
                            indexMetaData.index_infos[stream.seq_num] = ii;
                        }
                    }
                    else
                    {
                        foreach (FileDataStream<KeyType> dstream in fstreams.Values)
                        {
                            IndexInfo ii = dstream.GetIndexInfo();
                            indexMetaData.index_infos[dstream.seq_num] = ii;
                        }
                    }
                    if (streamtype == StreamFactory.StreamSecurityType.Secure)
                    {
                        indexMetaData.SignMetadata(OwnerPriKey);
                    }
                    else
                    {
                        indexMetaData.FlushIndexMetaData();
                    }
                    if (logger != null) logger.Log("End IndexMD Flush");
                    Sync();

                    if (logger != null) logger.Log("Start StreamMD Flush");
                    if (metadataClient == null)
                    {
                        localMdServer.FlushMetaDataServer();
                    }
                    if (logger != null) logger.Log("End StreamMD Flush");
                }
            }
            isClosed = true;
            if (logger != null) logger.Log("End Stream Close");
            return isClosed;
        }

        public void Dispose() // NOT virtual
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalizer from running.
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    disposed = true;
                    if (disposing)
                    {
                        // Call Dispose() on other objects owned by this instance.
                        // You can reference other finalizable objects here.
                    }

                    // Release unmanaged resources owned by (just) this object.
                    Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in Dispose: " + e.StackTrace);
            }
        }

        ~MetaStream()
        {
            Dispose(false);
        }


    }
}
