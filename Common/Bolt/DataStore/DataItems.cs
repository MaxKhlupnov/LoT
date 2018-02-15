using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public interface IDataItem
    {
        long GetTimestamp();
        IKey GetKey();
        IValue GetVal();
    }

    public class DataItems<KeyType, ValType> : IEnumerable<IDataItem>
        where KeyType : IKey, new()
        where ValType : IValue, new()
    {
        private List<IDataItem> offsets;
        
        public DataItems(List<DataBlockInfo> tso_list, ValueDataStream<KeyType, ValType> dfs, IKey k)
        {
            offsets = new List<IDataItem>();
            foreach (DataBlockInfo tso in tso_list)
            {
                offsets.Add((IDataItem) new DataItem<KeyType, ValType>(tso.offset, tso.ts, dfs, k));
            }
        }
        /*
        public DataItems(List<DataBlockInfo> tso_list, MetaStream<KeyType, ValType> dfs, IKey k)
        {
            offsets = new List<IDataItem>();
            foreach (DataBlockInfo tso in tso_list)
            {
                offsets.Add((IDataItem) new DataItem<KeyType, ValType>(tso.offset, tso.ts, dfs, k));
            }
        }

        
        public DataItems(List<DataBlockInfo> tso_list, OldDataDirStream<KeyType, ValType> dds, IKey k)
        {
            offsets = new List<IDataItem>();
            foreach (DataBlockInfo tso in tso_list)
            {
                offsets.Add((IDataItem)new DataDirItem<KeyType, ValType>(tso.offset, dds, k));
            }
        }
        */

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public IEnumerator<IDataItem> GetEnumerator()
        {
            return (offsets as IEnumerable<IDataItem>).GetEnumerator();
        }
    }

    public class DataItem<KeyType, ValType> : IDataItem
        where KeyType : IKey, new()
        where ValType : IValue, new()
    {
        private long offset;
        //private MetaStream<KeyType, ValType> mstream;
        private ValueDataStream<KeyType, ValType> estream;
        private bool loaded;

        private long ts;
        private IValue val;
        private IKey key;

        /*public DataItem(long o, long t, MetaStream<KeyType, ValType> dfs, IKey k)
        {
            offset = o;
            mstream = dfs;
            key = k;
            loaded = false;
            ts = t;
        }*/
        
        public DataItem(long o, long t, ValueDataStream<KeyType, ValType> dfs, IKey k)
        {
            offset = o;
            estream = dfs;
            key = k;
            loaded = false;
            ts = t;

        }

        internal void LoadData()
        {
            {
                DataBlock<KeyType, ValType> db;
                db = estream.ReadDataBlock(offset);
                val = db.getValue();
                //ts = db.timestamp;
            }

            loaded = true;
        }
        
        public long GetTimestamp()
        {
            if (!loaded)
            {
                LoadData();
            }
            return ts;
        }

        public IKey GetKey()
        {
            if (!loaded)
            {
                LoadData();
            }
            return key;
        }
        
        public IValue GetVal()
        {
            if (!loaded)
            {
                LoadData();
            }
            return val;
        }
    }

    /*
    public class DataDirItem<KeyType, ValType> : DataItem<KeyType, StrValue>
    {
        private OldDataDirStream<KeyType, ValType> DirStream;

        public DataDirItem(long o, OldDataDirStream<KeyType, ValType> dds, IKey k)
            : base(o, (OldDataFileStream<KeyType, StrValue>)dds, k)
        {
            DirStream = dds;
        }

        public new IValue GetVal()
        {
            IValue valuePath = base.GetVal();
            return DirStream.ReadData(valuePath); 
        }
    }
    */

}
