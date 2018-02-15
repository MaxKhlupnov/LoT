using HomeOS.Hub.Common.Bolt.DataStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WriteStreamSyncTester
{
    class Program
    {
        static string AzureaccountName = "";
        static string AzureaccountKey = "";
        static LocationInfo locationInfo =  new LocationInfo(AzureaccountName, AzureaccountKey, SynchronizerType.Azure);
        static int WriteFrequencySeconds = 1;
        static bool isWriting = false; 

        static void Main(string[] args)
        {
            try
            {
                StreamFactory sf = StreamFactory.Instance;
                FqStreamID fqsid = new FqStreamID("new5", "A0", "Test");
                CallerInfo callerinfo = new CallerInfo(null, "A0", "A0", 1);
                String mdserver =  "http://localhost:23456/MetaDataServer/";

                //sf.deleteStream(fqsid, callerinfo, mdserver);
                IStream stream = sf.openValueDataStream<StrKey, ByteValue>(fqsid,
                            callerinfo,
                            locationInfo,
                            StreamFactory.StreamSecurityType.Plain,
                                           CompressionType.None,
                            StreamFactory.StreamOp.Write, mdserver, 4 * 1024 * 1024, 1, null, false, WriteFrequencySeconds);


                Thread writerthread = new Thread(() => Write(stream));
                isWriting = true;
                writerthread.Start();
                Console.WriteLine("Starting Writer .... (press enter to stop) ");
                Console.ReadLine();
                isWriting = false;

                stream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        
        }

        


        private static void Write(IStream stream)
        {
            try
            {
                int i = 1;
                StrKey k1 = k1 = new StrKey("k1");
                while (true)
                {
                    stream.Append(k1, new ByteValue(StreamFactory.GetBytes("k1-value" + i)));
                    i++;

                    Console.WriteLine("Written "+i+" values");
                   if (i %10==0)
                        stream.Seal(false);


                    if (isWriting)
                        System.Threading.Thread.Sleep(1000);
                    else
                        break;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception in write: "+e);
            }

        }


    }
}
