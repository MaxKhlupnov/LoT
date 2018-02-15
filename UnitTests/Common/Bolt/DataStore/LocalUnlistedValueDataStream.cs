using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeOS.Hub.Common.Bolt.DataStore;

using System.Collections.Generic;
using System.Collections;

namespace HomeOS.Hub.UnitTests.Common.Bolt.DataStore
{
    [TestClass]
    public class LocalUnlistedValueDataStream
    {
        IStream vds;
        StrKey k1;
        StrKey k2;

        [TestInitialize]
        public void Setup()
        {
            StreamFactory sf = StreamFactory.Instance;
            DateTime Date = new DateTime(DateTime.UtcNow.Ticks);
            string HomeName = String.Format("TestHome-{0}", Date.ToString("yyyy-MM-dd"));
            string Caller = String.Format("{0}", Date.ToString("HH-mm-ss"));
            string AppName = Caller;
            Random rnd = new Random();
            string StreamName = String.Format("{0}", rnd.Next());

            FqStreamID fqstreamid = new FqStreamID(HomeName, AppName, StreamName);
            CallerInfo ci = new CallerInfo(null, Caller, Caller, 1);
            
            sf.deleteStream(fqstreamid, ci);
            
            vds = sf.openValueDataStream<StrKey, ByteValue>(fqstreamid, ci,
                                                                 null,
                                                                 StreamFactory.StreamSecurityType.Plain,
                                                                 CompressionType.None,
                                                                 StreamFactory.StreamOp.Write);

            k1 = new StrKey("k1");
            k2 = new StrKey("k2");
        }

        [TestCleanup]
        public void Cleanup()
        {
            vds.Close();
        }

        [TestMethod]
        public void LocalUnlistedValueDataStreamTest_TestUpdateByteValue()
        {
            vds.Append(k1, new ByteValue(StreamFactory.GetBytes("k1-cmu")));
            vds.Append(k2, new ByteValue(StreamFactory.GetBytes("k2-msr")));
            vds.Append(k1, new ByteValue(StreamFactory.GetBytes("k1-msr")));
        }

        [TestMethod]
        public void LocalUnlistedValueDataStreamTest_TestGetByteValue()
        {
            Assert.IsTrue("k1-msr" == vds.Get(k1).ToString());
            Assert.IsTrue("k2-msr" == vds.Get(k2).ToString());
        }

        [TestMethod]
        public void LocalUnlistedValueDataStreamTest_TestGetAllStrValue()
        {
            IEnumerable<IDataItem> dataItemEnum = vds.GetAll(k1);
            int i = 0;
            foreach (IDataItem di in dataItemEnum)
            {
                switch (i)
                {
                    case 0:
                        Assert.IsTrue("k1-msr" == di.GetVal().ToString());
                        break;
                    case 1:
                        Assert.IsTrue("k1-msr-1" == di.GetVal().ToString());
                        break;
                    case 2:
                        Assert.IsTrue("k1-msr-2" == di.GetVal().ToString());
                        break;
                    default:
                        break;
                }
                i++;
            }
            Assert.IsTrue(i == 3);
        }
    }
}
