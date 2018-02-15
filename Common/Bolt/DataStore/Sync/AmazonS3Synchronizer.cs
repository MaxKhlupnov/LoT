using Amazon.S3;
using Amib.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class AmazonS3Synchronizer : ISync, IDisposable
    {
        protected Logger logger;
        protected bool disposed;
        private string localSource;
        private string bucketName, indexFileName, dataFileName;

        private SynchronizeDirection syncDirection;
        private AmazonS3Helper amazonS3Helper;
        private byte[] chunkListHash;
        List<ChunkInfo> dataFileMetadata = null;

        // constants
        private const string DataChunkDirPrefix = "ChunkCache-";
        private const string DataChunkFilePrefix = "Chunk-";
        public int MaxConcurrentFileSyncThreads; 

        /// <summary>
        /// We-use the remote info as: accountName = awsAccessKeyId and accountKey = awsSecretAccessKey
        /// </summary>
        public AmazonS3Synchronizer(RemoteInfo remoteInfo, string bucket, SynchronizeDirection syncDirection, CompressionType compressionType, EncryptionType encryptionType, byte[] encryptionKey, byte[] initializationVector, Logger log, int ChunkSize,  int ThreadPoolSize = 1 )
        {
            this.logger = log;
            disposed = false;
            this.syncDirection = syncDirection;
            bucketName = bucket.ToString().Replace(' ', '-').ToLower(); ;// amazon S3 does not like spaces in bucket names
            
            amazonS3Helper = new AmazonS3Helper(remoteInfo, bucket, compressionType, encryptionType, encryptionKey, initializationVector, logger, ChunkSize, ThreadPoolSize);
            this.MaxConcurrentFileSyncThreads = ThreadPoolSize;
        }

        public void SetLocalSource(string FqDirName)
        {
            if (!Directory.Exists(FqDirName))
            {
                Console.WriteLine("Please ensure that the local target directory exists.");
                throw new ArgumentException("Please ensure that the local target directory exists.");
            }

            localSource = FqDirName;
        }

        public void SetIndexFileName(string indexFileName)
        {
            this.indexFileName = indexFileName;
        }

        public void SetDataFileName(string dataFileName)
        {
            this.dataFileName = dataFileName;
        }

        public bool Sync()
        {
            if (syncDirection == SynchronizeDirection.Upload)
            {
                if (logger != null) logger.Log("Start Synchronizer Enlisting Files");
                List<string> fileList = AzureHelper.ListFiles(localSource, this.indexFileName, this.dataFileName);
                List<string> fileListToUpload = new List<string>(fileList);
                if (logger != null) logger.Log("End Synchronizer Enlisting Files");
                // todo filter list, to exclude archival stream files.
                bool retVal = true;

                foreach (string file in fileList)
                {
                    if (Path.GetFileName(file).Equals(this.indexFileName))
                    {
                        if (logger != null) logger.Log("Start Synchronizer Upload FileSimple");
                        retVal = amazonS3Helper.UploadFileToS3Object(indexFileName, file);
                        if (logger != null) logger.Log("End Synchronizer Upload FileSimple");
                        fileListToUpload.Remove(file);
                    }
                    else if (Path.GetFileName(file).Equals(this.dataFileName))
                    {
                        if (logger != null) logger.Log("Start Synchronizer Upload FileAsChunks");
                        chunkListHash = amazonS3Helper.UploadFileAsChunks(file);
                        if (logger != null) logger.Log("End Synchronizer Upload FileAsChunks");
                        retVal = (chunkListHash == null) ? false : true;
                        fileListToUpload.Remove(file);
                    }

                    if (SyncFactory.FilesExcludedFromSync.Contains(Path.GetFileName(file)) || SyncFactory.FilesExcludedFromSync.Contains(Path.GetExtension(file)))
                    {
                        // AzureHelper.structuredLog("I", "Ignoring sync for file {0}" , file);
                        // do nothing. just ignore the file
                        fileListToUpload.Remove(file);
                    }
                } //we've taken care of the index, data, and files to ignore. whatever is left are datablock files for file data streams (?)


                if (logger != null) logger.Log("Start Synchronizer Upload FDSFiles");
                SmartThreadPool threadPool = new SmartThreadPool();
                threadPool.MaxThreads = this.MaxConcurrentFileSyncThreads;
                foreach (string file in fileListToUpload)
                {
                    IWorkItemResult wir1 = threadPool.QueueWorkItem(new WorkItemCallback(this.UploadFileToBlockBlob_worker), file);
                }
                threadPool.Start();
                threadPool.WaitForIdle();
                threadPool.Shutdown();
                if (logger != null) logger.Log("End Synchronizer Upload FDSFiles");
            }

            if (syncDirection == SynchronizeDirection.Download)
            {
                this.GetDataFileMetadata();// get chunkMD and populate chunkList Hash
                return amazonS3Helper.DownloadS3ObjectToFile(indexFileName, localSource + "/" + indexFileName);// download index 
            }

            return true;//TODO fix this to return correct value for upload cases
        }


        private object UploadFileToBlockBlob_worker(object state)
        {
            string filePath = (string)state;
            bool retVal = amazonS3Helper.UploadFileToS3Object(Path.GetFileName(filePath), filePath);

            if (!retVal)
                AmazonS3Helper.structuredLog("E", "UploadFile for {0} failed ", filePath);
            return null;
        }


        ~AmazonS3Synchronizer()
        {
            Dispose(false);
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
                    localSource = null;
                    bucketName=null;
                    indexFileName = null;
                    dataFileName = null;
                    amazonS3Helper.Dispose();
                    disposed = true;
                }
            }
        }


         


         public byte[] ReadData(long offset, long byteCount)
         {
             GetDataFileMetadata();

             Dictionary<int, long> chunkindexandoffsets = amazonS3Helper.GetChunkIndexAndOffsetInChunk(dataFileMetadata, offset, byteCount);

             byte[] buffer = null;
             long bytesToRead = byteCount;
             foreach (int chunkIndex in chunkindexandoffsets.Keys)
             {
                 if (buffer != null)
                     buffer = buffer.Concat(FetchChunk(chunkIndex, chunkindexandoffsets[chunkIndex], bytesToRead)).ToArray();
                 else
                     buffer = FetchChunk(chunkIndex, chunkindexandoffsets[chunkIndex], bytesToRead);
                 bytesToRead = bytesToRead - buffer.Length;
             }

             byte[] retVal = new Byte[byteCount];
             Buffer.BlockCopy(buffer, 0, retVal, 0, (int)byteCount);
             //Buffer.BlockCopy(buffer, (int)chunkindexandoffsets.ElementAt(0).Value, retVal, 0, (int)byteCount);

             return retVal;
         }

         public bool Delete()
         {
             return amazonS3Helper.DeleteBucket();
         }


         public byte[] GetChunkListHash()
         {
             if (this.chunkListHash != null)
                 return chunkListHash;
             else
                 throw new Exception("attempting to getChunkListHash without calling Sync()");
         }

         public bool DownloadFile(string s3objectName, string filePath)
         {
             return amazonS3Helper.DownloadS3ObjectToFile(s3objectName, filePath);
         }

         private void GetDataFileMetadata()
         {
             if (dataFileMetadata == null)
             {
                 Tuple<List<ChunkInfo>, byte[]> retVal = amazonS3Helper.GetObjectMetadata(dataFileName);
                 dataFileMetadata = retVal.Item1;
                 chunkListHash = retVal.Item2;
             }
         }

        /*private byte[] FetchChunk(int chunkIndex)
        {
            GetDataFileMetadata();
            byte[] chunkData =  ReadFromChunkCache(chunkIndex);

            if (chunkData == null)
            {
                chunkData = amazonS3Helper.DownloadChunk(dataFileName, chunkIndex);
                UpdateChunkCache(chunkData,chunkIndex);
            }
            return chunkData;
        }*/

        private byte[] FetchChunk(int chunkIndex, long offsetInChunk, long bytesToRead)
        {
            GetDataFileMetadata();
            if (logger != null) logger.Log("Start Synchronizer ReadFromCache");
            byte[] chunkData = ReadFromChunkCache(chunkIndex, offsetInChunk, bytesToRead);
            if (logger != null) logger.Log("End Synchronizer ReadFromCache");

            if (chunkData == null)
            {
                if (logger != null) logger.Log("Start Synchronizer Actually Downloading Chunk");
                chunkData = amazonS3Helper.DownloadChunk(dataFileName, chunkIndex);
                if (logger != null) logger.Log("End Synchronizer Actually Downloading Chunk");
                if (logger != null) logger.Log("Start Synchronizer Update Chunk Cache");
                UpdateChunkCache(chunkData, chunkIndex);

                int end = Math.Min((int)bytesToRead, chunkData.Length);
                byte[] retVal = new byte[end];
                Buffer.BlockCopy(chunkData, (int)offsetInChunk, retVal, 0, end);


                if (logger != null) logger.Log("End Synchronizer Update Chunk Cache");
            }
            return chunkData;
        }


        private void UpdateChunkCache(byte[] chunkData, int chunkIndex, bool overwrite = false)
        {
            string DataChunkCacheDir = GetDataChunkCacheDir();
            string DataChunkFileName = GetDataChunkFileName(chunkIndex, DataChunkCacheDir);

            try
            {
                if (!Directory.Exists(DataChunkCacheDir))
                {
                    Directory.CreateDirectory(DataChunkCacheDir);
                    DirectoryInfo info = new DirectoryInfo(DataChunkCacheDir);
                    // stuff that deals with windows's dir access policies.
                    System.Security.AccessControl.DirectorySecurity security = info.GetAccessControl();
                    security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(Environment.UserName, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.InheritanceFlags.ContainerInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
                    security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(Environment.UserName, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.InheritanceFlags.ContainerInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
                    info.SetAccessControl(security);
                }

                if (!File.Exists(DataChunkFileName) || overwrite)
                {
                    File.WriteAllBytes(DataChunkFileName, chunkData);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("exception in updating chunk cache: " + e);
            }


        }

        private byte[] ReadFromChunkCache(int chunkIndex, long offsetInChunk, long bytesToRead)
        {
            if (logger != null) logger.Log("Start ReadFromCache Build FileName");
            string DataChunkCacheDir = GetDataChunkCacheDir();
            string DataChunkFileName = GetDataChunkFileName(chunkIndex, DataChunkCacheDir);
            if (logger != null) logger.Log("End ReadFromCache Build FileName");
            Byte[] ret = null;
            try
            {
                //if (!Directory.Exists(DataChunkCacheDir))
                //  return null;
                // if (File.Exists(DataChunkFileName))
                // {
                if (logger != null) logger.Log("Start ReadFromCache ReadAllBytes");
                if (logger != null) logger.Log("Start ReadFromCache Open");
                FileStream fout = new FileStream(DataChunkFileName,
                                            FileMode.Open,
                                            FileAccess.Read,
                                            FileShare.ReadWrite);
                if (logger != null) logger.Log("End ReadFromCache Open");
                fout.Seek(offsetInChunk, SeekOrigin.Begin);
                //read file to MemoryStream
                int minToRead = Math.Min((int)(fout.Length - offsetInChunk) + 1, (int)bytesToRead);
                byte[] bytes = new byte[minToRead];
                fout.Read(bytes, 0, minToRead);
                // fout.Close();
                ret = bytes; //File.ReadAllBytes(DataChunkFileName);
                if (logger != null) logger.Log("End ReadFromCache ReadAllBytes");
            }
            catch (DirectoryNotFoundException e)
            {
                if (logger != null) logger.Log("End ReadFromCache Open");
                if (logger != null) logger.Log("End ReadFromCache ReadAllBytes");
                Console.WriteLine("exception in reading chunk cache: " + e);
                return null;
            }
            catch (FileNotFoundException e)
            {
                if (logger != null) logger.Log("End ReadFromCache Open");
                if (logger != null) logger.Log("End ReadFromCache ReadAllBytes");
                Console.WriteLine("exception in reading chunk cache: " + e);
                return null;
            }
            catch (Exception e)
            {
                if (logger != null) logger.Log("End ReadFromCache ReadAllBytes");
                Console.WriteLine("exception in reading chunk cache: " + e);
                return null;
            }
            return ret;
        }

       
        private String GetDataChunkCacheDir()
        {
            StringBuilder s = new StringBuilder(this.localSource, this.localSource.Length + DataChunkDirPrefix.Length + dataFileName.Length + 10);
            s.Append("\\");
            s.Append(DataChunkDirPrefix);
            s.Append(dataFileName);
            return s.ToString();
        }

        private string GetDataChunkFileName(int chunkIndex, string DataChunkCacheDir)
        {
            string chunkIndex_string = chunkIndex.ToString();
            StringBuilder s = new StringBuilder(DataChunkCacheDir, DataChunkCacheDir.Length + DataChunkFilePrefix.Length + chunkIndex_string.Length + 10);
            s.Append("\\");
            s.Append(DataChunkFilePrefix);
            s.Append(chunkIndex_string);
            return s.ToString();
        }

         
    }
}
