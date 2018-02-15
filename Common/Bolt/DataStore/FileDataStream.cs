using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class FileDataStream<KeyType> : ValueDataStream<KeyType, StrValue>, IDisposable
        where KeyType : IKey, new()
    {
        private bool remoteRead;

        public FileDataStream(Logger Log, FqStreamID FQSID, int num,
            CallerInfo Ci, LocationInfo Li,
            StreamFactory.StreamOp op, StreamFactory.StreamSecurityType type,
            CompressionType ctype, int ChunkSizeForUpload, int UploadThreadPoolSize, 
            string prkey, string pukey, MetaDataService.ACLEntry key_md,
            IndexInfo ii, bool alreadyExists = true)
            : base(Log, FQSID, num, Ci, Li, op, type, ctype, ChunkSizeForUpload, UploadThreadPoolSize,
                      prkey, pukey, key_md, ii, alreadyExists:alreadyExists)
        {
            remoteRead = false;
            // if remote stream, read op. fetch valueFile when needed for get queries
            if (this.synchronizer != null && streamop == StreamFactory.StreamOp.Read)
            {
                remoteRead = true;
            }
        }


        /// <summary>
        /// ValuePath is just the value file name ts.dat and not fully qualified file path
        /// </summary>
        /// <param name="valuePath"></param>
        /// <param name="dbi"></param>
        /// <returns></returns>
        internal ByteValue ReadData(IValue valuePath, DataBlockInfo dbi)
        {
            ByteValue byteValue = null;
            string FQValuePath = targetDir + "/" + valuePath;


            if (logger != null) logger.Log("Start Synchronizer Simple Download");
            if (null != valuePath && remoteRead)
            {
                if (!synchronizer.DownloadFile(valuePath.ToString(), FQValuePath))
                    return byteValue;
            }
            if (logger != null) logger.Log("End Synchronizer Simple Download");

            if (logger != null) logger.Log("Start FileDataStream ReadFromDisk");
            if (null != valuePath) 
            {
                FileStream fout = new FileStream(FQValuePath,
                                                 FileMode.OpenOrCreate,
                                                 FileAccess.Read,
                                                 FileShare.ReadWrite);
                fout.Seek(0, SeekOrigin.Begin);
                byte[] bytes = new byte[fout.Length];
                //read file to MemoryStream
                int bytesRead = 0;
                fout.Read(bytes, bytesRead, (int)fout.Length);
                fout.Close();
                byteValue = new ByteValue(bytes);
            }
            if (logger != null) logger.Log("End FileDataStream ReadFromDisk");
            if (streamtype == StreamFactory.StreamSecurityType.Secure)
            {
                if (logger != null) logger.Log("Start FileDataStream Decrypt DataBlock");
                if (!hasher.ComputeHash(byteValue.GetBytes()).SequenceEqual(dbi.hashValue))
                    return null;

                byteValue = new ByteValue(Crypto.DecryptBytesSimple(byteValue.GetBytes(), Crypto.KeyDer(acl_md.encKey), acl_md.IV));
                if (logger != null) logger.Log("End FileDataStream Decrypt DataBlock");
            }
            return byteValue;
        }

        /*
         * return null if key not found
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public new IValue Get(IKey key)
        {
            if (logger != null) logger.Log("Start FileDataStream Retreive FilePathValue");
            IValue valuePath = base.Get(key, true);
            DataBlockInfo dbi = base.GetDBI(key);
            if (logger != null) logger.Log("End FileDataStream Retreive FilePathValue");
            return ReadData(valuePath, dbi);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public new Tuple<IKey, IValue> GetLatest()
        {
            Tuple<IKey, IValue> latest_kv = base.GetLatest(true);
            return new Tuple<IKey,IValue>(latest_kv.Item1, ReadData(latest_kv.Item2, base.GetDBI(latest_kv.Item1)));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public new Tuple<IValue, long> GetLatest(IKey tag)
        {
            Tuple<IValue, long> latest = base.GetLatest(tag);
            return new Tuple<IValue, long>(ReadData(latest.Item1, base.GetDBI(tag)), latest.Item2);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public new void Append(IKey key, IValue value, long timestamp)
        {
            if (logger != null) logger.Log("Start FileDataStream Append");
            UpdateHelper(key, value, true, timestamp);
            if (logger != null) logger.Log("End FileDataStream Append");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public new void Append(List<IKey> listOfKeys, IValue value)
        {
            StrValue strDataFilePathValue = null;
            byte[] hash = null; 
            long timestamp = StreamFactory.NowUtc();
            foreach (IKey key in listOfKeys)
            {
                if (logger != null) logger.Log("Start ValueDataStream Append");

                if (strDataFilePathValue == null)
                {
                    Tuple<byte[], StrValue> temp = UpdateHelper(key, value, true, timestamp);
                    hash = temp.Item1;
                    strDataFilePathValue = temp.Item2;
                }
                else
                    base.UpdateHelper(key, strDataFilePathValue, true, hash, timestamp);
                if (logger != null) logger.Log("End ValueDataStream Append");
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public new void Append(List<Tuple<IKey, IValue>> list)
        {
            long timestamp = StreamFactory.NowUtc();
            foreach (Tuple<IKey, IValue> keyValPair in list)
            {
                if (logger != null) logger.Log("Start ValueDataStream Append");
                UpdateHelper(keyValPair.Item1, keyValPair.Item2, true, timestamp);
                if (logger != null) logger.Log("End ValueDataStream Append");
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public new void Update(IKey key, IValue value)
        {
            UpdateHelper(key, value, false);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected Tuple<Byte[], StrValue> UpdateHelper(IKey key, IValue value, bool IsAppend, long timestamp = -1)
        {
            if (!value.GetType().Equals(typeof(ByteValue)))
            {
                throw new InvalidDataException("Invalid IValue Type.  ByteValue expected.");
            }

            if (logger != null) logger.Log("Start FileDataStream Delete Old DataBlock");
            // check if the entry is present, so that the old file can be deleted
            IValue valueDataFilePathOld = base.Get(key);
            // remove old entry/file if present and update has been called
            if (valueDataFilePathOld != null && !IsAppend)
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(valueDataFilePathOld.ToString());
                try
                {
                    fi.Delete();
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            if (logger != null) logger.Log("End FileDataStream Delete Old DataBlock");



            if (logger != null) logger.Log("Start FileDataStream Construct DataBlock");
            long ts;  // timestamp 
            ts = StreamFactory.HighResTick();
            string dataFilePath = targetDir + "/" + Convert.ToString(ts) + ".dat";
            StrValue strDataFilePathValue = new StrValue(Convert.ToString(ts) + ".dat");
            if (streamtype == StreamFactory.StreamSecurityType.Secure)
            {
                value = new ByteValue(Crypto.EncryptBytesSimple(value.GetBytes(), Crypto.KeyDer(acl_md.encKey), acl_md.IV));
            }
            else
            {
                value = new ByteValue(value.GetBytes());
            }


            // if (logger != null) logger.Log("Start FileDataStream Construct DBI");
            Byte[] hash;
            if (streamtype == StreamFactory.StreamSecurityType.Secure)
            {
                hash = hasher.ComputeHash(value.GetBytes());
            }
            else
            {
                hash = null;
            }
            // if (logger != null) logger.Log("End FileDataStream Construct DBI");
            if (logger != null) logger.Log("End FileDataStream Construct DataBlock");

            if (logger != null) logger.Log("Start FileDataStream Update FilePathValue");
            base.UpdateHelper(key, strDataFilePathValue, IsAppend, hash, timestamp);
            if (logger != null) logger.Log("End FileDataStream Update FilePathValue");

            // write <val> to file
            if (logger != null) logger.Log("Start FileDataStream WriteToDisc DataBlock");
            FileStream fout = new FileStream(dataFilePath,
                                             FileMode.OpenOrCreate, 
                                             FileAccess.Write, 
                                             FileShare.ReadWrite);
            fout.Write(value.GetBytes(), 0, (int)value.Size());
            fout.Flush(true);
            fout.Close();
            if (logger != null) logger.Log("End FileDataStream WriteToDisc DataBlock");

            return new Tuple<byte[], StrValue>(hash, strDataFilePathValue);
            
        }

        /*
        [MethodImpl(MethodImplOptions.Synchronized)]
        public new void Flush()
        {
            base.Flush();
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public new void Close()
        {
            base.Close();
        }
        */

        /*
        [MethodImpl(MethodImplOptions.Synchronized)]
        public new void DeleteStream()
        {
            base.DeleteStream();
        }
        */

        public new void Dispose() // NOT virtual
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalizer from running.
        }

        protected new virtual void Dispose(bool disposing)
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

        ~FileDataStream()
        {
            Dispose(false);
        }

    }
}
