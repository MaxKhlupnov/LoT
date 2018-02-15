using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeOS.Hub.Common.Bolt.DataStore;

using System.Collections.Generic;
using System.Collections;
using System.Threading;

namespace HomeOS.Hub.UnitTests.Common.Bolt.DataStore
{
    [TestClass]
    public class SyncFileStreamTest
    {
        StrKey k1;
        StrKey k2;
        LocationInfo locationInfo;
        [TestInitialize]
        public void Setup()
        {
            k1 = new StrKey("k1");
            k2 = new StrKey("k2");
            string AzureaccountName = "testdrive";
            string AzureaccountKey = "zRTT++dVryOWXJyAM7NM0TuQcu0Y23BgCQfkt7xh2f/Mm+r6c8/XtPTY0xxaF6tPSACJiuACsjotDeNIVyXM8Q==";
            locationInfo = new LocationInfo(AzureaccountName, AzureaccountKey, SynchronizerType.Azure);
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void SyncFileStreamTest_TestRepeatedClose()
        {
            for (int i = 0; i < 10; ++i)
            {
                StreamFactory sf = StreamFactory.Instance;
                IStream dfs_byte_val = sf.openValueDataStream<StrKey, ByteValue>(new FqStreamID("99-2729", "A0", "TestMultiClose"),
                            new CallerInfo(null, "A0", "A0", 1),
                            locationInfo,
                            StreamFactory.StreamSecurityType.Plain,
                                           CompressionType.None,
                            StreamFactory.StreamOp.Write);
                dfs_byte_val.Append(k1, new ByteValue(StreamFactory.GetBytes("k1-cmu-" + i)));
                dfs_byte_val.Close();
                Thread.Sleep(5000);
            }
        }
    }
}
