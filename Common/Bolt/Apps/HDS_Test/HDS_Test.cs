using HomeOS.Hub.Common.Bolt.DataStore;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HomeOS.Hub.Common.Bolt.Apps.Test
{
    class HDS_Test
    {
        private void CleanAccounts(string azureaccountname, string azureaccountkey, string amazons3accountname, string amazons3accountkey)
        {
            AzureHelper helper = new AzureHelper(azureaccountname, azureaccountkey, "foo", CompressionType.None, EncryptionType.None, null, null, new Logger(), 0, 0);
            helper.CleanAccount();
            AmazonS3Helper s3helper = new AmazonS3Helper(new RemoteInfo(amazons3accountname, amazons3accountkey),"foo723r2y3r723rh27r8i", CompressionType.None, EncryptionType.None, null, null, new Logger(), 0, 0);
            s3helper.DeleteAllBuckets();
        }


        static void Main(string[] args)
        {
            string AzureaccountName = ConfigurationManager.AppSettings.Get("AccountName");
            string AzureaccountKey = ConfigurationManager.AppSettings.Get("AccountSharedKey");
            LocationInfo li = new LocationInfo(AzureaccountName, AzureaccountKey, SynchronizerType.Azure);

            /*
            string dataFile = "D:\\b";
            int KB = 1024; 
            int[] chunk_sizes = { 4*1024*KB , 8*1024*KB };

            for (int i = 1; i <= 1; i++)
            {
                for (int threads = 1; threads <= 1; threads++)
                {
                    foreach (int csize in chunk_sizes)
                    {
                        Console.Write(">");
                        File.Copy(dataFile, dataFile + threads + "," + csize);

                        AzureHelper helper = new AzureHelper(AzureaccountName, AzureaccountKey, "foo123123", CompressionType.None, EncryptionType.None, null, null, new Logger(), csize, threads);
                        long start = DateTime.Now.Ticks;
                        helper.UploadFileAsChunks(dataFile + threads + "," + csize);
                        long end = DateTime.Now.Ticks;
                        Console.WriteLine(threads + "," + csize + "," + (((double)(end - start) / (double)10000000)) );
                    }

                }
            }
            */










            li = null;
            FqStreamID fq_sid = new FqStreamID("1299-2716", "A", "TestBS");
            CallerInfo ci = new CallerInfo(null, "A", "A", 1);

            StreamFactory sf = StreamFactory.Instance;
            sf.deleteStream(fq_sid, ci);

            IStream dfs_byte_val = sf.openValueDataStream<StrKey, ByteValue>(fq_sid, ci, li,
                                                                 StreamFactory.StreamSecurityType.Plain,
                                                                 CompressionType.None,
                                                                 StreamFactory.StreamOp.Write);

            StrKey k1 = new StrKey("k1");

           dfs_byte_val.Append(k1, new ByteValue(StreamFactory.GetBytes("k1-cmu")));
           dfs_byte_val.Append(k1, new ByteValue(StreamFactory.GetBytes("k1-msr")));

           dfs_byte_val.Seal(false);
           dfs_byte_val.Append(k1, new ByteValue(StreamFactory.GetBytes("k1-uw")));

            dfs_byte_val.Close();
            Console.ReadKey();

            
            dfs_byte_val = sf.openValueDataStream<StrKey, ByteValue>(fq_sid, ci, li,
                                                                 StreamFactory.StreamSecurityType.Plain,
                                                                 CompressionType.None,
                                                                 StreamFactory.StreamOp.Write);

            
            Console.WriteLine("Get in read : " + dfs_byte_val.Get(k1));
            IEnumerable<IDataItem> data = dfs_byte_val.GetAll(k1, 0, StreamFactory.NowUtc());
            foreach (IDataItem dataItem in data)
                Console.WriteLine(dataItem.GetVal().ToString());

            dfs_byte_val.Close();
            Console.ReadKey();

            /*
            ValueSerializerBase<StrKey> vsb = new ValueSerializerBase<StrKey>();
            Byte[] buffer1 = vsb.SerializeToByteStream().ToArray();
            Byte[] buffer2 = SerializerHelper<StrKey>.SerializeToProtoStream(k1).ToArray();

            FileStream fout = new FileStream("tmp.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            BinaryWriter fs_bw = new BinaryWriter(fout);
            fs_bw.Write(buffer1);
            fs_bw.Write("-----W00t!-----");
            fs_bw.Write(buffer2);
            fs_bw.Write("-----W00t!-----");
            fs_bw.Close();
            fout.Close();
            */
        }
    }
}
