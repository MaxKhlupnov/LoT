using Amib.Threading;
using HomeOS.Hub.Common.Bolt.DataStore;

using ICSharpCode.SharpZipLib.BZip2;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class AzureHelper
    {
        public enum AzureBlobType : byte { BlockBlob = 0 , PageBlob }
        public enum Mapping : byte { FileToBlob = 0 , ChunkToBlob }

        private const int KB = 1024;
        private const int MB = 1024 * KB;
        private int StaticChunkSize = 4 * MB;
        public const int MaxAzureBlockSize = 4 * MB;
        public int MaxConcurrentUploadThreads = 1; 

        public static string ChunkMetadataBlobPrefix = "chunkMD-" ; // blob containing chunk metadata is always block blob by default
        public static int Timeout = 30; // seconds

        protected Logger logger;

        //private AzureBlobType azureBlobType; 
        //private Mapping mapping;
        private RemoteInfo remoteInfo;
        private CompressionType chunkCompressionType;
        private EncryptionType chunkEncryptionType;
        
        //AES specific
        private byte[] encryptionKey;
        private byte[] InitializationVector;

        private string containerName;

        
        

        public AzureHelper(string accountName, string accountKey, string containerName, CompressionType chunkCompression, EncryptionType chunkEncryption , byte[] encryptionKey , byte[] initializationVector, Logger log, int ChunkSize, int ThreadPoolSize)
        {
            this.logger = log;
            //this.azureBlobType = AzureBlobType.BlockBlob;
            //this.mapping = Mapping.FileToBlob;
            this.remoteInfo = new RemoteInfo(accountName, accountKey);
            this.containerName = containerName;
            this.chunkCompressionType = chunkCompression;
            this.chunkEncryptionType = chunkEncryption;
            this.encryptionKey = encryptionKey;
            this.InitializationVector = initializationVector;
            this.StaticChunkSize = ChunkSize;
            this.MaxConcurrentUploadThreads = ThreadPoolSize;
        }

        public byte[] UploadFileAsChunks(string filePath)
        {
            string blobName;
            List<ChunkInfo> chunkList_cloud = new List<ChunkInfo>();; // list of chunk indexed by chunk-index (e.g. 0, 1, 2,....)
            List<ChunkInfo> chunkList_local; // list of chunk indexed by chunk-index (e.g. 0, 1, 2,....)

            try
            {
                // First check if chunkMD exists on the server or not
                if (logger != null) logger.Log("Start Synchronizer Check Blob Exists");
                blobName = Path.GetFileName(filePath);
                CloudBlockBlob chunkMDblockBlob = GetBlockBlobReference(ChunkMetadataBlobPrefix + blobName);
                bool blobExists = BlockBlobExists(chunkMDblockBlob);
                if (logger != null) logger.Log("End Synchronizer Check Blob Exists");

                if (blobExists)
                {
                    if (logger != null) logger.Log("Start Synchronizer Fill Remote ChunkList");
                    // Fill chunkList
                    FileMD fileMD = JsonConvert.DeserializeObject<FileMD>(chunkMDblockBlob.DownloadText());
                    StaticChunkSize = fileMD.StaticChunkSize;
                    // Get chunklist at cloud in memory locally
                    chunkList_cloud = fileMD.ChunkList;
                    if (logger != null) logger.Log("End Synchronizer Fill Remote ChunkList");
                    chunkCompressionType = SyncFactory.GetCompressionType(fileMD.compressionType);
                    chunkEncryptionType = SyncFactory.GetEncryptionType(fileMD.encryptionType);
                }

                if (logger != null) logger.Log("Start Synchronizer Fill Local ChunkList");
                StaticChunk staticChunker = new StaticChunk(StaticChunkSize);
                // Store local chunkList in memory
                //long start = DateTime.Now.Ticks;
                chunkList_local = staticChunker.GetCurrentChunkList(filePath); // if doing other class that implements the IChunk interface
                //long end = DateTime.Now.Ticks;
               // Console.WriteLine("time taken : " + (double)((double)(end - start) / (double)10000000));
            
                if (logger != null) logger.Log("End Synchronizer Fill Local ChunkList");
                // structuredLog("I", "Number of chunks locally: " + chunkList_local.Count);

                // Figure out the changes
                if (logger != null) logger.Log("Start Synchronizer ChunkList Compare");
                List<ChunkInfo> chunkList_toUpload = staticChunker.GetUploadChunkList(chunkList_local, chunkList_cloud);
                // structuredLog("I", "Number of chunks on cloud blob: " + chunkList_cloud.Count);
                // structuredLog("I", "Number of chunks to be uploaded: " + chunkList_toUpload.Count);
                if (logger != null) logger.Log("End Synchronizer ChunkList Compare");

                if (logger != null) logger.Log("Start Synchronizer Upload Multiple Chunks");
                UploadChunkList(ref chunkList_toUpload, filePath, blobName);
                if (logger != null) logger.Log("End Synchronizer Upload Multiple Chunks");

                // structuredLog("I", "Number of chunks uploaded: " + chunkList_toUpload.Count);

                if (logger != null) logger.Log("Start Synchronizer Commit BlockList");
                // Commit the ordered blocklist
                if (chunkList_toUpload.Count > 0)// write new metadata, and putblocklist() if we uploaded some chunks
                {
                    CloudBlockBlob blockBlob = GetBlockBlobReference(blobName);
                    List<int> blockIDCommitList = GetBlockIDList(ref chunkList_local, ref chunkList_cloud, ref chunkList_toUpload);
                    long startt = DateTime.Now.Ticks;
                    blockBlob.PutBlockList(EncodeBlockList(blockIDCommitList), GetBlobRequestOptions());
                    long endt = DateTime.Now.Ticks;
                    if (logger != null) logger.Log("PUTBLOCK LIST : " + (double)((double)(endt - startt) / (double)10000000));
                }
                if (logger != null) logger.Log("End Synchronizer Commit BlockList");

                // Upload the local chunklist to the cloud
                if (logger != null) logger.Log("Start Synchronizer ChunkList Upload");
                string json = JsonConvert.SerializeObject(new FileMD(StaticChunkSize, chunkList_local, SyncFactory.GetCompressionTypeAsString(this.chunkCompressionType), SyncFactory.GetEncryptionTypeAsString(this.chunkEncryptionType)), new KeyValuePairConverter());
    
                if (chunkList_toUpload.Count > 0 || chunkList_local.Count==0) //upload new chunk list only if we uploaded some new chunks, or if this is a new stream, with no data in stream.dat
                    chunkMDblockBlob.UploadText(json);

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


        // Upload the difference in the chunkLists
        private void UploadChunkList(ref List<ChunkInfo> chunkList_toUpload, string filePath, string blobName)
        {
           // SmartThreadPool threadPool = new SmartThreadPool();
          //  threadPool.MaxThreads = MaxConcurrentUploadThreads;

            if (logger != null) logger.Log("Start UploadChunkList Create and Queue Threads");
            SmartThreadPool threadPool = new SmartThreadPool();
            threadPool.MaxThreads = MaxConcurrentUploadThreads;

            foreach (ChunkInfo chunk in chunkList_toUpload)
            {
                ChunkInfo chunkToUpload = chunk;
                IWorkItemResult wir1 = threadPool.QueueWorkItem(new WorkItemCallback(this.UploadChunk_Worker), new Tuple<string, string, ChunkInfo>(filePath, blobName, chunkToUpload));
            //    UploadChunk(filePath, blobName, ref chunkToUpload);
            }
            threadPool.Start();
            threadPool.WaitForIdle();
            threadPool.Shutdown();
            if (logger != null) logger.Log("End UploadChunkList Create and Queue Threads");
            
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

        private List<int> GetBlockIDList(ref List<ChunkInfo> chunkList_local, ref List<ChunkInfo> chunkList_cloud, ref List<ChunkInfo> chunkList_toUpload)
        {
            List<int> blockIDCommitList = new List<int>();

            foreach (ChunkInfo chunk in chunkList_local)
            {
                foreach (ChunkInfo cloudChunk in chunkList_cloud)
                {
                    if (cloudChunk.chunkIndex == chunk.chunkIndex)
                    {
                        chunk.blockList = cloudChunk.blockList;
                        blockIDCommitList = blockIDCommitList.Concat(cloudChunk.blockList).ToList();
                        break;
                    }
                }

                foreach (ChunkInfo uploadedChunk in chunkList_toUpload)
                {
                    if (uploadedChunk.chunkIndex == chunk.chunkIndex)
                    {
                        chunk.blockList = uploadedChunk.blockList;
                        blockIDCommitList = blockIDCommitList.Concat(uploadedChunk.blockList).ToList();
                        break;
                    }
                }

            }

            return blockIDCommitList ;

        }


        private void UploadChunk(string filePath, string blobName, ref ChunkInfo chunk)
        {
            int tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (logger != null) logger.Log("Start Synchronizer Read Chunk From File" + tid);
            if (StaticChunkSize > MaxAzureBlockSize && StaticChunkSize % MaxAzureBlockSize != 0)
                throw new NotImplementedException("chunk size (current: " + chunk.rsize + ") has to be a fixed multiple of maximum block size :" + MaxAzureBlockSize);

            // structuredLog("I", "uploading chunk with index: " + chunk.chunkIndex);

            byte[] chunkBuffer = new byte[chunk.rsize];


            long start = DateTime.Now.Ticks;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                
                fs.Seek(chunk.roffset, SeekOrigin.Begin);
                BinaryReader br = new BinaryReader(fs);
                br.Read(chunkBuffer, 0, chunkBuffer.Length);//read chunk into buffer
                fs.Close();
            }
            long end = DateTime.Now.Ticks;
           // Console.WriteLine("time taken : " + (double)((double)(end - start) / (double)10000000));
           
            if (logger != null) logger.Log("End Synchronizer Read Chunk From File" + tid);


            
            // Here is the compression and encryption for the chunk
            if (logger != null) logger.Log("Start Synchronizer Compress Chunk");
            byte[] compressedChunkBuffer = Compress(chunkBuffer);// for now
            if (logger != null) logger.Log("End Synchronizer Compress Chunk");
            if (logger != null) logger.Log("Start Synchronizer Encrypt Chunk");
            byte[] encryptedCompressedChunkBuffer = Encrypt(compressedChunkBuffer);
            if (logger != null) logger.Log("End Synchronizer Encrypt Chunk");

            if (logger != null) logger.Log("Start Synchronizer Upload Chunk" + tid);

            int blockID;
            if(StaticChunkSize < MaxAzureBlockSize)
                blockID = (int)chunk.chunkIndex ;
            else
                blockID = (int)chunk.chunkIndex * (int)(StaticChunkSize / MaxAzureBlockSize);
            int blockCount = 0;
            CloudBlockBlob blockBlob = GetBlockBlobReference(blobName);

            // Upload the chunk
            while (blockCount * MaxAzureBlockSize < encryptedCompressedChunkBuffer.Length)
            {
                string blockIdBase64 = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(blockID.ToString(CultureInfo.InvariantCulture).PadLeft(16, '0')));

                int blockSize = MaxAzureBlockSize <= encryptedCompressedChunkBuffer.Length - blockCount * MaxAzureBlockSize ? MaxAzureBlockSize : encryptedCompressedChunkBuffer.Length - blockCount * MaxAzureBlockSize;

                long startt = DateTime.Now.Ticks;
                using (MemoryStream ms = new MemoryStream(encryptedCompressedChunkBuffer, blockCount * MaxAzureBlockSize, blockSize))
                {
                    blockBlob.PutBlock(blockIdBase64, ms, GetMD5FromStream(ms.ToArray()), GetBlobRequestOptions());
                }
                long endt = DateTime.Now.Ticks;
                 //Console.WriteLine("+" + (double)((double)(endt - startt) / (double)10000000));

                chunk.AddToBlockList(blockID);
                chunk.SetCSize(encryptedCompressedChunkBuffer.Length);
                blockID++;
                blockCount++;
                
            }
            if (logger != null) logger.Log("End Synchronizer Upload Chunk" + tid);
        }


        private List<string> EncodeBlockList(List<int> blockIDCommitList)
        {
            List<string> blockIDCommitList_encoded = new List<string>();
            foreach (int blockID in blockIDCommitList)
            {
                blockIDCommitList_encoded.Add(Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(blockID.ToString(CultureInfo.InvariantCulture).PadLeft(16, '0'))));
            }
            return blockIDCommitList_encoded;
        }


        #region azure container, blob and blockblob specific methods
        private CloudBlockBlob GetBlockBlobReference(string blobName, bool createContainerIfNotExists = true)
        {
            try
            {
                CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentialsAccountAndKey(this.remoteInfo.accountName, this.remoteInfo.accountKey), true);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);

                if (createContainerIfNotExists)
                    container.CreateIfNotExist(GetBlobRequestOptions());

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                return blockBlob;
            }
            catch (Exception e)
            {
                structuredLog("E", "Exception in Getting BlockBlobReference: " + e.Message);
                throw e;
                //return null;
            }
        }

        private BlobRequestOptions GetBlobRequestOptions()
        {
            BlobRequestOptions options = new BlobRequestOptions()
            {
                // Changing for immediate return in case of disconnection
                //RetryPolicy = RetryPolicies.RetryExponential(RetryPolicies.DefaultClientRetryCount, RetryPolicies.DefaultMaxBackoff),
                RetryPolicy = RetryPolicies.Retry(0, TimeSpan.FromSeconds(0)),// i.e. do not retry 
                Timeout = TimeSpan.FromSeconds(AzureHelper.Timeout) // i.e. timeout in a second
            };
            return options;
        }

        public bool BlockBlobExists(CloudBlockBlob blob)
        {
            try
            {
                blob.FetchAttributes(GetBlobRequestOptions());
                return true;
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                   // structuredLog("I", "BlockBlob: " + blob.Name + " does not exist.");
                   return false;
                }
                else
                {
                    // structuredLog("E", " StorageClientExceptio .BlockBlob: " + e);
                    throw;
                }
            }
        }

        public bool DownloadBlockBlobToFile(string blobName, string filePath)
        {
            try
            {
                CloudBlockBlob blockBlob = GetBlockBlobReference(blobName);
                GC.Collect();
                blockBlob.DownloadToFile(filePath, GetBlobRequestOptions());
                GC.Collect();
                return true;
            }
            catch (Exception e)
            {
                structuredLog("E", "Exception in DownloadBlockBlobToFile: blobName: " + blobName + " filePath: " + filePath + " " + e.Message);
                return false;
            }

        }

        public bool UploadFileToBlockBlob(string blobName, string filePath)
        {
            try
            {
                CloudBlockBlob blockBlob = GetBlockBlobReference(blobName);
                blockBlob.UploadFile(filePath, GetBlobRequestOptions());
                return true;
            }
            catch (Exception e)
            {
                structuredLog("E", " Exception in UploadFileToBlockBlob: blobName: " + blobName + " filePath: " + filePath + " " + e.Message);
                return false;
            }

        }


        public Tuple<List<ChunkInfo>, byte[]> GetBlobMetadata(string blobName)
        {
            CloudBlockBlob chunkMDblockBlob = GetBlockBlobReference(ChunkMetadataBlobPrefix + blobName);
            bool blobExists = BlockBlobExists(chunkMDblockBlob);
            List<ChunkInfo> retVal;
            byte[]hash ;

            if (blobExists)
            {
                string chunkMD_JSON = chunkMDblockBlob.DownloadText();
                FileMD fileMD = JsonConvert.DeserializeObject<FileMD>(chunkMD_JSON);
                SHA1 sha1 = new SHA1CryptoServiceProvider();
                hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(chunkMD_JSON));
                retVal = fileMD.ChunkList;
            }
            else
            {
                retVal = null;
                hash = null;

                if (!blobName.Equals("stream_md.dat"))
                    Console.WriteLine("Now: blob exists false for  "+chunkMDblockBlob);
            }
            

            return new Tuple<List<ChunkInfo>,byte[]>(retVal,hash ) ;
        }

        public bool DeleteContainer()
        {
            try
            {
                CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentialsAccountAndKey(this.remoteInfo.accountName, this.remoteInfo.accountKey), true);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(this.containerName);
                blobContainer.Delete(GetBlobRequestOptions());
                return true;

            }
            catch (Exception e)
            {
                structuredLog("E", " Exception in DeleteContainer: " + this.containerName + " :" + e);
                return false;
            }
        }

    
#endregion


        #region logging and file system ops

        private string GetMD5FromStream(byte[] data)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] blockHash = md5.ComputeHash(data);
            return Convert.ToBase64String(blockHash, 0, 16);
        }

        public static void structuredLog(string type, params string[] messages)
        {
            bool verbose = true;
        
            if (type == "ER") type = "ERROR";
            else if (verbose && type == "I") type = "INFO";
            else if (type == "E") type = "EXCEPTION";
            else if (verbose && type == "W") type = "WARNING";

            StringBuilder s = new StringBuilder();
            s.Append("[AzureHelper]" + "[" + type + "]");
            foreach (string message in messages)
                s.Append("[" + message + "]");
             Console.WriteLine(DateTime.Now +" : "+ s.ToString());
        }

        public static List<string> ListFiles(string directory, string indexFileName, string dataFileName)
        {
            List<string> retVal = new List<string>();
            string datafilepath = String.Empty, indexfilepath = String.Empty;
            try
            {
                foreach (string f in Directory.GetFiles(directory))
                {
                    retVal.Add(f);
                    if (Path.GetFileName(f).Equals(indexFileName))
                        indexfilepath = f;
                    if (Path.GetFileName(f).Equals(dataFileName))
                        datafilepath = f;
                }

                if (!indexfilepath.Equals(String.Empty) && !datafilepath.Equals(String.Empty))
                {
                    retVal.Remove(indexfilepath);
                    retVal.Remove(datafilepath);
                    retVal.Add(datafilepath);
                    retVal.Add(indexfilepath);
                }
            }
            catch (Exception e)
            {
                structuredLog("E", "Exception " + e.Message + ". ListFiles, directory: " + directory);
            }
            return retVal;
        }
        
        #endregion

        #region reading chunks from blob 

        // Function returns the chunkIDs and the offset in the chunk for a given file-offset
        public Dictionary<int,long> GetChunkIndexAndOffsetInChunk(List<ChunkInfo> blobMetadata, long requiredDataOffset, long size)
        {
            Dictionary<int, long> retVal = new Dictionary<int, long>();

            double temp = requiredDataOffset/StaticChunkSize;
            int requiredChunkIndex = (int)Math.Ceiling(temp);
            long offSetInChunk = (requiredDataOffset % StaticChunkSize);

            retVal[requiredChunkIndex]= offSetInChunk;
            size = size - StaticChunkSize + offSetInChunk;

            while (size > 0)
            {
                requiredChunkIndex++;
                retVal[requiredChunkIndex] = 0;
                size = size - StaticChunkSize ;
            }

            /*
             * * Commenting out this one for now and assuming only static chunking
            foreach (ChunkInfo chunk in blobMetadata)
            {
                long requiredSegmentOffset_End = requiredDataOffset + size;
                long chunkEnd = chunk.roffset + chunk.rsize;

                // if the required segment (offset + size) lie within this chunk 
                if (requiredDataOffset >= chunk.roffset && requiredDataOffset <= chunkEnd
                    && requiredSegmentOffset_End >= chunk.roffset && requiredSegmentOffset_End <= chunkEnd)
                {
                    retVal.Add(chunk.chunkIndex, requiredDataOffset - chunk.roffset);
                    return retVal;
                }
                // if only the offset lies within the chunk but the segment straddles across chunk boundaries
                else if (requiredDataOffset >= chunk.roffset && requiredDataOffset < chunkEnd
                    && requiredSegmentOffset_End > chunkEnd)
                {
                    retVal.Add(chunk.chunkIndex, requiredDataOffset - chunk.roffset);
                }

                else if (requiredSegmentOffset_End > chunk.roffset && requiredSegmentOffset_End <= chunkEnd)
                {
                    retVal.Add(chunk.chunkIndex,0);
                    break;
                }
                else if (requiredDataOffset < chunk.roffset && requiredSegmentOffset_End > chunkEnd)
                {
                    retVal.Add(chunk.chunkIndex, 0);
                }
            }*/
            return retVal;
        }


        public byte[] DownloadChunk(string blobName,List<ChunkInfo> blobMetadata,  int chunkIndex)
        {

            long offset = 0; 
            int size = 0; 
            foreach (ChunkInfo chunk in blobMetadata)
            {
                if (chunk.chunkIndex == chunkIndex)
                {
                    if (chunkCompressionType == CompressionType.None)
                        size = chunk.rsize;
                    else
                        size = chunk.csize;
                    break;
                }
                else
                {
                    if (chunkCompressionType == CompressionType.None)
                        offset += chunk.rsize;
                    else
                        offset += chunk.csize;
                }
            }


            byte[] buffer = new byte[size];
            try
            {
                CloudBlockBlob blockBlob = GetBlockBlobReference(blobName, false);
                HttpWebRequest blobGetRequest = BlobRequest.Get(blockBlob.Uri, 60, null, null);

                // Add header to specify the range
                blobGetRequest.Headers.Add("x-ms-range", string.Format(System.Globalization.CultureInfo.InvariantCulture, "bytes={0}-{1}", offset, offset + size - 1));
                
                blobGetRequest.Headers.Add("x-ms-range-get-content-md5", "true"); // this tell azure to compute md5 of the byte range and return in the Content-MD5 header
                // Note: it only works for byte ranges that are <= 4MB. So will not work for chunk sizes more than that

                // Sign request.
                StorageCredentials credentials = blockBlob.ServiceClient.Credentials;
                credentials.SignRequest(blobGetRequest);


                // Read chunk.
                while (true)
                {
                    using (HttpWebResponse response = blobGetRequest.GetResponse() as
                        HttpWebResponse)
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            int offsetInBuffer = 0;
                            int remaining = size;
                            while (remaining > 0)
                            {
                                int read = stream.Read(buffer, offsetInBuffer, remaining);
                                offsetInBuffer += read;
                                remaining -= read;
                            }

                        }

                        string contentMD5 = response.GetResponseHeader("Content-MD5");
                        if (!GetMD5FromStream(buffer).Equals(contentMD5, StringComparison.CurrentCultureIgnoreCase))
                        {
                            structuredLog("ER", "Content md5 does not match in download of chunk. retrying");
                        }
                        else
                            break;

                    }
                }

                if (logger != null) logger.Log("Start Synchronizer Decrypt Chunk");
                Byte[] ret = Decrypt(buffer);
                if (logger != null) logger.Log("End Synchronizer Decrypt Chunk");
                if (logger != null) logger.Log("Start Synchronizer Decompress Chunk");
                ret = Decompress(ret);
                if (logger != null) logger.Log("End Synchronizer Decompress Chunk");
                return ret;
            }

            catch (Exception e)
            {
                structuredLog("E", " Exception in DownloadChunk: " + e);
                return null;
            }



        }

        #endregion


        #region chunk-level compression and decompression stuff


        private byte[] Compress(byte[] input)
        {
            byte[] retVal = null;

            switch (this.chunkCompressionType)
            {
                case CompressionType.None:
                    retVal=input;
                    break;
                case CompressionType.GZip:
                    retVal = GZipCompress(input);
                    break;
                case CompressionType.BZip2:
                    retVal = BZip2Compress(input);
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
                    retVal = GZipDecompress(input);
                    break;
                case CompressionType.BZip2:
                    retVal = BZip2Decompress(input);
                    break;
                default:
                    throw new NotImplementedException("compression type " + this.chunkCompressionType + " not implemented");
            }
            return retVal;
        }

        public static byte[] GZipCompress(byte[] input)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream compressionStream = new GZipStream(memoryStream, CompressionLevel.Fastest))
                {
                    compressionStream.Write(input, 0, input.Length);
                    compressionStream.Close();
                    return memoryStream.ToArray();
                }
            }
        }

        public static byte[] GZipDecompress(byte[] input)
        {
            const int size = 4096;
            byte[] buffer = new byte[size];
            using (MemoryStream memoryStream = new MemoryStream(input))
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    using (MemoryStream outputMemoryStream = new MemoryStream())
                    {
                        int count = 0;
                        do
                        {
                            count =gzipStream.Read(buffer, 0, size);
                            if (count > 0)
                            {
                                outputMemoryStream.Write(buffer, 0, count);
                            }
                        }
                        while (count > 0);
                        return outputMemoryStream.ToArray();
                    }
                }
            }

        }

        
        public static byte[] BZip2Compress(byte[] input)
        {
            const int compressionLevel = 9 ; 

            using (MemoryStream inputMemoryStream = new MemoryStream(input))
            using(MemoryStream outputMemoryStream = new MemoryStream())
            {
                BZip2.Compress(inputMemoryStream, outputMemoryStream, true, compressionLevel);
                return outputMemoryStream.ToArray();
            }
        }

        public static byte[] BZip2Decompress(byte[] input)
        {
            using (MemoryStream inputMemoryStream = new MemoryStream(input))
            using (MemoryStream outputMemoryStream = new MemoryStream())
            {
                BZip2.Decompress(inputMemoryStream, outputMemoryStream, true);
                return outputMemoryStream.ToArray();
            }
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


        public void CleanAccount(string pattern="")
        {
            CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentialsAccountAndKey(this.remoteInfo.accountName, this.remoteInfo.accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            foreach (CloudBlobContainer blobcontainer in blobClient.ListContainers())
            {
                if (!pattern.Equals("") && blobcontainer.Name.Contains(pattern))
                    blobcontainer.Delete(GetBlobRequestOptions());
                else
                    blobcontainer.Delete(GetBlobRequestOptions());   
            }

        }

    }


    [DataContract]
    public class FileMD
    {
        [DataMember]
        public int StaticChunkSize { get; set; }
        [DataMember]
        public string compressionType { get; set; }
        [DataMember]
        public string encryptionType { get; set; }

        [DataMember]
        public List<ChunkInfo> ChunkList { get; set; }

        public FileMD(int staticchunksize, List<ChunkInfo> chunkList, string chunkCompressionType, string encryptionType)
        {
            this.StaticChunkSize = staticchunksize;
            this.ChunkList = chunkList;
            this.compressionType = chunkCompressionType;
            this.encryptionType = encryptionType;
        }
    }

    public class ChunkInfo : IEquatable<ChunkInfo>
    {

        public ChunkInfo(int chunkIndex, int rawSize, string sha1, long rawOffset, string blobName = null)
        {
            this.chunkIndex = chunkIndex;
            this.rsize = rawSize;
            this.sha1 = sha1;
            this.roffset = rawOffset;
            this.blockList = new List<int>();
            this.blobName = blobName;
        }

        public int chunkIndex;
        public string sha1;

        public int rsize; // raw (uncompressed) in bytes
        public long roffset; // offset in the raw file

        public int csize;// compressed size in bytes
       
        public List<int> blockList;// blockIDs corresponding to the chunk
        public string blobName;

        public void AddToBlockList(int blockID)
        {
            blockList.Add(blockID);
        }

        public bool Equals(ChunkInfo chunk)
        {
            if (this.chunkIndex == chunk.chunkIndex && this.sha1.Equals(chunk.sha1, StringComparison.CurrentCultureIgnoreCase))
                return true;
            else
                return false;

        }

        public void SetCSize(int compressizedSize)
        {
            this.csize = compressizedSize;
        }

        public void SetBlobName(string blobName)
        {
            this.blobName = blobName;
        }

        public override string ToString()
        {
            return "ChunkIndex: " + chunkIndex + " sha1:" + sha1 + " size: " + rsize + " offset: " + roffset;
        }


    }


    public class StaticChunk : IChunk
    {

        private long chunkSize; 

        public StaticChunk(long chunkSize)
        {
            this.chunkSize = chunkSize;
        }


        public List<ChunkInfo> GetCurrentChunkList(string filePath)
        {
            List<ChunkInfo> retVal = new List<ChunkInfo>();
            try
            {
                byte[] buffer = new byte[this.chunkSize];
                int bytesRead;
                int chunkIndex = 0; // chunk indices start at 0
                int offset = 0;
                string hash; 
                

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        SHA1 sha1 = new SHA1CryptoServiceProvider();
                        hash = Convert.ToBase64String(sha1.ComputeHash(buffer, 0 , bytesRead));

                        retVal.Add(new ChunkInfo(chunkIndex, bytesRead, hash, offset));
                        chunkIndex++;
                        offset += bytesRead;
                    }
                    fs.Close();
                }

            }

            catch (Exception e)
            {
                AzureHelper.structuredLog("E", e.Message + ". GetStaticChunks: " + filePath + ". " + e);
            }
            return retVal;
        }

        public List<ChunkInfo>  GetUploadChunkList(List<ChunkInfo> chunkList_local, List<ChunkInfo> chunkList_cloud)
        {
            List<ChunkInfo> chunkList_toUpload = new List<ChunkInfo>();
            
            foreach (ChunkInfo chunk in chunkList_local)
            {
                if (!chunkList_cloud.Contains(chunk))
                {
                    chunkList_toUpload.Add(chunk);
                }


            }
            return chunkList_toUpload;
        }
    
    }


    public interface IChunk
    {
        /// <summary>
        /// For the given file return a list of (chunk-index, chunkInfo) tuples ; chunk-indices start at 0
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        List<ChunkInfo> GetCurrentChunkList(string filePath);


        List<ChunkInfo> GetUploadChunkList(List<ChunkInfo> chunkList_local, List<ChunkInfo> chunkList_cloud);
    }
}


/*  if (chunkList_toUpload[chunkIndex].size <= MaxAzureBlockSize)
                    {
                        fs.Seek(chunkList_toUpload[chunkIndex].offset , SeekOrigin.Begin); // move the file pointer to beginning of the chunk
                        
                        BinaryReader br = new BinaryReader(fs);
                        byte[] buff = new byte[chunkList_toUpload[chunkIndex].size];// create new buffer of the size of  the chunk to be uploaded
                        br.Read(buff, 0, chunkList_toUpload[chunkIndex].size);//read chunk into buffer
                        string blockIdBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockID.ToString(CultureInfo.InvariantCulture).PadLeft(16,'0')));
                        using (MemoryStream ms = new MemoryStream(buff, 0, chunkList_toUpload[chunkIndex].size))
                        {
                            blockBlob.PutBlock(blockIdBase64, ms, GetMD5FromStream(buff), GetBlobRequestOptions());
                        }
                        blockIDList.Add(blockIdBase64);
                        blockID++;
                        buff = null;
                    }
                    else 
 
 private byte[] DownloadBlockBlob(CloudBlockBlob blockBlob)
        {
            byte[] retVal = null;
            try
            {
                
                string url = blockBlob.Uri.ToString();
                if (blockBlob.ServiceClient.Credentials.NeedsTransformUri)
                {
                    url = blockBlob.ServiceClient.Credentials.TransformUri(url);
                }

                var req = BlobRequest.Get(new Uri(url), Timeout, null , null);
                blockBlob.ServiceClient.Credentials.SignRequest(req);

                using (var reader = new BinaryReader(req.GetResponse().GetResponseStream()))
                {
                    retVal = reader.ReadBytes(int.MaxValue);
                }
                req.GetResponse().GetResponseStream().Close();
            }
            catch (Exception e)
            {
                structuredLog("E", e.Message + ". DownloadBlockBlob: " + blockBlob.ToString() + ". " + e);
            }
            return retVal;
        }
 */

/*
        private bool UploadChunkListToBlob(List<ChunkInfo> chunkList_toUpload,string filePath, string blobName)
        {
            if (this.azureBlobType != AzureBlobType.BlockBlob)
                throw new NotImplementedException("only implemented block blobs");

            if (this.mapping != Mapping.FileToBlob )
                throw new NotImplementedException("only implemented file to blob");

            if (!File.Exists(filePath))
                return false;

            if (chunkList_toUpload.Count <= 0 )
                return false;

            // the following code handles only the static chunking case 
            int blockID ;
            CloudBlockBlob blockBlob = GetBlockBlobReference(blobName);
            List<string> blockIDList = new List<string>();
            if(BlockBlobExists(blockBlob))
                blockIDList.AddRange(blockBlob.DownloadBlockList(BlockListingFilter.Committed).Select(b => b.Name));

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                foreach (long chunkIndex in chunkList_toUpload.Keys)
                {
                    blockID = (int)chunkIndex * (int)(StaticChunkSize/MaxAzureBlockSize);
                    int blockCount = 0; // we may have multiple blocks of MaxAzureBlockSize size per chunk

                    if (chunkList_toUpload[chunkIndex].size > MaxAzureBlockSize && chunkList_toUpload[chunkIndex].size % MaxAzureBlockSize != 0)
                        throw new NotImplementedException("chunk size (current: " + chunkList_toUpload[chunkIndex].size + ") has to be a fixed multiple of maximum block size :" + MaxAzureBlockSize);


                    while (blockCount * MaxAzureBlockSize < chunkList_toUpload[chunkIndex].size)
                    {
                        byte[] buff = new byte[Math.Min(MaxAzureBlockSize, chunkList_toUpload[chunkIndex].size - blockCount * MaxAzureBlockSize)];// create new buffer of the required size (this could also be the last block of the chunk)
                        fs.Seek(chunkList_toUpload[chunkIndex].offset + blockCount * MaxAzureBlockSize, SeekOrigin.Begin); // move the file pointer to beginning of the chunk
                        BinaryReader br = new BinaryReader(fs);

                        br.Read(buff, 0, buff.Length);//read chunk into buffer
                        string blockIdBase64 = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(blockID.ToString(CultureInfo.InvariantCulture).PadLeft(16, '0')));
                        using (MemoryStream ms = new MemoryStream(buff, 0, buff.Length))
                        {
                            blockBlob.PutBlock(blockIdBase64, ms, GetMD5FromStream(buff), GetBlobRequestOptions());
                        }

                        if (!blockIDList.Contains(blockIdBase64))
                            blockIDList.Add(blockIdBase64);
                        blockID++;
                        blockCount++;
                        buff = null;
                    }

                    structuredLog("I", "Chunk pushed as block(s). Chunk Index: " + chunkIndex + " . Size: " + chunkList_toUpload[chunkIndex].size + ". sha1: " + chunkList_toUpload[chunkIndex].sha1);
                }

                blockBlob.PutBlockList(blockIDList, GetBlobRequestOptions());
                structuredLog("I", "PutBlockList. Number of blocks commited: " + blockIDList.Count);
                fs.Close();
                return true;
            }
        }
 * 
 *  Secret function to clean storage account when there are lots of test streams
 * public void CleanAccount()
        {
            CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentialsAccountAndKey(this.remoteInfo.accountName, this.remoteInfo.accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            foreach (CloudBlobContainer blobcontainer in blobClient.ListContainers())
            {
                if(blobcontainer.Name.Contains("teststream"))
                    blobcontainer.Delete(GetBlobRequestOptions());
            }
            
        }


        */
//    {