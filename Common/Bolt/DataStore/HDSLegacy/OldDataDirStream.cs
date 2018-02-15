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

namespace HDS
{
    public class OldDataDirStream<KeyType, ValType> : OldDataFileStream<KeyType, StrValue>, IStream, IDisposable
    {

        public OldDataDirStream(FqStreamID FQSID, StreamFactory.StreamOp Op, CallerInfo Ci, ISync sync)
            : base(FQSID, Op, Ci, sync)
        {
            if (!typeof(IValue).IsAssignableFrom(typeof(ValType)))
            {
                throw new InvalidDataException("ValType must implement IValue");
            }
        }

        // TODO(trinabh): Check ACL stuff here

        internal ByteValue ReadData(IValue valuePath)
        {
            ByteValue byteValue = null;
            if (null != valuePath)
            {
                string dataFilePath = valuePath.ToString();
                FileStream fout = new FileStream(dataFilePath,
                                                 FileMode.OpenOrCreate,
                                                 FileAccess.Read,
                                                 FileShare.ReadWrite);
                fout.Seek(0, SeekOrigin.Begin);
                //create new MemoryStream object
                MemoryStream memStream = new MemoryStream();
                byte[] bytes = new byte[fout.Length];
                //read file to MemoryStream
                int bytesRead = 0;
                fout.Read(bytes, bytesRead, (int)fout.Length);
                fout.Close();
                memStream.Write(bytes, 0, bytes.Length);
                memStream.SetLength(bytes.Length);
                byteValue = SerializerHelper<ByteValue>.DeserializeFromByteStream(memStream);
            }

            return byteValue;
        }

        /*
         * return null if key not found
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public new IValue Get(IKey key)
        {
            if (!base.AllowRead())
            {
                throw new InvalidDataException("Not permitted to read stream");
            }
            IValue valuePath = base.Get(key);
            return ReadData(valuePath);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public new Tuple<IKey, IValue> GetLatest()
        {
            if (!base.AllowRead())
            {
                throw new InvalidDataException("Not permitted to read stream");
            }
            Tuple<IKey, IValue> latest_kv = base.GetLatest();
            return new Tuple<IKey,IValue>(latest_kv.Item1, ReadData(latest_kv.Item2));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public new void Append(IKey key, IValue value)
        {
            if (!base.AllowWrite())
            {
                throw new InvalidDataException("Not permitted to write to stream");
            }
            UpdateHelper(key, value, true);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public new void Update(IKey key, IValue value)
        {
            if (!base.AllowWrite())
            {
                throw new InvalidDataException("Not permitted to write to stream");
            }
            UpdateHelper(key, value, false);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected new void UpdateHelper(IKey key, IValue value, bool IsAppend)
        {
            if (!value.GetType().Equals(typeof(ValType)))
            {
                throw new InvalidDataException("Invalid IValue Type");
            }

            long ts;  // timestamp 

            // check if the entry is present, so that the old file can be deleted
            IValue valueDataFilePathOld = base.Get(key);

            ts = StreamFactory.HighResTick();
            string dataFilePath = targetDir + "/" + Convert.ToString(ts) + ".dat";
            StrValue strDataFilePathValue = new StrValue(dataFilePath);
            
            base.UpdateHelper(key, strDataFilePathValue, IsAppend);

            FileStream fout = new FileStream(dataFilePath,
                                             FileMode.OpenOrCreate, 
                                             FileAccess.Write, 
                                             FileShare.ReadWrite);
            
            // write <val> to file
            MemoryStream memStream = value.SerializeToByteStream();
            fout.Write(memStream.GetBuffer(), 0, (int)memStream.Length);
            fout.Close();

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
        }

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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public new void DeleteStream()
        {
            base.DeleteStream();
        }

        public new void Dispose() // NOT virtual
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalizer from running.
        }

        protected new virtual void Dispose(bool disposing)
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

        ~OldDataDirStream()
        {
            Dispose(false);
        }

    }
}
#endif