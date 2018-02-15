//#define HDS_COMMENT
#if HDS_COMMENT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Common;
using System.Collections;

//#define HDS_CHECK_ACL

namespace HDS
{
    public class TS_Offset : IEquatable<TS_Offset>, IComparable<TS_Offset>
    {
        public long ts;
        public long offset;

        public TS_Offset(long t, long o)
        {
            ts = t;
            offset = o;
        }
       
        public bool Equals(TS_Offset other) 
        {
            if (other == null) 
                return false;

            if (this.ts == other.ts)
                return true;
            else 
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null) 
                return false;

            TS_Offset tso = obj as TS_Offset;
            if (tso == null)
                return false;
            else    
                return Equals(tso);   
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int CompareTo(TS_Offset other)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (other == null) return 1;

            return ts.CompareTo(other.ts);
        }

        /* returns true if startTime >= ts <= endTime */
        public bool Between(long startTime, long endTime)
        {
            return ((ts >= startTime) && (ts <= endTime)) ? true : false;
        }
    }

    public class OldDataFileStream<KeyType, ValType> : IStream, IDisposable
    {
        protected string callerId;
        protected int callerSecret;
        protected string targetDir;
        protected OldMetaData md;
        protected FileStream fout;
        protected BinaryWriter fs_bw;
        protected FileStream fin;
        protected BinaryReader fs_br;
        protected SHA1CryptoServiceProvider sha1;
        protected Dictionary<IKey, List<TS_Offset>> index; // key -> [offsets_in_file]
        protected TS_Offset latest_tso;
        /* protected List<TS_Offset> ts_index; // [ <ts, offset> ] */
        protected ISync synchronizer;
        protected bool disposed;
        protected bool isReading;
        protected bool isWriting;
        protected bool isClosed;

        public enum WriteOp : byte { AppendOp = 0, UpdateOp }

        public OldDataFileStream(FqStreamID FQSID, StreamFactory.StreamOp Op, CallerInfo Ci, ISync sync) 
        {
            if (!typeof(IKey).IsAssignableFrom(typeof(KeyType)))
            {
                throw new InvalidDataException("KeyType must implement IKey");
            }
            if (!typeof(IValue).IsAssignableFrom(typeof(ValType)))
            {
                throw new InvalidDataException("ValType must implement IValue");
            }
            
            callerId = Ci.friendlyName;
            callerSecret = Ci.secret;

            synchronizer = sync;

            isClosed = false;
            disposed = false;
            sha1 = new SHA1CryptoServiceProvider();
            index = new Dictionary<IKey, List<TS_Offset>>();
            /* ts_index = new List<TS_Offset>(); */

            latest_tso = new TS_Offset(0, 0);

            // Get the current directory.             
            string BaseDir = Path.GetFullPath((null != Ci.workingDir) ? Ci.workingDir : Directory.GetCurrentDirectory());
            targetDir = BaseDir + "/" + FQSID.ToString();
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            if (synchronizer != null)
            {
                synchronizer.SetLocalSource(targetDir);
            }

            md = new OldMetaData(targetDir, ".md");

            // Check if stream has to be CREATED
            if (!md.load)
            {
                if (FQSID.AppId == callerId)  
                {
                    md.setOwner(FQSID.AppId);
                    md.SetReadAccess(FQSID.AppId);
                    md.SetWriteAccess(FQSID.AppId);
                    md.FlushMetaData();
                    Console.WriteLine("Created stream " + targetDir + " for " + callerId);
                }
                else
                {
                    throw new InvalidOperationException(callerId + " not permitted to create stream for " + FQSID.AppId);
                }
            }

            // Open stream for read or write
            if (Op == StreamFactory.StreamOp.Read)
            {
                if (!OpenForRead())
                {
                    throw new InvalidDataException("Couldn't open stream for reading");
                }
            }
            else
            {
                if (!OpenForWrite())
                {
                    throw new InvalidDataException("Couldn't open stream for writing");
                }
            }


            // Build index
            try
            {
                // permission checks succeeded

                // load index from file if present
                // TODO: if not and stream.dat is present: recreate index from stream.dat
                string IndexFQN = targetDir + "/index.dat";
                if (File.Exists(IndexFQN))
                {
                    FileStream iout = new FileStream(IndexFQN, FileMode.Open,
                                                     FileAccess.Read, FileShare.ReadWrite);

                    BinaryReader index_br = new BinaryReader(iout);

                    try
                    {
                        while (true)
                        {
                            string key = index_br.ReadString();
                            IKey ikey = SerializerHelper<KeyType>.DeserializeFromJsonStream(key) as IKey;
                            int num_offsets = index_br.ReadInt32();
                            List<TS_Offset> offsets = new List<TS_Offset>(num_offsets);
                            for (int i = 0; i < num_offsets; i++)
                            {
                                long ts = index_br.ReadInt64();
                                long offset = index_br.ReadInt64();
                                TS_Offset tso = new TS_Offset(ts, offset);
                                offsets.Add(tso);
                                if (ts > latest_tso.ts)
                                {
                                    latest_tso = tso;
                                }
                            }
                            index[ikey] = offsets;
                        };
                    }
                    catch (EndOfStreamException)
                    {
                        // done
                    }
                    finally
                    {
                        index_br.Close();
                    }
                }

                /*
                // load ts_index
                string TsIndexFQN = targetDir + "/ts_index.dat";
                if (File.Exists(TsIndexFQN))
                {
                    FileStream iout = new FileStream(TsIndexFQN, FileMode.Open,
                                                     FileAccess.Read, FileShare.ReadWrite);

                    BinaryReader index_br = new BinaryReader(iout);

                    try
                    {
                        while (true)
                        {
                            long ts = index_br.ReadInt64();
                            long offset = index_br.ReadInt64();
                            ts_index.Add(new TS_Offset(ts, offset));
                        };
                    }
                    catch (EndOfStreamException)
                    {
                        // done
                    }
                    finally
                    {
                        index_br.Close();
                    }
                }
                */

                // create the FileStream
                fout = new FileStream(targetDir + "/stream.dat", FileMode.OpenOrCreate,
                                      FileAccess.Write, FileShare.ReadWrite);
                fout.Seek(0, SeekOrigin.End);

                fs_bw = new BinaryWriter(fout);

                fin = new FileStream(targetDir + "/stream.dat", FileMode.Open,
                                     FileAccess.Read, FileShare.ReadWrite);

                fs_br = new BinaryReader(fin);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to open file: " + targetDir + "/stream.dat");
                Console.WriteLine("{0} Exception caught.", e);
            }
        }

        protected bool AllowRead()
        {
            return this.isReading || this.isWriting;
        }

        protected bool AllowWrite()
        {
            return this.isWriting;
        }

        public bool GrantReadAccess(string key)
        {
            bool allow = false;
            if (md.IsOwner(this.callerId)) {
                if (md.LockMetaData())
                {
                    md.LoadMetaData();
                    md.SetReadAccess(key);
                    md.FlushMetaData();
                    allow = true;
                    md.UnlockMetaData();
                    Console.WriteLine("Granted read access to " + key);
                }
            }
            return allow;
        }

        public bool GrantWriteAccess(string key)
        {
            bool allow = false;
            if (md.IsOwner(this.callerId))
            {
                if (md.LockMetaData())
                {
                    md.LoadMetaData();
                    md.SetWriteAccess(key);
                    md.FlushMetaData();
                    allow = true;
                    md.UnlockMetaData();
                    Console.WriteLine("Granted write access to " + key);
                }
            }
            return allow;
        }


        public bool RevokeReadAccess(string key)
        {
            bool done = false;
            if (md.IsOwner(this.callerId))
            {
                if (md.LockMetaData())
                {
                    md.LoadMetaData();
                    if (!md.IsActiveReader(key))
                    {
                        md.RemoveReadAccess(key);
                        md.FlushMetaData();
                        done = true;
                        Console.WriteLine("Revoked read access for " + key);
                    }
                    md.UnlockMetaData();
                }
            }
            return done;
        }

        public bool RevokeWriteAccess(string key)
        {
            bool done = false;
            if (md.IsOwner(this.callerId))
            {
                if (md.LockMetaData())
                {
                    md.LoadMetaData();
                    if (!md.IsActiveWriter(key))
                    {
                        md.RemoveWriteAccess(key);
                        md.FlushMetaData();
                        done = true;
                        Console.WriteLine("Revoked write access for " + key);
                    }
                    md.UnlockMetaData();
                }
            }
            return done;
        }

        protected bool OpenForRead()
        {
            bool allow = false;
            if (md.LockMetaData())
            {
                md.LoadMetaData();
#if HDS_CHECK_ACL
                if (md.HasReadAccess(callerId) && !md.HasActiveWriters())
                {
#endif
                    allow = true;
                    md.SetActiveReader(callerId);
                    md.FlushMetaData();
                    isReading = true;
                    Console.WriteLine(callerId + " is now reading stream " + targetDir);
#if HDS_CHECK_ACL
                }
#endif
                    md.UnlockMetaData();
            }
            return allow;
        }

        

        protected bool OpenForWrite()
        {
            bool allow = false;
            if (md.LockMetaData())
            {
                md.LoadMetaData();
#if HDS_CHECK_ACL
                if (md.HasWriteAccess(callerId) && !md.HasActiveReaders() && !md.HasActiveWriters())
                {
#endif
                    allow = true;
                    md.SetActiveWriter(callerId);
                    md.FlushMetaData();
                    isWriting = true;
                    Console.WriteLine(callerId + " is now writing to stream " + targetDir);
#if HDS_CHECK_ACL
                }
#endif
                    md.UnlockMetaData();
            }
            return allow;
        }

        protected bool CloseHelper()
        {
            bool done = false;
            if (md != null && md.LockMetaData())
            {
                md.LoadMetaData();
                md.RemoveActiveReader(callerId);
                md.RemoveActiveWriter(callerId);
                isReading = false;
                isWriting = false;
                md.FlushMetaData();
                done = true;
                Console.WriteLine(callerId + "closed stream " + targetDir);
                md.UnlockMetaData();
            }
            return done;
        }

        internal void Sync()
        {
            if (null != synchronizer)
            {
                synchronizer.Sync();
            }
        }

        internal DataBlock<KeyType, ValType> ReadDataBlock(long offset)
        {
            fs_br.BaseStream.Seek(offset, SeekOrigin.Begin);
            string json = fs_br.ReadString();
            DataBlock<KeyType, ValType> db = SerializerHelper<DataBlock<KeyType, ValType>>.DeserializeFromJsonStream(json);
            return db;
        }


        protected bool GetHelper(IKey key)
        {
            bool isContained = false;

            if (!AllowRead())
            {
                throw new InvalidDataException("Not permitted to read stream");
            }
            if (!key.GetType().Equals(typeof(KeyType)))
            {
                throw new InvalidDataException("Invalid IKey Type");
            }

            /*
            string keyHash = StreamFactory.GetString(
                                sha1.ComputeHash(StreamFactory.GetBytes(key)));
             */

            if (index.ContainsKey(key))
            {
                // TODO: this could just have been a simple collision
                //   -- make sure that this is the same key
                //   -- if different, use a secondary hash function

                isContained = true;
            }
            return isContained;
        }

        /*
         * return null if key not found
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public IValue Get(IKey key)
        {
            if (GetHelper(key))
            {
                List<TS_Offset> offsets = index[key];
                long offset = offsets.Last().offset;
                DataBlock<KeyType, ValType> db = ReadDataBlock(offset);
                IKey ikey = db.key;
                IValue value = db.value;

                if (key.CompareTo(ikey) == 0)
                {
                    return value;
                }
                else
                {
                    // collision?
                    // TODO: need to handle collision?
                    throw new InvalidDataException("Key mismatch (unhandled key collision)");
                }
            }
            return null;
        }

         /*
         * return null if key not found
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<IDataItem> GetAll(IKey key)
        {
            if (GetHelper(key))
            {
                List<TS_Offset> offsets = index[key];
                DataItems<KeyType, ValType> di = new DataItems<KeyType, ValType>(offsets, this, key);
                return di;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<IDataItem> GetAll(IKey key, long startTimeStamp, long endTimeStamp)
        {
            if (GetHelper(key))
            {
                List<TS_Offset> allOffsets = index[key];
                List<TS_Offset> offsets = new List<TS_Offset>();
                foreach (TS_Offset tso in allOffsets)
                {
                    if (tso.Between(startTimeStamp, endTimeStamp))
                    {
                        offsets.Add(tso);
                    }
                }                
                DataItems<KeyType, ValType> di = new DataItems<KeyType, ValType>(offsets, this, key);
                return di;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<IKey> GetKeys(IKey startKey, IKey endKey)
        {
            List<IKey> keyList = new List<IKey>();
            foreach (var item in index.Keys)
            {
                if (item.Between(startKey, endKey))
                {
                    keyList.Add(item);
                }
            }
            return keyList;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Tuple<IKey, IValue> GetLatest()
        {
            if (!AllowRead())
            {
                throw new InvalidDataException("Not permitted to read stream");
            }
            Tuple<IKey, IValue> latest_kv = null;
            if (latest_tso != null)
            {
                DataBlock<KeyType, ValType> db = ReadDataBlock(latest_tso.offset);
                IKey ikey = db.key;
                IValue value = db.value;
                latest_kv = new Tuple<IKey, IValue>(db.key, db.value);
            }
            return latest_kv;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Append(IKey key, IValue value)
        {
            if (!AllowWrite())
            {
                throw new InvalidDataException("Not permitted to write to stream");
            }
            UpdateHelper(key, value, true);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Update(IKey key, IValue value)
        {
            if (!AllowWrite())
            {
                throw new InvalidDataException("Not permitted to write to stream");
            }
            UpdateHelper(key, value, false);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void UpdateHelper(IKey key, IValue value, bool IsAppend)
        {
            if (!key.GetType().Equals(typeof(KeyType)))
            {
                throw new InvalidDataException("Invalid IKey Type");
            }
            if (!value.GetType().Equals(typeof(ValType)))
            {
                throw new InvalidDataException("Invalid IValue Type");
            }

            /*
            string keyHash = StreamFactory.GetString(
                                sha1.ComputeHash(StreamFactory.GetBytes(key)));
             */

            List<TS_Offset> offsets;
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
                offsets = new List<TS_Offset>();
                index[key] = offsets;
            }

            // get file offset; add <key_hash, offset> to index
            fs_bw.BaseStream.Seek(0, SeekOrigin.End);
            long offset = fs_bw.BaseStream.Position;

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
            long ts = StreamFactory.NowUtc();
            DataBlock<KeyType, ValType> db = new DataBlock<KeyType, ValType>();
            db.op = op;
            db.timestamp = ts;
            db.key = key;
            db.value = value;
            fs_bw.Write(db.SerializeToJsonStream());
            fs_bw.Flush();

            latest_tso = new TS_Offset(ts, offset);
            // apply to index
            if (IsAppend)
            {
                offsets.Add(latest_tso);
            }
            else
            {
                if (offsets.Count == 0)
                {
                    offsets.Add(latest_tso);
                } 
                else 
                {
                    offsets[offsets.Count - 1] = latest_tso;
                }
            }
            /* ts_index.Add(new TS_Offset(ts, offset)); */
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Flush()
        {
            FlushIndex();
            /* FlushTsIndex(); */
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void FlushIndex()
        {
            FileStream iout = new FileStream(targetDir + "/index.dat", FileMode.Create,
                                             FileAccess.Write, FileShare.ReadWrite);
            iout.Seek(0, SeekOrigin.End);

            BinaryWriter index_bw = new BinaryWriter(iout); 
            
            foreach (KeyValuePair<IKey, List<TS_Offset>> IndexEntry in index)
            {
                index_bw.Write(IndexEntry.Key.SerializeToJsonStream());
                index_bw.Write((Int32)IndexEntry.Value.Count);
                foreach (TS_Offset tso in IndexEntry.Value)
                {
                    index_bw.Write((Int64)tso.ts);
                    index_bw.Write((Int64)tso.offset);
                }
            }

            index_bw.Close();
        }

        /*
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void FlushTsIndex()
        {
            FileStream iout = new FileStream(targetDir + "/ts_index.dat", FileMode.Create,
                                             FileAccess.Write, FileShare.ReadWrite);
            iout.Seek(0, SeekOrigin.End);

            BinaryWriter index_bw = new BinaryWriter(iout);

            foreach (TS_Offset tso in ts_index)
            {
                index_bw.Write((Int64)tso.ts);
                index_bw.Write((Int64)tso.offset);
            }

            index_bw.Close();
        }
        */

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Close()
        {
            if (!isClosed)
            {
                Flush();
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

                CloseHelper();
                Sync();
                isClosed = true;
            }
            return isClosed;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DeleteStream()
        {
            if (!AllowWrite())
            {
                throw new InvalidDataException("Not permitted to delete stream");
            }

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

        /* Delete all files in Dir */
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

        public void Dispose() // NOT virtual
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalizer from running.
        }

        protected virtual void Dispose(bool disposing)
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

        ~OldDataFileStream()
        {
            Dispose(false);
        }

    }
}
#endif