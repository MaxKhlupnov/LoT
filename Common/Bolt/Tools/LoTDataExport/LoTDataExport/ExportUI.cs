using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using HomeOS.Hub.Common.Bolt.DataStore;

namespace HomeOS.Hub.Common.Bolt.Tools.LotDataExport
{
    class ExportUI
    {
  
        public ExportUI()
        {
            ;
        }

        private IStream datastream = null;
        public IStream GetDataStream()
        {
            return datastream;
        }
        public void CloseDataStream()
        {
            if (datastream != null)
                datastream.Close();
        }

        public async Task SetupDataStream(bool remote, string accountName, string accountKey, string homeId, string appId, string streamId)
        {

            StreamFactory sf = StreamFactory.Instance;

            CallerInfo ci = new CallerInfo(null, appId, appId, 0);
            FqStreamID fq_sid = new FqStreamID(homeId, appId, streamId);
            if (remote)
            {
                LocationInfo li = new LocationInfo(accountName, accountKey, SynchronizerType.Azure);
                datastream = await Task.Run(() => sf.openValueDataStream<StrKey, StrValue>
                    (fq_sid, ci, li, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Read,
                    null, 4 * 1024 * 1024, 1, null, true));
            }
            else
            {
                datastream = await Task.Run(() => sf.openValueDataStream<StrKey, StrValue>
                    (fq_sid, ci, null, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Read,
                     null, 4 * 1024 * 1024, 1, null));
            }
    
        }

        public HashSet<IKey> GetKeys()
        {
            HashSet<IKey> keys = datastream.GetKeys(null, null);
            return keys;
        }

      
        //Assumes the datastream is setup
        public async Task GetData(HashSet<IKey> keys, DateTime dtbegin, DateTime dtend, String outputFileName)
        {

            if (datastream == null)
                return;

            FileStream fs = new FileStream(outputFileName, FileMode.Append);
            StreamWriter swOut = new StreamWriter(fs);

            DateTime dtbeginutc = dtbegin.ToUniversalTime();
            DateTime dtendutc = dtend.ToUniversalTime();
         
            foreach (IKey key in keys)
            {
                IEnumerable<IDataItem> dataItemEnum = await Task.Run(() => datastream.GetAll(key,
                                                                            dtbeginutc.Ticks,
                                                                            dtendutc.Ticks));
                if (dataItemEnum != null)
                {
                    try
                    {
                        foreach (IDataItem di in dataItemEnum)
                        {
                            DateTime ts = new DateTime(di.GetTimestamp());
                            swOut.WriteLine(key + ", " + ts.ToLocalTime() + ", " + di.GetVal().ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.Write(e.StackTrace);
                    }
                }
            }

            swOut.Close();
        }

      
    }
}



