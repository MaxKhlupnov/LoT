using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class ValueDataStream<KeyType, ValType> : IDisposable
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
        public int seq_num {get; set;}
        
        // Ownership
        protected MetaDataService.Principal caller;
        protected MetaDataService.Principal owner;
        protected string OwnerPriKey;
        protected string OwnerPubKey;

        // Location
        protected MetaDataService.AccountInfo account;
        protected ISync synchronizer;
        
        // Security related
        protected MetaDataService.ACLEntry acl_md;
        
        // Index related
        protected Dictionary<IKey, List<DataBlockInfo>> index; // key -> [offsets_in_file]
        protected IKey latest_key;
        protected long t_s;
        protected long t_e;
        protected bool isSealed;
        protected Byte[] IndexHash;

        private byte[] chunkListHash;

        private Dictionary<IKey, long> indexHeader = null; // key -> key_offset in indexFile 
        private long indexSize = 0; 
        
        // Low level helpers
        protected FileStream fout;
        protected BinaryWriter fs_bw;
        protected FileStream fin;
        protected BinaryReader fs_br;
        protected HashAlgorithm hasher;
        protected MetaDataService.MetaDataServiceClient clnt;
        
        // State related
        protected bool disposed;
        protected bool isClosed;
        protected StreamFactory.StreamOp streamop;
        protected StreamFactory.StreamSecurityType streamtype;
        protected CompressionType streamcompressiontype;

        //constants
        public const string IndexFileName = "index.dat";
        public const string DataLogFileName = "stream.dat";

        public enum WriteOp : byte { AppendOp = 0, UpdateOp }

        //Sync related
        protected int StreamChunkSizeForUpload;
        protected int StreamThreadPoolSize ;

        public void DumpLogs(string file)
        {
            if (logger != null)
                logger.Dump(file);
        }

        protected void Initialize(Logger Log, FqStreamID FQSID, 
            int num, CallerInfo Ci, LocationInfo Li,
            StreamFactory.StreamOp op, StreamFactory.StreamSecurityType type,
            CompressionType ctype, int ChunkSizeForUpload, int ThreadPoolSize)
        {
            // Logging related
            logger = Log;
            if (logger != null) logger.Log("Start ValueDataStream Init DataStructures");

            // Naming related
            streamid = FQSID;
            stream = new MetaDataService.FQStreamID();
            stream.HomeId = FQSID.HomeId;
            stream.AppId = FQSID.AppId;
            stream.StreamId = FQSID.StreamId;
            BaseDir = Path.GetFullPath((null != Ci.workingDir) ? Ci.workingDir : Directory.GetCurrentDirectory());
            targetDir = BaseDir + "/" + streamid.ToString();
            seq_num = num;
            targetDir = targetDir + "/" + seq_num;
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Ownership related
            caller = new MetaDataService.Principal();
            caller.HomeId = streamid.HomeId;
            caller.AppId = Ci.appName;
            owner = new MetaDataService.Principal();
            owner.HomeId = streamid.HomeId;
            owner.AppId = streamid.AppId;

            // Location
            account = new MetaDataService.AccountInfo();
            account.accountName = Li.accountName;
            account.accountKey = Li.accountKey;
            account.location = "" + Li.st;

            // Index related
            index = new Dictionary<IKey, List<DataBlockInfo>>();
            t_s = StreamFactory.NowUtc();
            t_e = 0;
            IndexHash = null;
            isSealed = false;

            // Low level
            hasher = new SHA256CryptoServiceProvider();

            // State maint
            streamop = op;
            streamtype = type;
            streamcompressiontype = ctype;

            //sync related
            this.StreamChunkSizeForUpload = ChunkSizeForUpload;
            this.StreamThreadPoolSize = ThreadPoolSize;
        }

        public IndexInfo GetIndexInfo()
        {
            IndexInfo ret = new IndexInfo();
            ret.startTime = t_s;
            ret.endTime = t_e;
            ret.isSealed = isSealed;
            ret.indexHash = IndexHash;
            ret.chunkListHash = chunkListHash;
            return ret;
        }

        protected void CreateSync(SynchronizeDirection dir)
        {
            // Create Synchronizer
            if (account.location == "None")
            {
                synchronizer = null;
            }
            else if (streamtype == StreamFactory.StreamSecurityType.Secure)
            {
                LocationInfo Li = new LocationInfo(account.accountName, account.accountKey, SyncFactory.GetSynchronizerType(account.location));
                synchronizer = SyncFactory.Instance.CreateSynchronizer(Li, streamid.ToString().Replace('/', '-').ToLower() + "-" + seq_num, logger, dir, streamcompressiontype, this.StreamChunkSizeForUpload, this.StreamThreadPoolSize, EncryptionType.AES, acl_md.encKey, acl_md.IV);
                synchronizer.SetLocalSource(targetDir);
                synchronizer.SetIndexFileName(IndexFileName);
                synchronizer.SetDataFileName(DataLogFileName);
            }
            else
            {
                LocationInfo Li = new LocationInfo(account.accountName, account.accountKey, SyncFactory.GetSynchronizerType(account.location));
                synchronizer = SyncFactory.Instance.CreateSynchronizer(Li, streamid.ToString().Replace('/', '-').ToLower() + "-" + seq_num, logger, dir, streamcompressiontype, this.StreamChunkSizeForUpload, this.StreamThreadPoolSize);
                synchronizer.SetLocalSource(targetDir);
                synchronizer.SetIndexFileName(IndexFileName);
                synchronizer.SetDataFileName(DataLogFileName);
            }
        }
        
        public ValueDataStream(Logger Log, FqStreamID FQSID, int num, 
            CallerInfo Ci, LocationInfo Li, 
            StreamFactory.StreamOp op, StreamFactory.StreamSecurityType type,
            CompressionType ctype, int ChunkSizeForUpload, int UploadThreadPoolSize, 
            string prkey, string pukey, MetaDataService.ACLEntry key_md, 
            IndexInfo ii,
            bool alreadyExists = true)
        {
            Initialize(Log, FQSID, num, Ci, Li, op, type, ctype, ChunkSizeForUpload, UploadThreadPoolSize);

            if (streamtype == StreamFactory.StreamSecurityType.Secure)
            {
                // Key related
                OwnerPriKey = prkey;
                OwnerPubKey = pukey;
                acl_md = key_md;
            }
            if (logger != null) logger.Log("End ValueDataStream Init DataStructures");

            // Fetch data
            FetchAndFillIndex(ii, alreadyExists);

            // Reset sync to upload
            if (op == StreamFactory.StreamOp.Write)
            {
                CreateSync(SynchronizeDirection.Upload);
            }

            isClosed = false;
            if (logger != null) logger.Log("Start ValueDataStream ReadFromDisk Open");
            OpenStream();
            if (logger != null) logger.Log("End ValueDataStream ReadFromDisk Open");
            disposed = false;
        }

        protected void FetchAndFillIndex(IndexInfo ii, bool alreadyExists = true)
        {
            if (alreadyExists == true)
            {
                if (logger != null) logger.Log("Start ValueDataStream Fetch Data At Open");
                CreateSync(SynchronizeDirection.Download);
                Sync();
                if (logger != null) logger.Log("End ValueDataStream Fetch Data At Open");

                // Index related
                t_s = ii.startTime;
                t_e = ii.endTime;
                isSealed = ii.isSealed;

                if (logger != null) logger.Log("Start ValueDataStream Build Index");
                if (!isSealed)
                {
                    if (FillIndex() == false)
                        throw new InvalidDataException("Couldn't fill index for stream");
                }
                else
                {
                    BuildIndexHeader();
                }
                if (logger != null) logger.Log("End ValueDataStream Build Index");

                if (streamtype == StreamFactory.StreamSecurityType.Secure)
                {
                    VerifyIntegrity(ii, alreadyExists);
                }
            }
        }

        protected void VerifyIntegrity(IndexInfo ii, bool alreadyExists)
        {
            // Verify intergrity
            if (logger != null) logger.Log("Start ValueDataStream Verify Integrity");
            if ((VerifyIndex(ii.indexHash) == false) || (VerifyChunkListHash(ii.chunkListHash) == false))
            {
                throw new InvalidDataException("Couldn't verify metadata integrity/freshness for segment");
            }
            if (logger != null) logger.Log("End ValueDataStream Verify Integrity");
        }
        
        protected SynchronizerType GetST(string location)
        {
            if (location == "None")
                return SynchronizerType.None;
            else if (location == "Azure")
                return SynchronizerType.Azure;
            else
                return SynchronizerType.AmazonS3;
        }
        
        protected bool VerifyIndex(Byte[] idx_hash)
        {
            if (idx_hash.SequenceEqual(IndexHash))
            {
                return true;
            }
            return false;
        }

        protected bool VerifyChunkListHash(Byte[] hash)
        {
            if (hash.SequenceEqual(chunkListHash))
            {
                return true;
            }
            return false;
        }



        protected bool FillIndex()
        {
            try
            {
                
                string IndexFQN = targetDir + "/" + IndexFileName;
                //Console.WriteLine("***************** filling index : "+ IndexFQN);
                if (File.Exists(IndexFQN))
                {
                    FileStream iout = new FileStream(IndexFQN, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    BinaryReader index_br = new BinaryReader(iout);

                    long latest_ts = 0;

                    try
                    {
                        while (true)
                        {
                            string key = index_br.ReadString();
                            IKey ikey = SerializerHelper<KeyType>.DeserializeFromJsonStream(key) as IKey;
                            int num_offsets = index_br.ReadInt32();
                            List<DataBlockInfo> offsets = new List<DataBlockInfo>(num_offsets);
                            for (int i = 0; i < num_offsets; i++)
                            {
                                long ts = index_br.ReadInt64();
                                long offset = index_br.ReadInt64();
                                DataBlockInfo tso = new DataBlockInfo(ts, offset);

                                if (streamtype == StreamFactory.StreamSecurityType.Secure)
                                {
                                    Int32 h_len = index_br.ReadInt32();
                                    if(h_len > 0)
                                        tso.hashValue = index_br.ReadBytes(h_len);
                                }
                                offsets.Add(tso);
                                if (ts > latest_ts)
                                {
                                    latest_ts = ts;
                                    latest_key = ikey;
                                }
                            }
                            index[ikey] = offsets;
                            IncrementIndexSize(offsets.Count);
                        }

                    }
                    catch (Exception)
                    {
                        // done

                //    }
                //    finally
              //      {
                        if (streamtype == StreamFactory.StreamSecurityType.Secure)
                        {
                            iout.Position = 0;
                            IndexHash = hasher.ComputeHash(iout);
                        }

                       // Console.WriteLine("********************* closed " + IndexFQN);
                        index_br.Close();
                        iout.Close();
                        GC.Collect();
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("{0} Exception caught.", exp);
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal bool OpenStream()
        {
            try
            {
                // if remote stream, read op (reading by chunks)
                if (synchronizer != null && streamop == StreamFactory.StreamOp.Read)
                {
                    fs_bw = null;
                    fs_br = null;
                }
                else
                {
                    // create the FileStream
                    fout = new FileStream(targetDir + "/" + DataLogFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                    fout.Seek(0, SeekOrigin.End);
                    fs_bw = new BinaryWriter(fout);
                    fin = new FileStream(targetDir + "/" + DataLogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fs_br = new BinaryReader(fin);
                }

                isClosed = false;
                return true;
            }
            catch (Exception exp)
            {
                Console.WriteLine("Failed to open file: " + targetDir + "/" + DataLogFileName);
                Console.WriteLine("{0} Exception caught.", exp);
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void Sync() 
        {
            if (logger != null) logger.Log("Start ValueDataStream Sync");
            if (null != synchronizer)
            {
                if (synchronizer.Sync())
                    this.chunkListHash = synchronizer.GetChunkListHash();
            }
            if (logger != null) logger.Log("End ValueDataStream Sync");
        }



        [MethodImpl(MethodImplOptions.Synchronized)]
        internal DataBlock<KeyType, ValType> ReadDataBlock(long offset, bool ignoreIntegrity = false)
        {
            if (logger != null) logger.Log("Start ValueDataStream GetDB");
            int data_len;
            byte[] data;


            try
            {
                if (logger != null) logger.Log("Start ValueDataStream ReadData");
                if (fs_br == null) // remote read
                {
                    synchronizer.SetDataFileName(DataLogFileName);
                    int dataLength = BitConverter.ToInt32(synchronizer.ReadData(offset, 4), 0);
                    data = synchronizer.ReadData(offset + 4, dataLength);

                }
                else
                {
                    if (logger != null) logger.Log("Start ValueDataStream ReadFromDisk");
                    fs_br.BaseStream.Seek(offset, SeekOrigin.Begin);
                    data_len = fs_br.ReadInt32();
                    data = fs_br.ReadBytes(data_len);
                    if (logger != null) logger.Log("End ValueDataStream ReadFromDisk");
                }
                if (logger != null) logger.Log("End ValueDataStream ReadData");

                if (logger != null) logger.Log("Start ValueDataStream Deserialize DataBlock");
                //DataBlock<KeyType, ValType> db = SerializerHelper<DataBlock<KeyType, ValType>>.DeserializeFromByteStream(new MemoryStream(data));
                DataBlock<KeyType, ValType> db = SerializerHelper<DataBlock<KeyType, ValType>>.DeserializeFromProtoStream(new MemoryStream(data));
                if (logger != null) logger.Log("End ValueDataStream Deserialize DataBlock");

                /*if (streamtype == StreamFactory.StreamSecurityType.Secure && ignoreIntegrity == false)
                {
                    if (logger != null) logger.Log("Start verifying hash of db within segment");
                    // Commenting out because decryption is carried out by the syncronizer
                 //   if (!hasher.ComputeHash(db.value.GetBytes()).SequenceEqual(dbi.hashValue))
                   //     return null;
                    if (logger != null) logger.Log("End verifying hash of db within segment");
                }

                if (streamtype == StreamFactory.StreamSecurityType.Secure)
                {
                    if (logger != null) logger.Log("Start decrypting db within index");
                    // Commenting out because decryption is carried out by the syncronizer
                    //db.value = new ByteValue(Crypto.DecryptBytesSimple(db.value.GetBytes(), Crypto.KeyDer(Crypto.GetSpecificKey(acl_md.encKey, acl_md.keyVersion, dbi.key_version)), acl_md.IV));
                    if (logger != null) logger.Log("End decrypting db within index");
                }*/
                if (logger != null) logger.Log("End ValueDataStream GetDB");
                return db;
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                if (logger != null) logger.Log("End ValueDataStream GetDB");
                return null;
            }
        }


        protected bool GetHelper(IKey key)
        {
            bool isContained = false;

            if (!key.GetType().Equals(typeof(KeyType)))
            {
                throw new InvalidDataException("Invalid IKey Type");
            }

            /*
            string keyHash = StreamFactory.GetString(
                                sha1.ComputeHash(StreamFactory.GetBytes(key)));
             */

            if (index.ContainsKey(key))
            // if (indexHeader.ContainsKey(key))
            {
                // TODO: this could just have been a simple collision
                //   -- make sure that this is the same key
                //   -- if different, use a secondary hash function

                isContained = true;
            }
            return isContained;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IValue Get(IKey key)
        {
            return Get(key, false);
        }
        
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected IValue Get(IKey key, bool ignoreIntegrity = false)
        {
            List<DataBlockInfo> offsets = null;

            if (logger != null) logger.Log("Start ValueDataStream Get Offset");
            if (isSealed)// if the stream has been sealed. then get the required offset list from indexHeader 
            {
                offsets = GetOffsetListFromHeader(key);
                if (offsets == null)
                    return null;
            }
            else if (GetHelper(key)) // if not sealed, and this is a valid key get it from index
                offsets = index[key];
            else
                return null;
            if (logger != null) logger.Log("End ValueDataStream Get Offset");

            if (logger != null) logger.Log("Start ValueDataStream Read DataBlock");
            // use the offset list to perform required op
            DataBlock<KeyType, ValType> db = ReadDataBlock(offsets.Last().offset, ignoreIntegrity);
            if (db == null)
                return null;
            IValue value = db.getValue();
            if (logger != null) logger.Log("End ValueDataStream Read DataBlock");

            return value;

            /* 
                // collision?
                // TODO: need to handle collision?
                throw new InvalidDataException("Key mismatch (unhandled key collision)");
            */
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public DataBlockInfo GetDBI(IKey key)
        {
            List<DataBlockInfo> offsets = null;
            if (isSealed)
                offsets = GetOffsetListFromHeader(key);
            //else if (GetHelper(key) && streamtype == StreamFactory.StreamSecurityType.Secure)
            else if (GetHelper(key))
                offsets = index[key];
            else
                return null;

            return offsets.Last();
        }

        /*
        * return null if key not found
        */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<IDataItem> GetAll(IKey key)
        {
            List<DataBlockInfo> offsets = null;
            if (isSealed)
                offsets = GetOffsetListFromHeader(key);
            else if (GetHelper(key))
                offsets = index[key];
            else
                return null;

            DataItems<KeyType, ValType> di = null;
            if (offsets != null)
            {
                di = new DataItems<KeyType, ValType>(offsets, this, key);
            }
            return di;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<IDataItem> GetAll(IKey key, long startTimeStamp, long endTimeStamp)
        {
            if (logger != null) logger.Log("Start ValueDataStream Get Offset");
            List<DataBlockInfo> allOffsets = null;
            if (isSealed)
                allOffsets = GetOffsetListFromHeader(key);
            else if (GetHelper(key))
            {
                allOffsets = index[key];
            }
            else
                return null;
            

            DataItems<KeyType, ValType> di = null;
            if (allOffsets != null)
            {
                List<DataBlockInfo> offsets = new List<DataBlockInfo>();
                int startTSIndex = allOffsets.BinarySearch(new DataBlockInfo(startTimeStamp, 0), new DataBlockInfoComparer());
                int endTSIndex = allOffsets.BinarySearch(new DataBlockInfo(endTimeStamp, 0), new DataBlockInfoComparer());


                if (startTSIndex < 0)
                    startTSIndex = ~startTSIndex;
                if (endTSIndex < 0)
                    endTSIndex = ~endTSIndex - 1;


                for (int i = startTSIndex; i <= endTSIndex; i++)
                    offsets.Add(allOffsets.ElementAt(i));
                /*
                foreach (DataBlockInfo tso in allOffsets)
                {
                    if (tso.Between(startTimeStamp, endTimeStamp))
                    {
                        offsets.Add(tso);
                    }
                }
                 */
                di = new DataItems<KeyType, ValType>(offsets, this, key);
                if (logger != null) logger.Log("End ValueDataStream Get Offset");
            }
            return di;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<IDataItem> GetAllWithSkip(IKey key, long startTimeStamp, long endTimeStamp, long skip)
        {
            List<DataBlockInfo> allOffsets = null;
            if (isSealed)
                allOffsets = GetOffsetListFromHeader(key);
            else if (GetHelper(key))
            {
                allOffsets = index[key];
            }
            else
                return null;

            DataItems<KeyType, ValType> di = null;
            if (allOffsets != null)
            {
                List<DataBlockInfo> requiredOffsets = new List<DataBlockInfo>();

                foreach (DataBlockInfo datablockinfo in allOffsets)
                {
                    if (datablockinfo.ts > endTimeStamp)
                        break;
                    else if (datablockinfo.ts >= startTimeStamp && datablockinfo.ts <= endTimeStamp && (datablockinfo.ts - startTimeStamp) % skip == 0)
                    {
                        requiredOffsets.Add(datablockinfo);
                    }
                }

                di = new DataItems<KeyType, ValType>(requiredOffsets, this, key);
            }
            return di;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public HashSet<IKey> GetKeys(IKey startKey, IKey endKey)
        {
            List<IKey> keySet; 
            if (isSealed)
                keySet = indexHeader.Keys.ToList();
            else
                keySet = index.Keys.ToList();

            HashSet<IKey> retVal = new HashSet<IKey>();
            foreach (var item in keySet)
            {
                if ((startKey == null) && (endKey == null))
                {
                    retVal.Add(item);
                }
                else if ( (startKey == null) && (item.CompareTo(endKey) <= 0) )
                {
                    retVal.Add(item);
                }
                if (item.Between(startKey, endKey))
                {
                    retVal.Add(item);
                }
            }
            return retVal;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Tuple<IKey, IValue> GetLatest()
        {
            return GetLatest(false);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected Tuple<IKey, IValue> GetLatest(bool ignoreIntegrity = false)
        {
            Tuple<IKey, IValue> latest_kv = null;
            if (latest_key != null)
            {
                IValue value = Get(latest_key);
                if (value != null)
                {
                    latest_kv = new Tuple<IKey, IValue>(latest_key, value);
                }
            }
            return latest_kv;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Tuple<IValue, long> GetLatest(IKey tag)
        {
            Tuple<IValue, long> latest_tuple = null;
            if (tag != null)
            {
                IValue value = Get(tag);
                if (value != null)
                {
                    DataBlockInfo dbi = GetDBI(tag);
                    latest_tuple = new Tuple<IValue, long>(value, dbi.ts);
                }
            }
            return latest_tuple;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Append(IKey key, IValue value, long timestamp)
        {
            if (logger != null) logger.Log("Start ValueDataStream Append");
            UpdateHelper(key, value, true, null, timestamp);
            if (logger != null) logger.Log("End ValueDataStream Append");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Append(List<Tuple<IKey,IValue>> list)
        {
            long timestamp = StreamFactory.NowUtc();
            foreach(Tuple<IKey,IValue> keyValPair in list)
            {
                if (logger != null) logger.Log("Start ValueDataStream Append");
                UpdateHelper(keyValPair.Item1, keyValPair.Item2, true, null, timestamp);
                if (logger != null) logger.Log("End ValueDataStream Append");
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Append(List<IKey> listOfKeys, IValue value)
        {
            long offset = -1;
            long timestamp = StreamFactory.NowUtc();
            foreach (IKey key in listOfKeys)
            {
                if (logger != null) logger.Log("Start ValueDataStream Append");
                offset= UpdateHelper(key, value, true, null, timestamp, offset);
                if (logger != null) logger.Log("End ValueDataStream Append");
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Update(IKey key, IValue value)
        {
            UpdateHelper(key, value, false);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected long UpdateHelper(IKey key, IValue value, bool IsAppend, Byte[] valHash = null, long timestamp=-1, long offsetInStream = -1 )
        {
            long offset;
            if (!key.GetType().Equals(typeof(KeyType)))
            {
                throw new InvalidDataException("Invalid IKey Type");
            }
            if (!value.GetType().Equals(typeof(ValType)))
            {
                throw new InvalidDataException("Invalid IValue Type");
            }

            if (logger != null) logger.Log("Start ValueDataStream Tag Lookup");
            List<DataBlockInfo> offsets;
            // check if the entry is present (and record offset)
            if (index.ContainsKey(key))
            {
                // TODO: this could just have been a simple collision
                //   -- make sure that this is the same key
                //   -- if different, use a secondary hash function

                offsets = index[key];
            }
            else
            {
                offsets = new List<DataBlockInfo>();
                index[key] = offsets;
                // IncrementIndexSize(offsets.Count);
                IncrementIndexSize(1);
            }
            if (logger != null) logger.Log("End ValueDataStream Tag Lookup");

            if (logger != null) logger.Log("Start ValueDataStream Construct DataBlock");
            // write <op (1B), ts, key, val_len, val>
            byte op;
            if (IsAppend)
            {
                op = (byte)WriteOp.AppendOp;
            }
            else
            {
                op = (byte)WriteOp.UpdateOp;
            }


            long ts;
            // Construct datablock
            if (timestamp == -1)
                ts = StreamFactory.NowUtc();
            else
                ts = timestamp;

            DataBlock<KeyType, ValType> db = new DataBlock<KeyType, ValType>();
            db.op = op;
            //db.timestamp = ts;
            //db.setKey(key);
            /*
             * Commenting out because the synchronizer will do this now at the chunk level
             * if (streamtype == StreamFactory.StreamSecurityType.Secure)
            {
                //if (logger != null) logger.Log("Start encrypting value");
                db.value = new ByteValue(Crypto.EncryptBytesSimple(value.GetBytes(), Crypto.KeyDer(acl_md.encKey), acl_md.IV));
                //if (logger != null) logger.Log("End encrypting value");
            }
            else*/
            {
                //db.value = new ByteValue(value.GetBytes());
                db.setValue(value);
            }
            if (logger != null) logger.Log("End ValueDataStream Construct DataBlock");

            if (offsetInStream == -1)
            {
                if (logger != null) logger.Log("Start ValueDataStream Serialize DataBlock");
                //Byte[] buffer = db.SerializeToByteStream().ToArray();
                Byte[] buffer = SerializerHelper<DataBlock<KeyType, ValType>>.SerializeToProtoStream(db).ToArray();
                if (logger != null) logger.Log("End ValueDataStream Serialize DataBlock");

                if (logger != null) logger.Log("Start ValueDataStream WriteToDisc DataBlock");
                // fs_bw.Write(db.SerializeToJsonStream());
                // get file offset; add <key_hash, offset> to index
                fs_bw.BaseStream.Seek(0, SeekOrigin.End);
                offset = fs_bw.BaseStream.Position;
                fs_bw.Write(buffer.Length);
                fs_bw.Write(buffer);
                fs_bw.Flush();
                if (logger != null) logger.Log("End ValueDataStream WriteToDisc DataBlock");
            }
            else
            {
                offset = offsetInStream;
            }
            // Construct dbinfo in index
            if (logger != null) logger.Log("Start ValueDataStream Construct and Add DataBlockInfo");
            DataBlockInfo latest_tso = new DataBlockInfo(ts, offset);
            if (streamtype == StreamFactory.StreamSecurityType.Secure)
            {
                if (valHash != null) // passed from the dir stream and written into datablockinfo. not written for file stream
                {
                    latest_tso.hashValue = valHash;
                }
                /*
                    else
                    {
                        if (logger != null) logger.Log("Start taking hash of encrypted value");
                        latest_tso.hashValue = hasher.ComputeHash(db.value.GetBytes());
                        if (logger != null) logger.Log("End taking hash of encrypted value");
                    
                    }
                     latest_tso.key_version = acl_md.keyVersion;
                */
            }

            // apply to index
            if (IsAppend)
            {
                offsets.Add(latest_tso);
                // IncrementIndexSize();
            }
            else
            {
                if (offsets.Count == 0)
                {
                    offsets.Add(latest_tso);
                    // IncrementIndexSize();
                }
                else
                {
                    offsets[offsets.Count - 1] = latest_tso;
                }
            }
            if (logger != null) logger.Log("End ValueDataStream Construct and Add DataBlockInfo");
            return offset;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Flush(bool retainIndex)
        {
            if (logger != null) logger.Log("Start ValueDataStream Flush");
            FlushIndex(retainIndex);
            if (logger != null) logger.Log("End ValueDataStream Flush");
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Seal(bool noop)
        {
            if (!isSealed && !isClosed && (streamop == StreamFactory.StreamOp.Write))
            {
                Close();
                isSealed = true;
                OpenStream();
            }
        }

        // writes index to file: read by FillIndex() and GetOffsetListFromHeader().
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void FlushIndex(bool retainIndex = false)
        {
            if (!isSealed && !isClosed && (streamop == StreamFactory.StreamOp.Write))
            {
                FileStream iout = new FileStream(targetDir + "/" + IndexFileName, FileMode.Create,
                                                 FileAccess.ReadWrite, FileShare.ReadWrite);
                iout.Seek(0, SeekOrigin.End);

                BinaryWriter index_bw = new BinaryWriter(iout);
                indexHeader = new Dictionary<IKey, long>();

                foreach (KeyValuePair<IKey, List<DataBlockInfo>> IndexEntry in index)
                {
                    indexHeader[IndexEntry.Key] = index_bw.BaseStream.Position;

                    index_bw.Write(IndexEntry.Key.SerializeToJsonStream());
                    index_bw.Write((Int32)IndexEntry.Value.Count);
                    foreach (DataBlockInfo tso in IndexEntry.Value)
                    {
                        index_bw.Write((Int64)tso.ts);
                        index_bw.Write((Int64)tso.offset);

                        if (streamtype == StreamFactory.StreamSecurityType.Secure)
                        {
                            if (tso.hashValue != null)// not null for secure dir streams
                            {
                                index_bw.Write((Int32)tso.hashValue.Length);
                                index_bw.Write(tso.hashValue);
                            }
                            else
                                index_bw.Write((Int32)0);
                        }
                    }
                }

                if (streamtype == StreamFactory.StreamSecurityType.Secure)
                {
                    iout.Position = 0;
                    IndexHash = hasher.ComputeHash(iout);
                }

                iout.Flush(true);
                index_bw.Close();
                iout.Close();
            }
            //***
            if (!retainIndex)
            {
                index.Clear();
                index = null;
                indexSize = 0;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Close(bool retainIndex = false)
        {
            if (logger != null) logger.Log("Start ValueDataStream Close");
            if (logger != null) logger.Log("Start ValueDataStream File Close");
            if (fs_bw != null)
            {
                if ( (streamop == StreamFactory.StreamOp.Write) && !isSealed )
                  fout.Flush(true);
                fs_bw.Close();
                fs_bw = null;
            }

            if (fs_br != null)
            {
                fs_br.Close();
                fs_br = null;
            }
            if (logger != null) logger.Log("End ValueDataStream File Close");

            if (!isClosed)
            {
                if (!isSealed && (streamop == StreamFactory.StreamOp.Write))
                {
                    t_e = StreamFactory.NowUtc();
                    Flush(retainIndex);
                    Sync();
                }
                else if (streamop == StreamFactory.StreamOp.Read)
                {
                    Flush(retainIndex);
                }
            }

            isClosed = true;
            if (logger != null) logger.Log("End ValueDataStream Close");
            return isClosed;
        }

        /*
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DeleteStream()
        {
            if (!isClosed)
            {
                if (fs_bw != null)
                {
                    fs_bw.Close();
                    fs_bw = null;
                }

                if (fs_br != null)
                {
                    fs_br.Close();
                    fs_br = null;
                }
                isClosed = true;
            }

            Boom(targetDir);
            Sync();
        }

        internal void Boom(string DirName)
        {
            if (DirName != null)
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(DirName);

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
        */


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
                    if (disposing)
                    {
                        // Call Dispose() on other objects owned by this instance.
                        // You can reference other finalizable objects here.
                    }

                    // Release unmanaged resources owned by (just) this object.
                    Close();
                    disposed = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in Dispose: " + e.StackTrace);
            }
        }

        ~ValueDataStream()
        {
            Dispose(false);
        }

        //***
        private void BuildIndexHeader()
        {
            FileStream iout;
            BinaryReader index_br;
            indexHeader = new Dictionary<IKey, long>();



            string IndexFilePath = targetDir + "/" + IndexFileName;
            

            if (!File.Exists(IndexFilePath))
                return;
            //Console.WriteLine(DateTime.Now+" ******** filling indexheader " + IndexFilePath);
            iout = new FileStream(IndexFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            index_br = new BinaryReader(iout);

            try
            {
                while (true)
                {
                    long keyOffset = index_br.BaseStream.Position;
                    string key = index_br.ReadString();
                    IKey ikey = SerializerHelper<KeyType>.DeserializeFromJsonStream(key) as IKey;
                    indexHeader[ikey] = keyOffset;

                    int num_offsets = index_br.ReadInt32();
                    for (int i = 0; i < num_offsets; i++)
                    {
                        index_br.ReadBytes(16);//timestamp and offset

                        if (streamtype == StreamFactory.StreamSecurityType.Secure)
                        {
                            index_br.ReadUInt32();//key_version
                            Int32 h_len = index_br.ReadInt32();//h_len?
                            index_br.ReadBytes(h_len);//hashvalue
                        }
                    }
                }
            }
            catch (Exception)
            {
                // done
        //    }
       //     catch(Exception e)
        //    {
        //        Console.Write("Exception in buildindexheader: "+e);
       //     }
       //     finally
       //     {
                if (streamtype == StreamFactory.StreamSecurityType.Secure)
                {
                    iout.Position = 0;
                    IndexHash = hasher.ComputeHash(iout);
                }
                GC.Collect();
                index_br.Close();
                iout.Close();
                //Console.WriteLine(DateTime.Now+" ******** closed indexheader " + IndexFilePath);
                GC.Collect();
            }
        }

        private List<DataBlockInfo> GetOffsetListFromHeader(IKey key)
        {
            if (!indexHeader.ContainsKey(key))
                return null;

            long keyOffset = indexHeader[key];

            string IndexFilePath = targetDir + "/" + IndexFileName;
            if (!File.Exists(IndexFilePath))
                throw new Exception("index file is missing!!!!! ");
            
            FileStream iout = new FileStream(IndexFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            BinaryReader index_br = new BinaryReader(iout);
            index_br.BaseStream.Seek(keyOffset, SeekOrigin.Begin);

            string keyInFile = index_br.ReadString();// increment file pointer
            //IKey ikey = SerializerHelper<KeyType>.DeserializeFromJsonStream(keyInFile) as IKey;
            // TODO compare with Ikey above


            int num_offsets = index_br.ReadInt32();
            List<DataBlockInfo> offsets = new List<DataBlockInfo>(num_offsets);
            for (int i = 0; i < num_offsets; i++)
            {
                long ts = index_br.ReadInt64();
                long offset = index_br.ReadInt64();
                DataBlockInfo tso = new DataBlockInfo(ts, offset);

                if (streamtype == StreamFactory.StreamSecurityType.Secure)
                {
                    Int32 h_len = index_br.ReadInt32();
                    if (h_len > 0)
                        tso.hashValue = index_br.ReadBytes(h_len);
                }
                offsets.Add(tso);    
            }

            return offsets;
        }

        internal MetaDataService.AccountInfo GetAccountInfo()
        {
            return this.account;
        }

        private void IncrementIndexSize(int OffsetCount)
        {
            indexSize += OffsetCount * (2 * sizeof(long) + sizeof(uint) + 32 /*hash array*/ );
        }
        
        private void IncrementIndexSize()
        {
            indexSize += (2 * sizeof(long) + sizeof(uint));
        }

        internal long GetIndexSize()
        {
            return indexSize;
        }
    }
}
