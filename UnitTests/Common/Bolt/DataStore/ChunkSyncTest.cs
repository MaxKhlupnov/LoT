using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeOS.Hub.Common.Bolt.DataStore;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HomeOS.Hub.UnitTests.Common.Bolt.DataStore
{
    [TestClass]
    public class AzureChunkSyncTest
    {
        [TestMethod]
        public void AzureChunkWriteRead()
        {
            string accountName = "testdrive";
            string accountKey = "zRTT++dVryOWXJyAM7NM0TuQcu0Y23BgCQfkt7xh2f/Mm+r6c8/XtPTY0xxaF6tPSACJiuACsjotDeNIVyXM8Q==";
            RemoteInfo ri = new RemoteInfo(accountName, accountKey);

            AzureHelper helper = new AzureHelper(accountName, accountKey, "testuploadchunksfile", CompressionType.None, EncryptionType.None, null, null, new Logger(), 4*1024*1024 , 1);


            helper.UploadFileAsChunks("D:\\testfiles\\testhuge.txt");


            int OFFSET_TO_READ = 4492321;
            int BYTES_TO_READ = 11000;

            List<ChunkInfo> metadata = helper.GetBlobMetadata("testhuge.txt").Item1; 
            Dictionary<int, long> chunkindexandoffsets = helper.GetChunkIndexAndOffsetInChunk(metadata, OFFSET_TO_READ, BYTES_TO_READ);

            byte[] temp = null;
            
            foreach(int chunkIndex in chunkindexandoffsets.Keys)
            {
                if(temp!=null)
                    temp = temp.Concat(helper.DownloadChunk("testhuge.txt", metadata,chunkIndex)).ToArray();
                else
                    temp = helper.DownloadChunk("testhuge.txt", metadata, chunkIndex);
            }

            byte[] test = temp.Skip((int)chunkindexandoffsets.ElementAt(0).Value).Take(BYTES_TO_READ).ToArray();

            byte[] truth = new byte[BYTES_TO_READ];
            using (BinaryReader reader = new BinaryReader(new FileStream("D:\\testfiles\\testhuge.txt", FileMode.Open)))
            {
                reader.BaseStream.Seek(OFFSET_TO_READ, SeekOrigin.Begin);
                reader.Read(truth, 0, BYTES_TO_READ);
            }

            bool arraysAreEqual = Enumerable.SequenceEqual(test, truth);
            Console.WriteLine(arraysAreEqual);
            if (!arraysAreEqual)
                throw new Exception("local and downloaded bits dont match");

        }

        [TestMethod]
        public void AmazonS3ChunkWriteRead()
        {
            string accountName = "";
            string accountKey = "";


            RemoteInfo ri = new RemoteInfo(accountName, accountKey);

            AmazonS3Helper helper = new AmazonS3Helper(new RemoteInfo(accountName, accountKey), "testupl0", CompressionType.GZip, EncryptionType.None, null, null, new Logger(), 4*1024*1024, 1);


            helper.UploadFileAsChunks("D:\\testfiles\\test.txt");
            int OFFSET_TO_READ = 4492321;
            int BYTES_TO_READ = 11000;

            List<ChunkInfo> metadata = helper.GetObjectMetadata("test.txt").Item1;
            Dictionary<int, long> chunkindexandoffsets = helper.GetChunkIndexAndOffsetInChunk(metadata, OFFSET_TO_READ, BYTES_TO_READ);

            byte[] temp = null;

            foreach (int chunkIndex in chunkindexandoffsets.Keys)
            {
                if (temp != null)
                    temp = temp.Concat(helper.DownloadChunk("test.txt", chunkIndex)).ToArray();
                else
                    temp = helper.DownloadChunk("test.txt", chunkIndex);
            }

            byte[] test = temp.Skip((int)chunkindexandoffsets.ElementAt(0).Value).Take(BYTES_TO_READ).ToArray();

            byte[] truth = new byte[BYTES_TO_READ];
            using (BinaryReader reader = new BinaryReader(new FileStream("D:\\testfiles\\test.txt", FileMode.Open)))
            {
                reader.BaseStream.Seek(OFFSET_TO_READ, SeekOrigin.Begin);
                reader.Read(truth, 0, BYTES_TO_READ);
            }

            bool arraysAreEqual = Enumerable.SequenceEqual(test, truth);
            Console.WriteLine(arraysAreEqual);
            if (!arraysAreEqual)
                throw new Exception("local and downloaded bits dont match");

        }
    
    }
}
