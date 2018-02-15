using HomeOS.Hub.Common.Bolt.DataStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReadStreamSyncTester
{
    class Program
    {
        static string AzureaccountName = "";
        static string AzureaccountKey = "";
        static LocationInfo locationInfo = new LocationInfo(AzureaccountName, AzureaccountKey, SynchronizerType.Azure);
        static int ReadFrequencySeconds = 1;
        static bool isReading = false; 

        static void Main(string[] args)
        {
            StreamFactory sf = StreamFactory.Instance;
            FqStreamID fqsid = new FqStreamID("new5", "A0", "Test");
            CallerInfo callerinfo = new CallerInfo(null, "A0", "A0", 1);
            String mdserver =  "http://localhost:23456/MetaDataServer/";

            //sf.deleteStream(fqsid, callerinfo, mdserver);
            try
            {
                IStream stream = sf.openValueDataStream<StrKey, ByteValue>(fqsid,
                            callerinfo,
                            locationInfo,
                            StreamFactory.StreamSecurityType.Plain,
                                           CompressionType.None,
                            StreamFactory.StreamOp.Read, mdserver, 4 * 1024 * 1024, 1, null, false, ReadFrequencySeconds);


                Thread readerthread = new Thread(() => Read(stream));
                isReading = true;
                Console.WriteLine("Starting Reader .... (press enter to stop)");
                readerthread.Start();
                Console.ReadLine();
                isReading = false;
                stream.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void Read(IStream stream)
        {
            try
            {
                StrKey k1 = k1 = new StrKey("k1");
                while (true)
                {
                    IEnumerable<IDataItem> dataitems= stream.GetAll(k1);

                    DateTime now = DateTime.Now;
                    int count =0;
                    foreach (IDataItem item in dataitems)
                    {
                        item.GetVal();
                        count++;
                    }
                    
                    Console.WriteLine("[" + now + "]" + "GetAll "+count+" values received.");

                    if (isReading)
                        System.Threading.Thread.Sleep(ReadFrequencySeconds * 1000);
                    else
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in read: " + e);
            }

        }

    }
}
