using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeOS.Hub.Common.Bolt.DataStore;

using System.Collections.Generic;

namespace HomeOS.Hub.UnitTests.Common.Bolt.DataStore
{
    [TestClass]
    public class ValueDataStreamRangeTest
    {
        IStream dfs_str_val;
        List<IKey> keys;

        [TestInitialize]
        public void Setup()
        {
            StreamFactory sf = StreamFactory.Instance;
            
            dfs_str_val = sf.openValueDataStream<StrKey, StrValue>(new FqStreamID("99-2729", "A0", "TestRange"),
                                                                 new CallerInfo(null, "A0", "A0", 1),
                                                                 null,
                                                                 StreamFactory.StreamSecurityType.Plain,
                                                                 CompressionType.None,
                                                                 StreamFactory.StreamOp.Write);
            keys = new List<IKey>();
            for (int i = 0; i < 10; i++) 
            {
                keys.Add(new StrKey("k" + i));
                for (int j = 0; j < 100; j++)
                {
                    dfs_str_val.Append(keys[i], new StrValue("k" + i + "_value" + j));
                }
      //          dfs_str_val.Seal(null);
            }

        //    IEnumerable<IDataItem> iterator = dfs_str_val.GetAll(new StrKey("k0"));
       //     foreach (IDataItem data in iterator)
        //        Console.WriteLine("data is: " + data.GetTimestamp());

        }

        [TestCleanup]
        public void Cleanup()
        {
            dfs_str_val.Close();
        }

        [TestMethod]
        public void ValueDataRangeTest_TestGetLatestStrValue()
        {
            Assert.IsTrue("k9_value99" == dfs_str_val.GetLatest().Item2.ToString());
        }
    }
}
