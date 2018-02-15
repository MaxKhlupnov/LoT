using System;
using System.Collections.Generic;
using System.Configuration;

using HomeOS.Hub.Common.Bolt.DataStore;

namespace HomeOS.Hub.Common.Bolt.Tools.Export2CSV
{
    class Export
    {
        static void Main(string[] args)
        {
            Export e = new Export(true);
        }

        public Export(bool remote)
        {
            IStream datastream;

            string accountName = ConfigurationManager.AppSettings.Get("AccountName");
            string accountKey = ConfigurationManager.AppSettings.Get("AccountSharedKey");
            string homeId = ConfigurationManager.AppSettings.Get("HomeId");
            string appId = ConfigurationManager.AppSettings.Get("AppId");
            string streamId = ConfigurationManager.AppSettings.Get("StreamId");

            StreamFactory sf = StreamFactory.Instance;

            CallerInfo ci = new CallerInfo(null, appId, appId, 0);
            FqStreamID fq_sid = new FqStreamID(homeId, appId, streamId);
            if (remote)
            {
                LocationInfo li = new LocationInfo(accountName, accountKey, SynchronizerType.Azure);
                datastream = sf.openValueDataStream<StrKey, StrValue>
                    (fq_sid, ci, li, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Read,
                    null, 4*1024*1024, 1, null, true);
            }
            else
            {
                datastream = sf.openValueDataStream<StrKey, StrValue>
                    (fq_sid, ci, null, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Read, 
                     null,  4*1024*1024, 1, null);
            }

            /*
            StrKey key = new StrKey("foo");
            if (datastream != null)
            {
                datastream.Append(key, new StrValue("bar"));
                datastream.Append(key, new StrValue("baz"));
            }
             * */

            HashSet<IKey> keys = datastream.GetKeys(null, null);
            foreach (IKey key in keys)
            {
                IEnumerable<IDataItem> dataItemEnum = datastream.GetAll(key);
                foreach (IDataItem di in dataItemEnum)
                {
                    try
                    {
                        DateTime ts = new DateTime(di.GetTimestamp());
                        Console.WriteLine(key + ", " + ts + ", " + di.GetVal().ToString());
                    }
                    catch (Exception e)
                    {
                        Console.Error.Write(e.StackTrace);
                    }
                }
            }

            datastream.Close();
        }
    }
}
