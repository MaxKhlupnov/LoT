using Amazon.S3;
using Amazon.S3.Model;
using Amib.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class AmazonS3Helper
    {
        private bool disposed;

        private const int KB = 1024;
        private const int MB = 1024 * KB;
        private int StaticChunkSize = 4 * MB;
        
        public int MaxConcurrentUploadThreads = 10;
        public static string ChunkMetadataObjectPrefix = "chunkMD-"; // blob containing chunk metadata is always block blob by default
        public static string ChunkObjectNamePrefix = "chunk-"; // blob containing chunk metadata is always block blob by default

        public static int Timeout = 30; // seconds

        protected Logger logger;

        private CompressionType chunkCompressionType;
        private EncryptionType chunkEncryptionType;

        //AES specific
        private byte[] encryptionKey;
        private byte[] InitializationVector;

        private string bucketName;
        RemoteInfo remoteInfo;
        private AmazonS3Client amazonS3Client;


         public AmazonS3Helper(RemoteInfo remoteInfo,  string bucketName, CompressionType chunkCompression, EncryptionType chunkEncryption , byte[] encryptionKey , byte[] initializationVector, Logger log, int ChunkSize, int ThreadPoolSize)
        {
            this.logger = log;
            this.remoteInfo = remoteInfo;
            this.bucketName = bucketName;
            this.chunkCompressionType = chunkCompression;
            this.chunkEncryptionType = chunkEncryption;
            this.encryptionKey = encryptionKey;
            this.InitializationVector = initializationVector;
            amazonS3Client = new AmazonS3Client(remoteInfo.accountName, remoteInfo.accountKey);
            this.StaticChunkSize = ChunkSize;
            this.MaxConcurrentUploadThreads = ThreadPoolSize;

            this.disposed = false;
            CreateBucket(this.bucketName);
        }


         public bool UploadFileToS3Object(string s3ObjectName, string filePath)
         {

             try
             {
                 PutObjectRequest request = new PutObjectRequest();
                 request.WithBucketName(bucketName);
                 request.WithKey(s3ObjectName);
                 request.WithFilePath(filePath);//request.WithContentBody
                 amazonS3Client.PutObject(request);
                 return true;
             }
             catch (Exception e)
             {
                 structuredLog("E", "Exception in UploadFileToS3Object: "+e);
                 return false;
             }

         }

         public bool UploadByteArrayToS3Object(string s3ObjectName, byte[] input)
         {
             try
             {
                 MemoryStream ms = new MemoryStream();
                 ms.Write(input, 0, input.Length);
                 PutObjectRequest request = new PutObjectRequest();
                 request.WithBucketName(bucketName);
                 request.WithKey(s3ObjectName);
                 request.InputStream = ms;
                 amazonS3Client.PutObject(request);
                 return true;
             }
             catch (Exception e)
             {
                 structuredLog("E", "Exception in UploadFileToS3Object: " + e);
                 return false;
             }

         }

         public bool UploadStringToS3Object(string s3ObjectName, string input)
         {

             try
             {
                 PutObjectRequest request = new PutObjectRequest();
                 request.WithBucketName(bucketName);
                 request.WithKey(s3ObjectName);
                 request.WithContentBody(input);
                 amazonS3Client.PutObject(request);
                 return true;
             }
             catch (Exception e)
             {
                 structuredLog("E", "Exception in UploadFileToS3Object: " + e);
                 return false;
             }

         }

         public bool DownloadS3ObjectToFile(string s3ObjectName, string filePath)
         {
             try
             {
                 if (!S3ObjectExists(ChunkMetadataObjectPrefix + s3ObjectName))
                     return false;

                 GetObjectRequest request = new GetObjectRequest();
                 request.WithBucketName(bucketName);
                 request.WithKey(s3ObjectName);
                 GetObjectResponse response = amazonS3Client.GetObject(request);

                 if (File.Exists(filePath))
                     File.Delete(filePath);

                 var localFileStream = File.Create(filePath);
                 response.ResponseStream.CopyTo(localFileStream);
                 localFileStream.Close();
                 return true;

             }
             catch (Exception e)
             {
                 structuredLog("E", "Exception in DownloadS3ObjectToFile: " + e);
                 return false;
             }

         }

         public byte[] DownloadS3ObjectToBytes(string s3ObjectName)
         {
             try
             {
                 GetObjectRequest request = new GetObjectRequest();
                 request.WithBucketName(bucketName);
                 request.WithKey(s3ObjectName);
                 GetObjectResponse response = amazonS3Client.GetObject(request);

                 byte[] buffer = new byte[1024];
                 using (MemoryStream ms = new MemoryStream())
                 {
                    int read;
                    while ((read = response.ResponseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                            ms.Write(buffer, 0, read);
                    }
                    return ms.ToArray();
                 }

             }
             catch (Exception e)
             {
                 structuredLog("E", "Exception in DownloadS3ObjectToBytes: " + e);
                 return null;
             }

         }

         public System.Tuple<List<ChunkInfo>, byte[]> GetObjectMetadata(string s3objectName)
        {
            List<ChunkInfo> retVal = null;
             byte[] hash = null;
             string metadataObjectName = ChunkMetadataObjectPrefix + s3objectName;
             bool s3ObjectExists = S3ObjectExists(metadataObjectName);

             if (s3ObjectExists)
             {
                 GetObjectRequest request = new GetObjectRequest();
                 request.WithBucketName(bucketName);
                 request.WithKey(metadataObjectName);
                 GetObjectResponse response = amazonS3Client.GetObject(request);
                 StreamReader reader = new StreamReader(response.ResponseStream);

                 string chunkMD_JSON = reader.ReadToEnd();
                 FileMD fileMD = JsonConvert.DeserializeObject<FileMD>(chunkMD_JSON);
                 SHA1 sha1 = new SHA1CryptoServiceProvider();
                 hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(chunkMD_JSON));
                 retVal = fileMD.ChunkList;
             }

             return new System.Tuple<List<ChunkInfo>, byte[]>(retVal, hash);
        }

         #region logging and file system ops
         
        public static void structuredLog(string type, params string[] messages)
         {
             bool verbose = true;

             if (type == "ER") type = "ERROR";
             else if (verbose && type == "I") type = "INFO";
             else if (type == "E") type = "EXCEPTION";
             else if (verbose && type == "W") type = "WARNING";

             StringBuilder s = new StringBuilder();
             s.Append("[AmazonS3Helper]" + "[" + type + "]");
             foreach (string message in messages)
                 s.Append("[" + message + "]");
             Console.WriteLine(DateTime.Now + " : " + s.ToString());
         }

         public static List<string> ListFiles(string directory)
         {
             List<string> retVal = new List<string>();
             try
             {
                 foreach (string f in Directory.GetFiles(directory))
                     retVal.Add(Path.GetFullPath(f));
             }
             catch (Exception e)
             {
                 structuredLog("E", "Exception " + e.Message + ". ListFiles, directory: " + directory);
             }
             return retVal;
         }

        #endregion



        private bool CreateBucket(string bucketName)
        {
            try
            {
                ListBucketsResponse response = amazonS3Client.ListBuckets();
                foreach (S3Bucket bucket in response.Buckets)
                {
                    if (bucket.BucketName == bucketName)
                    {
                        return true;
                    }
                }

                amazonS3Client.PutBucket(new PutBucketRequest().WithBucketName(bucketName));
                return true;
            }
            catch (Exception e)
            {
                structuredLog("E", "Exception in CreateBucket: " + e);
                return false;
            }
            
        }

        public bool S3ObjectExists(string s3ObjectName)
        {
            GetObjectRequest request = new GetObjectRequest();
            request.BucketName = this.bucketName;
            request.Key = s3ObjectName;
            try
            {
                S3Response response = amazonS3Client.GetObject(request);
                if (response.ResponseStream != null)
                {
                    return true;
                }
            }
            catch (AmazonS3Exception)
            {
                // structuredLog("I", "S3object: "+s3ObjectName+" does not exist");
            }
            catch (Exception e)
            {
                structuredLog("E", "Exception in S3ObjectExists: " + e);
            }
            return false;
        }

        public byte[] UploadFileAsChunks(string filePath)
        {
            string s3objectName;
            List<ChunkInfo> chunkList_cloud = new List<ChunkInfo>(); ; // list of chunk indexed by chunk-index (e.g. 0, 1, 2,....)
            List<ChunkInfo> chunkList_local; // list of chunk indexed by chunk-index (e.g. 0, 1, 2,....)

            try
            {
                if (logger != null) logger.Log("Start Synchronizer Check Blob Exists");
                s3objectName = Path.GetFileName(filePath);
                bool s3ObjectExists = S3ObjectExists(ChunkMetadataObjectPrefix + s3objectName);
                if (logger != null) logger.Log("End Synchronizer Check Blob Exists");

                if (s3ObjectExists)
                {
                    if (logger != null) logger.Log("Start Synchronizer Fill Remote ChunkList");
                    GetObjectRequest request = new GetObjectRequest();
                    request.WithBucketName(bucketName);
                    request.WithKey(ChunkMetadataObjectPrefix + s3objectName);
                    GetObjectResponse response = amazonS3Client.GetObject(request);
                    StreamReader reader = new StreamReader(response.ResponseStream);

                    string chunkMD_JSON = reader.ReadToEnd();

                    FileMD fileMD = JsonConvert.DeserializeObject<FileMD>(chunkMD_JSON);
                    StaticChunkSize = fileMD.StaticChunkSize;
                    chunkList_cloud = fileMD.ChunkList;
                    if (logger != null) logger.Log("End Synchronizer Fill Remote ChunkList");
                    chunkCompressionType = SyncFactory.GetCompressionType(fileMD.compressionType);
                    chunkEncryptionType = SyncFactory.GetEncryptionType(fileMD.encryptionType);
                }
                
                if (logger != null) logger.Log("Start Synchronizer Fill Local ChunkList");
                StaticChunk staticChunker = new StaticChunk(StaticChunkSize);
                chunkList_local = staticChunker.GetCurrentChunkList(filePath); // if doing other class that implements the IChunk interface
                // structuredLog("I", "Number of chunks locally: " + chunkList_local.Count);
                if (logger != null) logger.Log("End Synchronizer Fill Local ChunkList");

                if (logger != null) logger.Log("Start Synchronizer ChunkList Compare");
                List<ChunkInfo> chunkList_toUpload = staticChunker.GetUploadChunkList(chunkList_local, chunkList_cloud);
                // structuredLog("I", "Number of chunks on cloud blob: " + chunkList_cloud.Count);
                // structuredLog("I", "Number of chunks to be uploaded: " + chunkList_toUpload.Count);
                if (logger != null) logger.Log("End Synchronizer ChunkList Compare");

                if (logger != null) logger.Log("Start Synchronizer Upload Multiple Chunks");
                UploadChunkList(ref chunkList_toUpload, filePath, s3objectName);
                if (logger != null) logger.Log("End Synchronizer Upload Multiple Chunks");

                // structuredLog("I", "Number of chunks uploaded: " + chunkList_toUpload.Count);
                
                if (logger != null) logger.Log("Start Synchronizer ChunkList Upload");
                string json = JsonConvert.SerializeObject(new FileMD(StaticChunkSize, chunkList_local, SyncFactory.GetCompressionTypeAsString(this.chunkCompressionType), SyncFactory.GetEncryptionTypeAsString(this.chunkEncryptionType)), new KeyValuePairConverter());

                if (chunkList_toUpload.Count > 0) //upload new chunk list only if we uploaded some new chunks
                {
                    UploadStringToS3Object(ChunkMetadataObjectPrefix + s3objectName, json);
                }

                SHA1 sha1 = new SHA1CryptoServiceProvider();
                byte[] ret = sha1.ComputeHash(Encoding.ASCII.GetBytes(json));
                if (logger != null) logger.Log("End Synchronizer ChunkList Upload");
                return ret;
            }
            catch (Exception e)
            {
                structuredLog("E", " . UploadFileAsChunks: " + e);
                return null;
            }
        }

        private void UploadChunkList(ref List<ChunkInfo> chunkList_toUpload, string filePath, string blobName)
        {
            SmartThreadPool threadPool = new SmartThreadPool();
            threadPool.MaxThreads = MaxConcurrentUploadThreads;
            foreach (ChunkInfo chunk in chunkList_toUpload)
            {
                ChunkInfo chunkToUpload = chunk;
                IWorkItemResult wir1 = threadPool.QueueWorkItem(new WorkItemCallback(this.UploadChunk_Worker), new Tuple<string, string, ChunkInfo>(filePath, blobName, chunkToUpload));
                //UploadChunk(filePath, blobName, ref chunkToUpload);
            }
            threadPool.Start();
            threadPool.WaitForIdle();
            threadPool.Shutdown();
        }

        private object UploadChunk_Worker(object state)
        {
            Tuple<string, string, ChunkInfo> rx = (Tuple<string, string, ChunkInfo>)state;
            string filePath = rx.Item1;
            string blobName = rx.Item2;
            ChunkInfo chunk = rx.Item3;
            UploadChunk(filePath, blobName, ref chunk);
            return null;
        }


        private void UploadChunk(string filePath, string s3objectName, ref ChunkInfo chunk)
        {
            // structuredLog("I", "uploading chunk with index: " + chunk.chunkIndex);
            // if (logger != null) logger.Log("Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
            if (logger != null) logger.Log("Start Synchronizer Read Chunk From File");

            byte[] chunkBuffer = new byte[chunk.rsize];

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {

                fs.Seek(chunk.roffset, SeekOrigin.Begin);
                BinaryReader br = new BinaryReader(fs);
                br.Read(chunkBuffer, 0, chunkBuffer.Length);//read chunk into buffer
                fs.Close();
            }
            if (logger != null) logger.Log("End Synchronizer Read Chunk From File");

            // Here is the compression and encryption for the chunk
            if (logger != null) logger.Log("Start Synchronizer Compress Chunk");
            byte[] compressedChunkBuffer = Compress(chunkBuffer);// for now
            if (logger != null) logger.Log("End Synchronizer Compress Chunk");
            if (logger != null) logger.Log("Start Synchronizer Encrypt Chunk");
            byte[] encryptedCompressedChunkBuffer = Encrypt(compressedChunkBuffer);
            if (logger != null) logger.Log("End Synchronizer Encrypt Chunk");

            if (logger != null) logger.Log("Start Synchronizer Upload Chunk");
            string chunkObjectName = ChunkObjectNamePrefix + s3objectName + "-" + chunk.chunkIndex;

            if (UploadByteArrayToS3Object(chunkObjectName, encryptedCompressedChunkBuffer))
            {
                chunk.SetBlobName(chunkObjectName);
                chunk.SetCSize(encryptedCompressedChunkBuffer.Length);
            }
            else
                throw new Exception("Chunk upload for given chunk has failed. FileName: " + s3objectName + " . Chunk:" + chunk.ToString());
            if (logger != null) logger.Log("End Synchronizer Upload Chunk");

        }

        #region compress decompress 

        private byte[] Compress(byte[] input)
        {
            byte[] retVal = null;

            switch (this.chunkCompressionType)
            {
                case CompressionType.None:
                    retVal = input;
                    break;
                case CompressionType.GZip:
                    retVal = AzureHelper.GZipCompress(input);
                    break;
                case CompressionType.BZip2:
                    retVal = AzureHelper.BZip2Compress(input);
                    break;
                default:
                    throw new NotImplementedException("compression type " + this.chunkCompressionType + " not implemented");
            }
            return retVal;
        }

        private byte[] Decompress(byte[] input)
        {
            byte[] retVal = null;

            switch (this.chunkCompressionType)
            {
                case CompressionType.None:
                    retVal = input;
                    break;
                case CompressionType.GZip:
                    retVal = AzureHelper.GZipDecompress(input);
                    break;
                case CompressionType.BZip2:
                    retVal = AzureHelper.BZip2Decompress(input);
                    break;
                default:
                    throw new NotImplementedException("compression type " + this.chunkCompressionType + " not implemented");
            }
            return retVal;
        }

        #endregion

        #region encrypt decrypt
        private byte[] Encrypt(byte[] input)
        {
            byte[] retVal = null;

            switch (this.chunkEncryptionType)
            {
                case EncryptionType.None:
                    retVal = input;
                    break;
                case EncryptionType.AES:
                    retVal = Crypto.EncryptBytesSimple(input, Crypto.KeyDer(this.encryptionKey), this.InitializationVector);
                    break;
                default:
                    throw new NotImplementedException("encryption type " + this.chunkEncryptionType + " not implemented");
            }
            return retVal;

        }

        private byte[] Decrypt(byte[] input)
        {
            byte[] retVal = null;

            switch (this.chunkEncryptionType)
            {
                case EncryptionType.None:
                    retVal = input;
                    break;
                case EncryptionType.AES:
                    retVal = Crypto.DecryptBytesSimple(input, Crypto.KeyDer(this.encryptionKey), this.InitializationVector);
                    break;
                default:
                    throw new NotImplementedException("decryption type " + this.chunkEncryptionType + " not implemented");
            }
            return retVal;

        }

        #endregion 


        public bool DeleteBucket()
        {
            try
            {
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = this.bucketName;
                ListObjectsResponse response = amazonS3Client.ListObjects(request);
                foreach (S3Object o in response.S3Objects)
                {
                    DeleteObjectRequest delrequest = new DeleteObjectRequest();
                    delrequest.BucketName = this.bucketName;
                    delrequest.Key = o.Key;
                    amazonS3Client.DeleteObject(delrequest);
                }

                DeleteBucketRequest delbucketrequest = new DeleteBucketRequest();
                delbucketrequest.BucketName = this.bucketName;
                amazonS3Client.DeleteBucket(delbucketrequest);
                return true;
            }

            catch (Exception e)
            {
                structuredLog("E", "Exception in DeleteBucket: " + e);
                return false;
            }
        }

        #region reading of data

        public Dictionary<int, long> GetChunkIndexAndOffsetInChunk(List<ChunkInfo> blobMetadata, long requiredDataOffset, long size)
        {
            Dictionary<int, long> retVal = new Dictionary<int, long>();

            double temp = requiredDataOffset / StaticChunkSize;
            int requiredChunkIndex = (int)Math.Ceiling(temp);
            long offSetInChunk = (requiredDataOffset % StaticChunkSize);

            retVal[requiredChunkIndex] = offSetInChunk;
            size = size - StaticChunkSize + offSetInChunk;

            while (size > 0)
            {
                requiredChunkIndex++;
                retVal[requiredChunkIndex] = 0;
                size = size - StaticChunkSize;
            }
            return retVal;
        }

        public byte[] DownloadChunk(string s3ObjectName, int chunkIndex)
        {
            try 
            {
                string chunkObjectName = ChunkObjectNamePrefix +s3ObjectName +"-" + chunkIndex;
                byte[] buffer = DownloadS3ObjectToBytes(chunkObjectName);
                return Decompress(Decrypt(buffer));
            }

            catch (Exception e)
            {
                structuredLog("E", " Exception in DownloadChunk: " + e);
                return null;
            }



        }

        #endregion 
        //***
        public System.Tuple<bool, List<string>> ListObjectsInBucket(AmazonS3Client client, string bucketName)
        {
            try
            {

                ListObjectsRequest request = new ListObjectsRequest();
                request.WithBucketName(bucketName);


                ListObjectsResponse response = client.ListObjects(request);
  
                List<string> retList = new List<string>();
                foreach (S3Object s3Object in response.S3Objects)
                    retList.Add(s3Object.Key);

                return new System.Tuple<bool, List<string>>(false, retList);
            }
            catch (Exception e)
            {
                return new System.Tuple<bool, List<string>>(false, new List<string>() { e.ToString()});
            }

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    this.remoteInfo = null;
                    this.bucketName = null;
                    this.encryptionKey = null;
                    this.InitializationVector = null;
                    amazonS3Client = null;
                    disposed = true;
                }
            }
        }

        public void DeleteAllBuckets()
        {
            ListBucketsResponse response = this.amazonS3Client.ListBuckets();
            foreach (S3Bucket b in response.Buckets)
            {
                Console.WriteLine("{0}\t{1}", b.BucketName, b.CreationDate);
                ListObjectsRequest r = new ListObjectsRequest();
                r.BucketName = b.BucketName;
                ListObjectsResponse resp = this.amazonS3Client.ListObjects(r);

                foreach (S3Object o in resp.S3Objects)
                {
                    DeleteObjectRequest dr = new DeleteObjectRequest();
                    dr.BucketName = b.BucketName;
                    dr.Key = o.Key;
                    amazonS3Client.DeleteObject(dr);
                }

                DeleteBucketRequest request = new DeleteBucketRequest();
                request.BucketName = b.BucketName;
                amazonS3Client.DeleteBucket(request);
            }

        }


    }


}
