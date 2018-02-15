using System;

using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Files;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using HomeOS.Hub.Common.Bolt.DataStore;

using System.Diagnostics;
using Amazon.S3.Model;
using Amazon.S3;

namespace HomeOS.Hub.Common.Bolt.Apps.Eval
{
    public enum StreamType : byte { Local = 0, LocalEnc = 1, Remote = 2, RemoteEnc = 3, Raw = 4, DiskRaw = 5, CloudRaw = 6}
    public enum StreamOperation : byte { 
        SameKeySameValueAppend = 0,
        SameKeyRandomValueAppend = 1,
        RandomKeyRandomValueAppend = 2,
        RandomKeySameValueAppend = 3,
        RandomKeyGet = 4,
        RandomKeyGetAll = 5,
        RandomKeyGetMultipleSegments = 6,
    }
    
    public class Experiment
    {

        public string exp_directory;
        public string exp_id {get; set;}
        public string compressed_exp_id;
        public Experiment()
        {
            exp_id = null;
            compressed_exp_id = null;
            exp_directory = null;
        }

        ~Experiment()
        {
            GC.Collect();
        }
        
        public void Destroy()
        {
            GC.Collect();
        }

        public override string ToString()
        {
            return compressed_exp_id;
        }

        public static Logger doDiskRaw(StreamOperation op, 
                    int num_operations, int val_size, 
                    StreamFactory.StreamDataType ptype,
                    string exp_dir)
        {
            Logger logger = new Logger();
            if (op == StreamOperation.RandomKeyRandomValueAppend || 
                op == StreamOperation.SameKeyRandomValueAppend || 
                op == StreamOperation.RandomKeySameValueAppend ||
                op == StreamOperation.SameKeySameValueAppend)
            {
                
                // Populate the keys and the values
                Random random = new Random(DateTime.Now.Millisecond);
                StrKey[] keys = new StrKey[num_operations];
                for (int i = 0; i < num_operations; ++i)
                {
                    keys[i] = new StrKey("" + i);
                }

                Byte[] singleval = new Byte[val_size];
                random.NextBytes(singleval);
                Byte[][] tmp = new Byte[num_operations][];
                if (!(op == StreamOperation.RandomKeySameValueAppend ||
                    op == StreamOperation.SameKeySameValueAppend))
                {

                    for (int i = 0; i < num_operations; ++i)
                    {
                        tmp[i] = new Byte[val_size];
                        random.NextBytes(tmp[i]);
                    }
                }

                StrKey key = new StrKey("ExpKey");
                if (ptype == StreamFactory.StreamDataType.Values)
                {
                    // ValueDataStream type
                    logger.Log("Start Stream Open");
                    FileStream fout = new FileStream(exp_dir + "/DiskRaw", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                    fout.Seek(0, SeekOrigin.End);
                    BinaryWriter fs_bw = new BinaryWriter(fout);
                    logger.Log("End Stream Open");
                    // TimeSpan span = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                    // Console.WriteLine("Time" + span.TotalSeconds);
                    logger.Log("Start Stream Append");
                    for (int i = 0; i < num_operations; ++i)
                    {
                        fs_bw.BaseStream.Seek(0, SeekOrigin.End);
                        /*
                        if (op == StreamOperation.SameKeySameValueAppend || op == StreamOperation.SameKeyRandomValueAppend)
                        {
                            fs_bw.Write(key.key);
                        }
                        else
                        {
                        */
                            fs_bw.Write(keys[i].key);
                        /*
                        }
                        */
                        fs_bw.Write(StreamFactory.NowUtc());
                        /*
                        if (!(op == StreamOperation.RandomKeySameValueAppend ||
                            op == StreamOperation.SameKeySameValueAppend))
                            fs_bw.Write(tmp[i]);
                        else
                        */
                            fs_bw.Write(singleval);
                        fs_bw.Flush();
                    }
                    fout.Flush(true);
                    fs_bw.Close();
                    // span = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                    // Console.WriteLine("Time" + span.TotalSeconds);
                    logger.Log("End Stream Append");
                }
                else
                {
                    string[] filenames = new string[num_operations];
                    for (int i = 0; i < num_operations; ++i)
                    {
                        filenames[i] = exp_dir + "/DiskRaw" + i;
                    }
                    // FileDataStream type
                    logger.Log("Start Stream Append");
                    for (int i = 0; i < num_operations; ++i)
                    {
                        FileStream fout = new FileStream(filenames[i], FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                        // fout.Seek(0, SeekOrigin.End);
                        BinaryWriter fs_bw = new BinaryWriter(fout);
                        // fs_bw.BaseStream.Seek(0, SeekOrigin.End);
                        /*
                        if (op == StreamOperation.SameKeySameValueAppend || op == StreamOperation.SameKeyRandomValueAppend)
                        {
                            fs_bw.Write(key.key);
                        }
                        else
                        {
                        */
                            fs_bw.Write(keys[i].key);
                        // }
                        fs_bw.Write(StreamFactory.NowUtc());
                        /*
                        if (!(op == StreamOperation.RandomKeySameValueAppend ||
                            op == StreamOperation.SameKeySameValueAppend))
                            fs_bw.Write(tmp[i]);
                        else
                        */
                            fs_bw.Write(singleval);
                        fout.Flush(true);
                        fs_bw.Close();
                    }
                    logger.Log("End Stream Append");
                }
            }
            else
            {
                // Populate the keys and the values
                Random random = new Random();
                Byte[] val = new Byte[val_size];

                if (ptype == StreamFactory.StreamDataType.Values)
                {
                    long[] offsets = new long[num_operations];
                    for (int i = 0; i < num_operations; ++i)
                    {
                        offsets[i] = random.Next(0, num_operations - 1) * val_size;
                    }
                    // ValueDataStream type
                    logger.Log("Start Stream Open");
                    FileStream fin = new FileStream(exp_dir + "/DiskRaw", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    BinaryReader fs_br = new BinaryReader(fin);
                    logger.Log("End Stream Open");
                    logger.Log("Start Stream Get");
                    logger.Log("Start Stream GetAll");
                    for (int i = 0; i < num_operations; ++i)
                    {
                        fs_br.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
                        fs_br.Read(val, 0, val_size);
                    }
                    fs_br.Close();
                    logger.Log("End Stream Get");
                    logger.Log("End Stream GetAll");
                }
                else
                {
                    string[] filenames = new string[num_operations];
                    for (int i = 0; i < num_operations; ++i)
                    {
                        filenames[i] = exp_dir + "/DiskRaw" + random.Next(0, num_operations);
                    }
                    // FileDataStream type
                    logger.Log("Start Stream Get");
                    logger.Log("Start Stream GetAll");
                    for (int i = 0; i < num_operations; ++i)
                    {
                        FileStream fin = new FileStream(filenames[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        BinaryReader fs_br = new BinaryReader(fin);
                        fs_br.Read(val, 0, val_size);
                        fs_br.Close();
                    }
                    logger.Log("End Stream Get");
                    logger.Log("End Stream GetAll");
                }
            }
            CostsHelper costhelper = new CostsHelper();
            logger.Log(DateTime.UtcNow.Ticks + ": DataRelated Storage: " + costhelper.getStorageUsage(exp_dir, dataRelated:true)/1000.0f);
            logger.Log(DateTime.UtcNow.Ticks + ": Constant Storage: " + costhelper.getStorageUsage(exp_dir, dataRelated:false)/1000.0f);
            return logger;
        }

        public static string doDiskSpeed(string filesize, string blocksize, StreamFactory.StreamOp op)
        {
            // For the example
            // Use ProcessStartInfo class
            try
            {
                File.Delete("testfile.dat");
            }
            catch
            {
            }
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "DiskSpd.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = " -UN " ;

            if (op == StreamFactory.StreamOp.Write)
                startInfo.Arguments += " -w ";

            startInfo.Arguments += " -b" + blocksize; 
            startInfo.Arguments += " -c" + filesize;
            startInfo.Arguments += " -d5";
            startInfo.Arguments += " testfile.dat";
            startInfo.RedirectStandardOutput = true;

            string ret = "";

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    ret = exeProcess.StandardOutput.ReadToEnd();
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception {0}", e);
                // Log error.
            }
            return ret;
        }

        private static Random _random = new Random(Environment.TickCount);

        long RandomLong(long min, long max, Random rand)
        {
            if (min == max)
                return min;
            byte[] buf = new byte[8];
            rand.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min)) + min);
        }

        public static string RandomString(int length)
        {
            string chars = "abcdefghijklmnopqrstuvwxyz";
            StringBuilder builder = new StringBuilder(length);

            for (int i = 0; i < length; ++i)
                builder.Append(chars[_random.Next(chars.Length)]);

            return builder.ToString();
        }

        /*
         Sample call for upload:-
            byte[] array = new byte[1024*1024*1024];
            Random random = new Random();
	        random.NextBytes(array);
            double timeTaken_Upload = Experiment.doRawCloudPerf(array, SynchronizerType.Azure, SynchronizeDirection.Upload, "fooContainer", "fooBlob");
            double timeTaken_Download = Experiment.doRawCloudPerf(array, SynchronizerType.Azure, SynchronizeDirection.Download, "fooContainer", "fooBlob");
         * 
         * 
         */
        public static double doRawCloudPerf(byte[] input, SynchronizerType synchronizerType, 
            SynchronizeDirection syncDirection, string exp_directory, Logger logger, string containerName=null, string blobName=null)
        {

            string accountName = ConfigurationManager.AppSettings.Get("AccountName");
            string accountKey = ConfigurationManager.AppSettings.Get("AccountSharedKey");

            DateTime begin=DateTime.Now, end=DateTime.Now;

            if (synchronizerType == SynchronizerType.Azure)
            {
                #region azure download/upload
                if (containerName==null)
                    containerName = "testingraw";
                if(blobName==null)
                    blobName = Guid.NewGuid().ToString();

                CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentialsAccountAndKey(accountName, accountKey), true);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);

                if (syncDirection == SynchronizeDirection.Upload)
                {
                    logger.Log("Start Stream Append");
                    container.CreateIfNotExist();
                    begin = DateTime.UtcNow;//////////////////////////////////////
                    try
                    {
                        using (MemoryStream memoryStream = new System.IO.MemoryStream(input))
                        {
                            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                            blockBlob.UploadFromStream(memoryStream);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("exception: " + e);
                    }
                    end = DateTime.UtcNow;//////////////////////////////////////
                    logger.Log("End Stream Append");
                }

                if (syncDirection == SynchronizeDirection.Download)
                {
                    logger.Log("Start Stream Get");
                    logger.Log("Start Stream GetAll");
                    try
                    {
                        CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                        byte[] blobContents = blockBlob.DownloadByteArray();

                        //if (File.Exists(blobName))
                        //   File.Delete(blobName);

                        begin = DateTime.UtcNow;//////////////////////////////////////
                        // using (FileStream fs = new FileStream(blobName, FileMode.OpenOrCreate))
                        // {
                            byte[] contents = blockBlob.DownloadByteArray();
                            // fs.Write(contents, 0, contents.Length);
                        // }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("exception: " + e);
                    }
                    end = DateTime.UtcNow;//////////////////////////////////////
                    logger.Log("End Stream Get");
                    logger.Log("End Stream GetAll");

                }
                #endregion

            }

            else if (synchronizerType == SynchronizerType.AmazonS3)
            {
                #region amazon s3 stuff
                if (containerName == null)
                    containerName = "testingraw";
                if (blobName == null)
                    blobName = Guid.NewGuid().ToString();

                AmazonS3Client amazonS3Client = new AmazonS3Client(accountName, accountKey);
                if (syncDirection == SynchronizeDirection.Upload)
                {
                    ListBucketsResponse response = amazonS3Client.ListBuckets();
                    foreach (S3Bucket bucket in response.Buckets)
                    {
                        if (bucket.BucketName == containerName)
                        {
                            break;
                        }
                    }
                    amazonS3Client.PutBucket(new PutBucketRequest().WithBucketName(containerName));

                    begin = DateTime.UtcNow;//////////////////////////////////////
                    MemoryStream ms = new MemoryStream();
                    ms.Write(input, 0, input.Length);
                    PutObjectRequest request = new PutObjectRequest();
                    request.WithBucketName(containerName);
                    request.WithKey(blobName);
                    request.InputStream = ms;
                    amazonS3Client.PutObject(request);
                    end = DateTime.UtcNow;//////////////////////////////////////

                }

                if (syncDirection == SynchronizeDirection.Download)
                {
                    if (File.Exists(blobName))
                        File.Delete(blobName);

                    begin = DateTime.UtcNow;//////////////////////////////////////
                    GetObjectRequest request = new GetObjectRequest();
                    request.WithBucketName(containerName);
                    request.WithKey(blobName);
                    GetObjectResponse response = amazonS3Client.GetObject(request);
                    var localFileStream = File.Create(blobName);
                    response.ResponseStream.CopyTo(localFileStream); 
                    localFileStream.Close();
                    end = DateTime.UtcNow;//////////////////////////////////////
                }
#endregion
            }
            else
            {
                throw new InvalidDataException("syncronizer type is not valid");
            }

            return (end - begin).TotalMilliseconds;// return total time to upload in milliseconds

        }

        public List<string> Dump(string CallerName,
                        string HomeName, string AppName, string StreamName, 
                        StreamType stream_type,
                        StreamOperation stream_op,
                        StreamFactory.StreamDataType ptype,
                        Byte[] value, int num_operations,
                        SynchronizerType synctype,
                        bool doCosts= false)
        {
            List<string> exp_details = new List<string>();
            exp_details.Add(exp_id);
            File.AppendAllLines(exp_directory + "/exp", exp_details);
            return exp_details;
        }
        
        public long GetTimeSpentTotal(string task, string[] log, bool once = false)
        {
            long start = 0;
            long end = 0;
            for (int i = 0; i < log.Length; ++i)
            {
                if (log[i].Contains("Start " + task) == true)
                {
                    char[] delimiterChars = { ':' };
                    start = Convert.ToInt64(log[i].Split(delimiterChars)[0]);
                    break;
                }
            }
            for (int i = log.Length - 1; i > 0; --i)
            {
                if (log[i].Contains("End " + task) == true)
                {
                    char[] delimiterChars = { ':' };
                    end = Convert.ToInt64(log[i].Split(delimiterChars)[0]);
                    break;
                }
            }
            return end-start;
        }
        
        public long GetCount(string task, string[] log, bool once = false)
        {
            long ret = 0;
            for (int i = 0; i < log.Length; ++i)
            {
                if (log[i].Contains("Start " + task) == true)
                {
                    ret += 1;
                }
            }
            return ret;
        }

        public long GetTimeSpentForAtomic(string task, string[] log, bool once = false)
        {
            long ret = 0;
            for (int i = 0; i < log.Length; ++i)
            {
                if (log[i].Contains("Start " + task) == true)
                {
                    char[] delimiterChars = { ':' };
                    long start = Convert.ToInt64(log[i].Split(delimiterChars)[0]);
                    long end = start;
                    int j = 0;
                    for (j = i + 1; j < log.Length; ++j)
                    {
                        if (log[j].Contains("End " + task) == true)
                        {
                            end = Convert.ToInt64(log[j].Split(delimiterChars)[0]);
                            break;
                        }
                    }
                    i = j;
                    ret += (end - start);
                    if (once == true)
                        return ret;
                }
            }
            return ret;
        }

        public string GetUsage(string task, string[] log)
        {
            for (int i = 0; i < log.Length; ++i)
            {
                if (log[i].Contains(task) == true)
                {
                    return log[i];
                }
            }
            return "";
        }
        
        public List<string> Analyze(
                        StreamType stream_type,
                        Byte[] value, int num_operations,
                        bool doCosts= false)
        {
            string[] exp_data = File.ReadAllLines(exp_directory + "/log");
            List<string> results = new List<string>();
            
            // Special treatment for raw disk throughput numbers
            if (stream_type == StreamType.Raw)
                return exp_data.ToList();


            char[] delimiterChars = { ':' };
            long start = Convert.ToInt64(exp_data[0].Split(delimiterChars)[0]);
            long end = Convert.ToInt64(exp_data[exp_data.Length - 1].Split(delimiterChars)[0]);
            
            long time_ms = (end - start);
            results.Add("1. Total time ms: " + (time_ms/TimeSpan.TicksPerMillisecond));
            
            // During Open
            results.Add("2. Stream Init DataStructures: " + GetTimeSpentForAtomic("Stream Init DataStructures", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("3. Stream Fill IndexMD Location: " + GetTimeSpentForAtomic("Stream Check Exists, Fill IndexMD Location", exp_data) / TimeSpan.TicksPerMillisecond);
            // If creating from scratch
            results.Add("4. CreateStream(): " + GetTimeSpentForAtomic("Stream Create Stream", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("5. Stream Store IndexMD Location: " + GetTimeSpentForAtomic("Stream Store IndexMD Location", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("6. Stream Gen Pub-Pri Key: " + GetTimeSpentForAtomic("Stream Generate Pub-Pri Key Pair", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("7. Stream Gen KeyRegression: " + GetTimeSpentForAtomic("Stream Generate Key Regresion Chain", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("8. Stream Store Content Key: " + GetTimeSpentForAtomic("Stream Store Content Key", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("9. CreatePart(): " + GetTimeSpentForAtomic("Segment Create", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("10. Store Segment Location: " + GetTimeSpentForAtomic("Segment Store Location Info", exp_data) / TimeSpan.TicksPerMillisecond);
            // Or opening already existing
            results.Add("11. Stream Download IndexMD: " + GetTimeSpentForAtomic("Stream Download IndexMD", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("12. Stream Get Pub-Pri Key: " + GetTimeSpentForAtomic("Stream Get Pub-Pri Keys", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("13. Stream Get Content Key: " + GetTimeSpentForAtomic("Stream Get Content Key", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("14. Stream Verify, Fill IndexMD: " + GetTimeSpentForAtomic("Stream Verify, Fill IndexMD", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("15. Stream Get Segments Locations: " + GetTimeSpentForAtomic("Stream Get Segment Location Infos", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("16. OpenPart(): " + GetTimeSpentForAtomic("Segment Open", exp_data) / TimeSpan.TicksPerMillisecond);
            // If opening a segment
            results.Add("17. VDS Init DataStructures: " + GetTimeSpentForAtomic("ValueDataStream Init DataStructures", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("18. VDS Fetch Data at Open: " + GetTimeSpentForAtomic("ValueDataStream Fetch Data At Open", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("19. VDS Build Index: " + GetTimeSpentForAtomic("ValueDataStream Build Index", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("20. VDS Verify Integrity: " + GetTimeSpentForAtomic("ValueDataStream Verify Integrity", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("21. VDS ReadFromDisk Open: " + GetTimeSpentForAtomic("ValueDataStream ReadFromDisk Open", exp_data) / TimeSpan.TicksPerMillisecond);
            
            // For writes
            results.Add("22. Appending: " + GetTimeSpentForAtomic("Stream Append", exp_data)/TimeSpan.TicksPerMillisecond);
            // For FileDataStream appends 
            results.Add("23. FDS Constructing and Encrypt, Hash DB: " + GetTimeSpentForAtomic("FileDataStream Construct DataBlock", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("24. FDS Construct DBI: " + GetTimeSpentForAtomic("FileDataStream Construct DBI", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("25. FDS Update FileValuePath: " + GetTimeSpentForAtomic("FileDataStream Update FilePathValue", exp_data)/TimeSpan.TicksPerMillisecond);
            // For ValueDataStream appends 
            results.Add("26. VDS Index Tag Lookup: " + GetTimeSpentForAtomic("ValueDataStream Tag Lookup", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("27. VDS Constructing DB: " + GetTimeSpentForAtomic("ValueDataStream Construct DataBlock", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("28. VDS Serializing DB: " + GetTimeSpentForAtomic("ValueDataStream Serialize DataBlock", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("29. VDS Writing DB: " + GetTimeSpentForAtomic("ValueDataStream WriteToDisc DataBlock", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("30. VDS Add DBI: " + GetTimeSpentForAtomic("ValueDataStream Construct and Add DataBlockInfo", exp_data) / TimeSpan.TicksPerMillisecond);
            // End ValueDataStream appends 
            results.Add("31. FDS Writing DB: " + GetTimeSpentForAtomic("FileDataStream WriteToDisc DataBlock", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("32. FDS Delete Old DB: " + GetTimeSpentForAtomic("FileDataStream Delete Old DataBlock", exp_data)/TimeSpan.TicksPerMillisecond);
            
            // During close
            results.Add("33. Stream Closing: " + GetTimeSpentForAtomic("Stream Close", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("34. VDS Close: " + GetTimeSpentForAtomic("ValueDataStream Close", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("35. VDS fs_bw close: " + GetTimeSpentForAtomic("ValueDataStream File Close", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("36. VDS Flush: " + GetTimeSpentForAtomic("ValueDataStream Flush", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("37. VDS Sync: " + GetTimeSpentForAtomic("ValueDataStream Sync", exp_data)/TimeSpan.TicksPerMillisecond);
            // results.Add("38. Sync Enlist Files: " + GetTimeSpentForAtomic("Synchronizer Enlisting Files", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("38. Sync Upload FDSFiles: " + GetTimeSpentForAtomic("Synchronizer Upload FDSFiles", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("39. Sync Upload Simple File: " + GetTimeSpentForAtomic("Synchronizer Upload FileSimple", exp_data, once:true) / TimeSpan.TicksPerMillisecond);
            results.Add("40. Sync Upload Chunked File: " + GetTimeSpentForAtomic("Synchronizer Upload FileAsChunks", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("41. Sync Check Blob Exists: " + GetTimeSpentForAtomic("Synchronizer Check Blob Exist", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("42. Sync Fill Remote ChunkList: " + GetTimeSpentForAtomic("Synchronizer Fill Remote ChunkList", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("43. Sync Fill Local ChunkList: " + GetTimeSpentForAtomic("Synchronizer Fill Local ChunkList", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("44. Sync ChunkList Compare: " + GetTimeSpentForAtomic("Synchronizer ChunkList Compare", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("45. Sync Upload Multiple Chunks: " + GetTimeSpentForAtomic("Synchronizer Upload Multiple Chunks", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("46. Sync Read Chunk From File: " + GetTimeSpentForAtomic("Synchronizer Read Chunk From File", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("47. Sync Compress Chunk: " + GetTimeSpentForAtomic("Synchronizer Compress Chunk", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("48. Sync Encrypt Chunk: " + GetTimeSpentForAtomic("Synchronizer Encrypt Chunk", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("49. Sync Upload Chunk: " + GetTimeSpentTotal("Synchronizer Upload Chunk", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("50. Sync Commit BlockList: " + GetTimeSpentForAtomic("Synchronizer Commit BlockList", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("51. Sync Upload ChunkList: " + GetTimeSpentForAtomic("Synchronizer ChunkList Upload", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("52. Stream IndexMD Flush: " + GetTimeSpentForAtomic("IndexMD Flush", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("53. Stream Sync: " + GetTimeSpentForAtomic("Stream Sync", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("54. StreamMD.dat Flush: " + GetTimeSpentForAtomic("StreamMD Flush", exp_data)/TimeSpan.TicksPerMillisecond);
            
            // Costs
            results.Add("55. " + GetUsage("CPU", exp_data));
            results.Add("56. " + GetUsage("Network", exp_data));
            results.Add("57. " + GetUsage("DataRelated Storage", exp_data));
            results.Add("58. " + GetUsage("Constant Storage", exp_data));

            // For Reads (Get) 
            results.Add("59. Getting: " + GetTimeSpentForAtomic("Stream Get", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("60. VDS Lookup offset: " + GetTimeSpentForAtomic("ValueDataStream Get Offset", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("61. Sync Chunking: " + GetTimeSpentForAtomic("Synchronizer Chunking", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("62. Sync Decompress Chunk: " + GetTimeSpentForAtomic("Synchronizer Decompress Chunk", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("63. Sync Decrypt Chunk: " + GetTimeSpentForAtomic("Synchronizer Decrypt Chunk", exp_data) / TimeSpan.TicksPerMillisecond);
            results.Add("64. VDS DeSerializing DB: " + GetTimeSpentForAtomic("ValueDataStream Deserialize DataBlock", exp_data)/TimeSpan.TicksPerMillisecond);
            // For FileDataStream reads 
            results.Add("65. FDS Simple Download: " + GetTimeSpentForAtomic("Synchronizer Simple Download", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("66. FDS Reading DB: " + GetTimeSpentForAtomic("FileDataStream ReadFromDisk", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("67. FDS Decrypt DB: " + GetTimeSpentForAtomic("FileDataStream Decrypt DataBlock", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("68. VDS Reading from disk: " + GetTimeSpentForAtomic("ValueDataStream ReadFromDisk", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("69. FDS Retreive FilePathValue: " + GetTimeSpentForAtomic("FileDataStream Retreive FilePathValue", exp_data)/TimeSpan.TicksPerMillisecond);
            
            
            // For Reads (GetAll) 
            results.Add("70. Getting All: " + GetTimeSpentForAtomic("Stream GetAll", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("71. Getting DB: " + GetTimeSpentForAtomic("ValueDataStream GetDB", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("72. Actually downloading chunk: " + GetTimeSpentForAtomic("Synchronizer Actually Downloading Chunk", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("73. Updating cache: " + GetTimeSpentForAtomic("Synchronizer Update Chunk Cache", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("74. Reading from cache: " + GetTimeSpentForAtomic("Synchronizer ReadFromCache", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("75. Reading from cache Build Filename: " + GetTimeSpentForAtomic("ReadFromCache Build FileName", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("76. Reading from cache read from disk open: " + GetTimeSpentForAtomic("ReadFromCache Open", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("77. Reading from cache read from disk total read: " + GetTimeSpentForAtomic("ReadFromCache ReadAllBytes", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("78. Create chunkcache dir: " + GetTimeSpentForAtomic("UpdateChunkCache Create Directory", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("79. Write chunk to chunkcache: " + GetTimeSpentForAtomic("UpdateChunkCache WriteChunk", exp_data)/TimeSpan.TicksPerMillisecond);
            results.Add("80. Updating cache count: " + GetCount("Synchronizer Update Chunk Cache", exp_data));
            results.Add("81. UploadChunkList create and queue threads: " + GetCount("UploadChunkList Create and Queue Threads", exp_data));
            
            File.AppendAllLines(exp_directory + "/results", results);
            
            return results;
        }

        public void Run(string CallerName,
                        string HomeName, string AppName, string StreamName, 
                        string RandName, 
                        long stime, long etime,
                        StreamType stream_type,
                        StreamOperation stream_op,
                        StreamFactory.StreamDataType ptype,
                        CompressionType ctype, int ChunkSize , int ThreadPoolSize, 
                        Byte[] value, int num_operations,
                        SynchronizerType synctype,
                        int max_key = 0,
                        string address = null,
                        bool doCosts= false,
                        bool doRaw = false)
        {
            // Set experiment directory
            CallerInfo ci = new CallerInfo(null, CallerName, CallerName, 1);
            exp_directory = Path.GetFullPath((null != ci.workingDir) ? ci.workingDir : Directory.GetCurrentDirectory());
            exp_directory = exp_directory + "/" + HomeName + "/" + AppName + "/" + StreamName;

            if (max_key == 0)
                max_key = num_operations;

            // Set a description/tag for the experiment
            this.exp_id = "Directory: " + HomeName + "/" + AppName + "/" + StreamName +
                " Caller:" + CallerName
                + " Stream Type:" + stream_type + " Stream Op: " + stream_op + " Stream Ptype: " + ptype + " Compression Type: " + ctype
                + " Value size: " + value.Length
                + " num_operations: " + max_key 
                + " actual_num_ops: " + num_operations 
                + " Sync type: " + synctype
                + " Do costs? " + doCosts + "Chunk Size: " + ChunkSize+ " ThreadPool Size:" +ThreadPoolSize;

            this.compressed_exp_id =
                " ST:" + stream_type + " OP: " + stream_op + " PT: " + ptype + " CT: " + ctype
                + " VS: " + value.Length
                + " I:" + num_operations
                + " MK:" + max_key 
                + " SYNC: " + synctype+ " chsize: "+ChunkSize + " nThreads: "+ThreadPoolSize  ;

            // Set remote storage server account info
            string AzureaccountName = ConfigurationManager.AppSettings.Get("AccountName");
            string AzureaccountKey = ConfigurationManager.AppSettings.Get("AccountSharedKey");

            string S3accountName = ConfigurationManager.AppSettings.Get("S3AccountName");
            string S3accountKey = ConfigurationManager.AppSettings.Get("S3AccountSharedKey");

            LocationInfo Li;
            if (synctype == SynchronizerType.Azure)
                Li = new LocationInfo(AzureaccountName, AzureaccountKey, SynchronizerType.Azure);
            else if (synctype == SynchronizerType.AmazonS3)
                Li = new LocationInfo(S3accountName, S3accountKey, SynchronizerType.AmazonS3);
            else
                Li = null;
            
            StreamFactory sf = StreamFactory.Instance;

            IStream stream = null;
            FqStreamID streamid = new FqStreamID(HomeName, AppName, StreamName);

            // Set op : R/W
            StreamFactory.StreamOp rw;
            if (stream_op == StreamOperation.RandomKeyRandomValueAppend
                || stream_op == StreamOperation.RandomKeySameValueAppend
                || stream_op == StreamOperation.SameKeyRandomValueAppend
                || stream_op == StreamOperation.SameKeySameValueAppend)
            {
                rw = StreamFactory.StreamOp.Write;
            }
            else
            {
                rw = StreamFactory.StreamOp.Read;
            }

            // Initialize costs
            CostsHelper costhelper = null;
            double baselineStorageKV = 0;
            if (doCosts)
            {
                costhelper = new CostsHelper();
                costhelper.getCurrentCpuUsage();
                costhelper.getNetworkUsage();
            }
            
            if (stream_type == StreamType.CloudRaw)
            {
                if (!Directory.Exists(exp_directory))
                {
                    Directory.CreateDirectory(exp_directory);
                }
                Logger logger = new Logger();
                Byte[] val = new Byte[value.Length * num_operations];
                // DateTime Date = new DateTime(DateTime.UtcNow.Ticks);
                // string cname = String.Format("CloudRaw-{0}", Date.ToString("yyyy-MM-dd"));
                // string bname = String.Format("{0}", Date.ToString("HH-mm-ss"));
                // string cname = String.Format("cloudraw-{0}", RandomString(4));
                // string bname = String.Format("{0}", RandomString(4));
                string cname = String.Format("cloudraw-{0}", RandName);
                string bname = String.Format("{0}", RandName);

                if (stream_op == StreamOperation.RandomKeyGet || 
                    stream_op == StreamOperation.RandomKeyGetMultipleSegments || 
                    stream_op == StreamOperation.RandomKeyGetAll)
                {
                    doRawCloudPerf(val, SynchronizerType.Azure,
                        SynchronizeDirection.Download, exp_directory, logger, containerName: cname, blobName: bname);
                    logger.Dump(exp_directory + "/log");
                }
                else
                {
                    doRawCloudPerf(val, SynchronizerType.Azure,
                        SynchronizeDirection.Upload, exp_directory, logger, containerName: cname, blobName: bname);
                    logger.Dump(exp_directory + "/log");
                }
                return;
            }
            
            if (stream_type == StreamType.DiskRaw)
            {
                if (!Directory.Exists(exp_directory))
                {
                    Directory.CreateDirectory(exp_directory);
                }
                Logger logger = doDiskRaw(stream_op, num_operations, value.Length, ptype, exp_directory);
                logger.Dump(exp_directory + "/log");
                return;
            }

            // Are we getting raw disk throughput?
            if (stream_type == StreamType.Raw)
            {
                string ret = doDiskSpeed((value.Length * num_operations)/1000 + "K", value.Length/1000 + "K", rw);
                if (!Directory.Exists(exp_directory))
                {
                    Directory.CreateDirectory(exp_directory);
                }
                File.WriteAllText(exp_directory + "/log", ret);
                return;
            }
            
            // Populate the keys and the values
            Random random = new Random(DateTime.Now.Millisecond);
            StrKey[] keys = new StrKey[max_key];
            for (int i = 0; i < max_key; ++i)
            {
                keys[i] = new StrKey("" + i);
            }

            /*
            List<ByteValue> vals = new List<ByteValue>(num_operations);
            Byte[][] tmp = new Byte[num_operations][];
            for (int i = 0; i < num_operations; ++i)
            {
                tmp[i] = new Byte[value.Length];
                random.NextBytes(tmp[i]);
            }

            for (int i = 0; i < num_operations; ++i)
            {
                keys[i] = new StrKey("" + i);
                vals.Add(new ByteValue(tmp[i]));
                // vals[i] = new ByteValue(tmp);
            }
            */

            Logger log = new Logger();
            // Open stream for different types of experiments
            if (stream_type == StreamType.Local && ptype == StreamFactory.StreamDataType.Values)
            {
                stream = sf.openValueDataStream<StrKey, ByteValue>(streamid, ci, null, StreamFactory.StreamSecurityType.Plain, ctype, rw, address, ChunkSize, ThreadPoolSize, log);
            }

            else if (stream_type == StreamType.LocalEnc && ptype == StreamFactory.StreamDataType.Values)
            {
                stream = sf.openValueDataStream<StrKey, ByteValue>(streamid, ci, null, StreamFactory.StreamSecurityType.Secure, ctype, rw, address, ChunkSize, ThreadPoolSize, log);
            }

            else if (stream_type == StreamType.Remote && ptype == StreamFactory.StreamDataType.Values)
            {
                stream = sf.openValueDataStream<StrKey, ByteValue>(streamid, ci, Li, StreamFactory.StreamSecurityType.Plain, ctype, rw, address, ChunkSize, ThreadPoolSize, log);
            }

            else if (stream_type == StreamType.RemoteEnc && ptype == StreamFactory.StreamDataType.Values)
            {
                stream = sf.openValueDataStream<StrKey, ByteValue>(streamid, ci, Li, StreamFactory.StreamSecurityType.Secure, ctype, rw, address, ChunkSize, ThreadPoolSize, log);
            }

            else if (stream_type == StreamType.Local && ptype == StreamFactory.StreamDataType.Files)
            {
                stream = sf.openFileDataStream<StrKey>(streamid, ci, null, StreamFactory.StreamSecurityType.Plain, ctype, rw, address, ChunkSize, ThreadPoolSize, log);
            }

            else if (stream_type == StreamType.LocalEnc && ptype == StreamFactory.StreamDataType.Files)
            {
                stream = sf.openFileDataStream<StrKey>(streamid, ci, null, StreamFactory.StreamSecurityType.Secure, ctype, rw, address, ChunkSize, ThreadPoolSize, log);
            }
            else if (stream_type == StreamType.Remote && ptype == StreamFactory.StreamDataType.Files)
            {
                stream = sf.openFileDataStream<StrKey>(streamid, ci, Li, StreamFactory.StreamSecurityType.Plain, ctype, rw, address, ChunkSize, ThreadPoolSize, log);
            }
            else if (stream_type == StreamType.RemoteEnc && ptype == StreamFactory.StreamDataType.Files)
            {
                stream = sf.openFileDataStream<StrKey>(streamid, ci, Li, StreamFactory.StreamSecurityType.Secure, ctype, rw, address, ChunkSize, ThreadPoolSize, log);
            }
            else
            {
                return;
            }

            if (stream_op == StreamOperation.RandomKeyRandomValueAppend)
            {
                List<ByteValue> vals = new List<ByteValue>(num_operations);
                Byte[][] tmp = new Byte[num_operations][];
                for (int i = 0; i < num_operations; ++i)
                {
                    tmp[i] = new Byte[value.Length];
                    random.NextBytes(tmp[i]);
                }

                for (int i = 0; i < num_operations; ++i)
                {
                    vals.Add(new ByteValue(tmp[i]));
                }
                
                
                for (int i = 0; i < num_operations; ++i)
                {
                    baselineStorageKV += keys[i].Size();
                    baselineStorageKV += vals[i].Size();
                    stream.Append(keys[i], vals[i]);
                }
                stream.Close();
            }
            
            else if (stream_op == StreamOperation.RandomKeySameValueAppend)
            {
                Byte[] singleval = new Byte[value.Length];
                random.NextBytes(singleval);
                ByteValue singlebv = new ByteValue(singleval);
                for (int i = 0; i < num_operations; ++i)
                {
                    baselineStorageKV += keys[i].Size();
                    baselineStorageKV += value.Length;
                    stream.Append(keys[i], singlebv);
                }
                stream.Close();
            }
            
            else if (stream_op == StreamOperation.SameKeySameValueAppend)
            {
                StrKey key = new StrKey("ExpKey");
                Byte[] singleval = new Byte[value.Length];
                random.NextBytes(singleval);
                ByteValue singlebv = new ByteValue(singleval);
                for (int i = 0; i < num_operations; ++i)
                {
                    stream.Append(key, singlebv);
                    // System.Threading.Thread.Sleep(10);
                }
                stream.Close();
            }
            
            else if (stream_op == StreamOperation.RandomKeyGet || stream_op == StreamOperation.RandomKeyGetMultipleSegments)
            {
                for (int i = 0; i < num_operations; ++i)
                {
                    stream.Get(keys[random.Next(0, max_key)]);
                }
                stream.Close();
            }
            
            else if (stream_op == StreamOperation.RandomKeyGetAll)
            {
                StrKey key = new StrKey("ExpKey");
                for (int i = 0; i < num_operations; )
                {
                    long st = 0;
                    long et = -1;
                    Console.WriteLine(stime + ":" + etime);
                    while (et < st)
                    {
                        st = RandomLong(stime, etime, random);
                        // et = RandomLong(stime, etime, random);
                        et = st + (10 * 10 * TimeSpan.TicksPerMillisecond);
                    }
                    Console.WriteLine(st + ":" + et);
                    IEnumerable<IDataItem> iterator = stream.GetAll(key, st, et);
                    foreach (IDataItem data in iterator)
                    {
                        data.GetVal();
                        ++i;

                        if (i == num_operations)
                            break;
                    }
                }
                stream.Close();
            }
            
            else if (stream_op == StreamOperation.SameKeyRandomValueAppend)
            {
                StrKey key = new StrKey("ExpKey");
                for (int i = 0; i < num_operations; ++i)
                {
                    baselineStorageKV += key.Size();
                    // baselineStorageKV += vals[i].Size();
                    // stream.Append(key, vals[i]);
                }
                stream.Close();
            }
            else 
            {
                for (int i = 0; i < num_operations; ++i)
                {
                    stream.Get(new StrKey("" + random.Next(0,num_operations - 1)));
                }
                stream.Close();
            }

            // Dump the instrumentation logs
            stream.DumpLogs(exp_directory + "/log");
            
            // Collect costs usage
            List<string> costs = new List<string>();
            if (doCosts)
            {
                costs.Add(DateTime.UtcNow.Ticks + ": CPU: " + costhelper.getCurrentCpuUsage());
                costs.Add(DateTime.UtcNow.Ticks + ": Network: " + costhelper.getNetworkUsage());
                costs.Add(DateTime.UtcNow.Ticks + ": DataRelated Storage: " + costhelper.getStorageUsage(this.exp_directory, dataRelated:true)/1000.0f);
                costs.Add(DateTime.UtcNow.Ticks + ": Constant Storage: " + costhelper.getStorageUsage(this.exp_directory, dataRelated:false)/1000.0f);
                costs.Add(DateTime.UtcNow.Ticks + ": Baseline Storage: " + baselineStorageKV/1000.0f);
            }
            File.AppendAllLines(exp_directory + "/log", costs);

            // sf.deleteStream(streamid, ci);
        }
    }
}