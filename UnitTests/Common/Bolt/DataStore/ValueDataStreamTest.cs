using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeOS.Hub.Common.Bolt.DataStore;

using System.Collections.Generic;
using System.Collections;

namespace HomeOS.Hub.UnitTests.Common.Bolt.DataStore
{
    [TestClass]
    public class ValueDataStreamTest
    {
        IStream dfs_byte_val;
        IStream dfs_str_val;
        StrKey k1;
        StrKey k2;

        [TestInitialize]
        public void Setup()
        {
            StreamFactory sf = StreamFactory.Instance;
            
            dfs_byte_val = sf.openValueDataStream<StrKey, ByteValue>(new FqStreamID("99-2729", "A0", "TestBS"),
                                                                 new CallerInfo(null, "A0", "A0", 1),
                                                                 null,
                                                                 StreamFactory.StreamSecurityType.Plain,
                                                                 CompressionType.None,
                                                                 StreamFactory.StreamOp.Write);

            dfs_str_val = sf.openValueDataStream<StrKey, StrValue>(new FqStreamID("99-2729", "A0", "TestSS"),
                                                                 new CallerInfo(null, "A0", "A0", 1),
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
            dfs_byte_val.Close();
            dfs_str_val.Close();
        }

        [TestMethod]
        public void ValueDataStreamTest_TestUpdateByteValue()
        {
            dfs_byte_val.Update(k1, new ByteValue(StreamFactory.GetBytes("k1-cmu")));
            dfs_byte_val.Update(k2, new ByteValue(StreamFactory.GetBytes("k2-msr")));
            dfs_byte_val.Update(k1, new ByteValue(StreamFactory.GetBytes("k1-msr")));
        }

        [TestMethod]
        public void ValueDataStreamTest_TestUpdateStrValue()
        {
            dfs_str_val.Update(k1, new StrValue("k1-cmu"));
            dfs_str_val.Update(k2, new StrValue("k2-msr"));
            dfs_str_val.Update(k1, new StrValue("k1-msr"));
        }

        [TestMethod]
        public void ValueDataStreamTest_TestGetByteValue()
        {
            Assert.IsTrue("k1-msr" == dfs_byte_val.Get(k1).ToString());
            Assert.IsTrue("k2-msr" == dfs_byte_val.Get(k2).ToString());
        }

        [TestMethod]
        public void ValueDataStreamTest_TestGetStrValue()
        {
            Assert.IsTrue("k1-msr" == dfs_str_val.Get(k1).ToString());
            Assert.IsTrue("k2-msr" == dfs_str_val.Get(k2).ToString());
        }

        [TestMethod]
        public void ValueDataStreamTest_TestGetLatestStrValue()
        {
            Assert.IsTrue("k1-msr" == dfs_str_val.GetLatest().Item2.ToString());
        }

        [TestMethod]
        public void ValueDataStreamTest_TestAppendStrValue()
        {
            dfs_str_val.Append(k1, new StrValue("k1-msr-1"));
            dfs_str_val.Append(k1, new StrValue("k1-msr-2"));
        }
        
        [TestMethod]
        public void ValueDataStreamTest_TestGetAllStrValue()
        {
            IEnumerable<IDataItem> dataItemEnum = dfs_str_val.GetAll(k1);
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
