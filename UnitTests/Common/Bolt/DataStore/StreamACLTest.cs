using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeOS.Hub.Common.Bolt.DataStore;


namespace HomeOS.Hub.UnitTests.Common.Bolt.DataStore
{
    [TestClass]
    public class StreamACLTest
    {
        [TestMethod]
        public void StreamACLTest_TestAccess()
        {
            /*
            StreamFactory sf = StreamFactory.Instance;
            IStream secure1 = sf.createFileStream<StrKey, ByteValue>(new FqStreamID("99-2713", "A0", "Test"), 
                                                                        StreamFactory.StreamOp.Write, 
                                                                        new CallerInfo(null, "A0", "A0", 1), 
                                                                        null, SynchronizerType.None);
            StrKey key1 = new StrKey("Trinabh");
            secure1.Update(key1, new ByteValue(StreamFactory.GetBytes("Gupta")));
            Console.WriteLine("Trinabh ==> " + secure1.Get(key1));
            secure1.Close();

            try
            {
                IStream secure2 = sf.createFileStream<StrKey, ByteValue>(new FqStreamID("99-2713", "A0", "Test"), StreamFactory.StreamOp.Read, new CallerInfo(null, "A1", "A1", 1), null, SynchronizerType.None);
                StrKey key2 = new StrKey("Trinabh");
                Console.WriteLine("Trinabh ==> " + secure2.Get(key2));
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
            }

            secure1.GrantReadAccess("A1");
            IStream secure3 = sf.createFileStream<StrKey, ByteValue>(new FqStreamID("99-2713", "A0", "Test"), StreamFactory.StreamOp.Read, new CallerInfo(null, "A1", "A1", 1), null, SynchronizerType.None);
            Console.WriteLine("Trinabh ==> " + secure3.Get(key1));
            secure3.Close();

            Console.WriteLine("ACL Testing complete!");
            Console.ReadKey();
             */
        }
    }
}
