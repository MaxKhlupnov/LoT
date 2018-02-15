using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeOS.Hub.Common.Bolt.DataStore;


namespace HomeOS.Hub.UnitTests.Common.Bolt.DataStore
{
    [TestClass]
    public class FileDataStreamTest
    {
        StreamFactory streamFactory;

        IStream dds_byte_val;
        StrKey k1;
        StrKey k2;
        LocationInfo locationInfo;
        FqStreamID streamID;
        CallerInfo callerInfo;
        StreamFactory.StreamSecurityType streamSecurityType;

        [TestInitialize]
        public void Setup()
        {
            k1 = new StrKey("k1");
            k2 = new StrKey("k2");
            string AzureaccountName = "testdrive";
            string AzureaccountKey = "zRTT++dVryOWXJyAM7NM0TuQcu0Y23BgCQfkt7xh2f/Mm+r6c8/XtPTY0xxaF6tPSACJiuACsjotDeNIVyXM8Q==";
            locationInfo = new LocationInfo(AzureaccountName, AzureaccountKey, SynchronizerType.Azure);
            streamID = new FqStreamID("99-a2000", "A0", "TestDS");
            callerInfo = new CallerInfo(null, "A0", "A0", 1);
            streamSecurityType = StreamFactory.StreamSecurityType.Plain;
            streamFactory = StreamFactory.Instance;
        }

        [TestCleanup]
        public void Cleanup()
        {
            dds_byte_val.Close();
        }

        [TestMethod]
        public void DirStreamTest_TestUpdateByteValue()
        {
            dds_byte_val = streamFactory.openFileDataStream<StrKey>(streamID, callerInfo, locationInfo, streamSecurityType, CompressionType.None, StreamFactory.StreamOp.Write);
            dds_byte_val.Update(k1, new ByteValue(StreamFactory.GetBytes("k1-cmu")));
            dds_byte_val.Update(k2, new ByteValue(StreamFactory.GetBytes("k2-msr")));
            dds_byte_val.Update(k1, new ByteValue(StreamFactory.GetBytes("k1-msr")));
        }

        [TestMethod]
        public void DirStreamTest_TestGetByteValue()
        {
            dds_byte_val = streamFactory.openFileDataStream<StrKey>(streamID, callerInfo, locationInfo, streamSecurityType, CompressionType.None, StreamFactory.StreamOp.Read);
            Assert.IsTrue("k1-msr" == dds_byte_val.Get(k1).ToString());
            Assert.IsTrue("k2-msr" == dds_byte_val.Get(k2).ToString());
        }

        [TestMethod]
        public void DirStreamTest_TestGetLatestStrValue()
        {
            dds_byte_val = streamFactory.openFileDataStream<StrKey>(streamID, callerInfo, locationInfo, streamSecurityType, CompressionType.None, StreamFactory.StreamOp.Read);
            Assert.IsTrue("k1-msr" == dds_byte_val.GetLatest().Item2.ToString());
        }
    }
}
