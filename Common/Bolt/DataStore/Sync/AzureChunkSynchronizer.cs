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
using Amib.Threading;
using System.Security.Cryptography;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class AzureChunkSynchronizer : ISync, IDisposable
    {
        protected Logger logger;
        protected bool disposed;
        private SynchronizeDirection syncDirection;
        private string FqDirName;
        private string accountName, accountKey, container, indexFileName, dataFileName;
        

        List<ChunkInfo> dataFileMetadata = null; 
        private AzureHelper azureHelper;
        private byte[] chunkListHash;

        // constants
        private const string DataChunkDirPrefix =  "ChunkCache-";
        private const string DataChunkFilePrefix = "Chunk-";
        public int ThreadPoolSize; 
        //protected SyncOrchestrator orchestrator;

        public AzureChunkSynchronizer(RemoteInfo ri, string container, SynchronizeDirection syncDirection, CompressionType compressionType, EncryptionType encryptionType, byte[] encryptionKey, byte[] initializationVector, Logger log, int ChunkSize, int ThreadPoolSize=1)
        {
            logger = log;
            disposed = false;
            // Setup Store and Provider
            //
            this.accountName = ri.accountName;
            this.accountKey = ri.accountKey;
            this.container = container;
            this.syncDirection = syncDirection;
            this.azureHelper = new AzureHelper(this.accountName, this.accountKey, this.container, compressionType, encryptionType, encryptionKey, initializationVector, log, ChunkSize, ThreadPoolSize);
            this.chunkListHash = null;
            this.ThreadPoolSize = ThreadPoolSize;

          
        }

        public void SetLocalSource(string FqDirName)
        {
            if (!Directory.Exists(FqDirName))
            {
                Console.WriteLine("Please ensure that the local target directory exists.");
                throw new ArgumentException("Please ensure that the local target directory exists.");
            }
 
            this.FqDirName = FqDirName;

        }
        public void SetIndexFileName(string indexFileName)
        {
            this.indexFileName = indexFileName;
        }


        public bool Sync()
        {
            bool retVal = true;


            try
            {
                if (syncDirection == SynchronizeDirection.Upload)
                {
                    if (logger != null) logger.Log("Start Synchronizer Enlisting Files");
                    List<string> fileList = AzureHelper.ListFiles(FqDirName, this.indexFileName, this.dataFileName); //what if this returns empty list
                    List<string> fileListToUpload = new List<string>(fileList);
                    if (logger != null) logger.Log("End Synchronizer Enlisting Files");
                    // todo filter list, to exclude archival stream files.

                    IWorkItemResult[] workItemResults = new IWorkItemResult[this.ThreadPoolSize];
                    int i = 0;

                    foreach (string file in fileList)
                    {
                        if (Path.GetFileName(file).Equals(this.indexFileName))
                        {
                            if (logger != null) logger.Log("Start Synchronizer Upload FileSimple");
                            retVal = (azureHelper.UploadFileToBlockBlob(indexFileName, file)) && retVal;
                            if (logger != null) logger.Log("End Synchronizer Upload FileSimple");
                            fileListToUpload.Remove(file);
                        }
                        else if (Path.GetFileName(file).Equals(this.dataFileName))
                        {
                            if (logger != null) logger.Log("Start Synchronizer Upload FileAsChunks");
                            chunkListHash = azureHelper.UploadFileAsChunks(file);
                            if (logger != null) logger.Log("End Synchronizer Upload FileAsChunks");
                            retVal = ((chunkListHash == null) ? false : true) && retVal;
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
                    threadPool.MaxThreads = this.ThreadPoolSize;

                    foreach (string file in fileListToUpload)
                    {
                        workItemResults[i++] = threadPool.QueueWorkItem(new WorkItemCallback(this.UploadFileToBlockBlob_worker), file);
                    }
                    threadPool.Start();
                    threadPool.WaitForIdle();
                    for (int j = 0; j < i; j++)
                    {
                        retVal = retVal && ((bool)workItemResults[j].Result);
                    }
                    threadPool.Shutdown();

                    if (logger != null) logger.Log("End Synchronizer Upload FDSFiles");
                }

                if (syncDirection == SynchronizeDirection.Download)
                {
                    this.GetDataFileMetadata();// get chunkMD and populate chunkList Hash
                    retVal = azureHelper.DownloadBlockBlobToFile(indexFileName, FqDirName + "/" + indexFileName);// download index 
                }
            }
            catch(Exception e)
            {
                if (logger != null) logger.Log("Exception in Sync(): " + e.GetType() + ", " + e.Message);
                retVal = false;
            }

            return retVal;
        }


        private object UploadFileToBlockBlob_worker(object state)
        {
            string filePath = (string)state;
            bool retVal = azureHelper.UploadFileToBlockBlob(Path.GetFileName(filePath), filePath);

            if (!retVal)
                AzureHelper.structuredLog("E", "UploadFile for {0} failed ", filePath);
            return null;
        }

        public void Dispose() // NOT virtual
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalizer from running.
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Call Dispose() on other objects owned by this instance.
                    // You can reference other finalizable objects here.
                }

                // Release unmanaged resources owned by (just) this object.
                disposed = true;
            }
        }

        ~AzureChunkSynchronizer()
        {
            Dispose(false);
        }


        #region data reads


        public void SetDataFileName(string dataFileName)
        {
            this.dataFileName = dataFileName;
        }


        public byte[] ReadData(long offset, long byteCount)
        {
            if (logger != null) logger.Log("Start Synchronizer Chunking");
            GetDataFileMetadata();

            Dictionary<int, long> chunkindexandoffsets = azureHelper.GetChunkIndexAndOffsetInChunk(dataFileMetadata, offset, byteCount);

            byte[] buffer = null;
            long bytesToRead = byteCount;
            foreach (int chunkIndex in chunkindexandoffsets.Keys)
            {
                if (buffer != null)
                    buffer = buffer.Concat(FetchChunk(chunkIndex, chunkindexandoffsets[chunkIndex], bytesToRead)).ToArray();
                else
                    buffer = FetchChunk(chunkIndex,chunkindexandoffsets[chunkIndex], bytesToRead);
                bytesToRead = bytesToRead - buffer.Length;
            }

            byte[] retVal = new Byte[byteCount];
            Buffer.BlockCopy(buffer, 0, retVal, 0, (int)byteCount);
            //Buffer.BlockCopy(buffer, (int)chunkindexandoffsets.ElementAt(0).Value, retVal, 0, (int)byteCount);

            if (logger != null) logger.Log("End Synchronizer Chunking");
            return retVal;
        }


        public bool DownloadFile(string blobname, string filePath)
        {
            return azureHelper.DownloadBlockBlobToFile(blobname, filePath);
        }

        private void GetDataFileMetadata()
        {
            if (dataFileMetadata == null)
            {
                Tuple<List<ChunkInfo>,byte[]> retVal = azureHelper.GetBlobMetadata(dataFileName);
                dataFileMetadata = retVal.Item1;
                chunkListHash = retVal.Item2;
            }
        }

        private byte[] FetchChunk(int chunkIndex, long offsetInChunk, long bytesToRead)
        {
            GetDataFileMetadata();
            if (logger != null) logger.Log("Start Synchronizer ReadFromCache");
            byte[] chunkData = ReadFromChunkCache(chunkIndex, offsetInChunk, bytesToRead);
            if (logger != null) logger.Log("End Synchronizer ReadFromCache");

            if (chunkData == null)
            {
                if (logger != null) logger.Log("Start Synchronizer Actually Downloading Chunk");
                chunkData = azureHelper.DownloadChunk(dataFileName, dataFileMetadata, chunkIndex);
                if (logger != null) logger.Log("End Synchronizer Actually Downloading Chunk");
                if (logger != null) logger.Log("Start Synchronizer Update Chunk Cache");
                UpdateChunkCache(chunkData,chunkIndex);

                int end = Math.Min((int)bytesToRead, chunkData.Length);
                byte[] retVal = new byte[end];
                Buffer.BlockCopy(chunkData, (int)offsetInChunk, retVal, 0, end);
                if (logger != null) logger.Log("End Synchronizer Update Chunk Cache");
                return retVal;
            }
            return chunkData;
        }

        private byte[] ReadFromChunkCache(int chunkIndex, long offsetInChunk,long bytesToRead)
        {
            if (logger != null) logger.Log("Start ReadFromCache Build FileName");
            string DataChunkCacheDir = GetDataChunkCacheDir();
            string DataChunkFileName = GetDataChunkFileName(chunkIndex, DataChunkCacheDir);
            if (logger != null) logger.Log("End ReadFromCache Build FileName");
            Byte[] ret = null;
            try
            {
                if (logger != null) logger.Log("Start ReadFromCache ReadAllBytes");
                if (logger != null) logger.Log("Start ReadFromCache Open");
                FileStream fout = new FileStream(DataChunkFileName,
                                            FileMode.Open,
                                            FileAccess.Read,
                                            FileShare.ReadWrite);
                if (logger != null) logger.Log("End ReadFromCache Open");
                
                // Check if the chunkHash matches the one in the most recent chunkMD (dataFileMetadata)
                int expected_size = dataFileMetadata[chunkIndex].rsize;
                byte[] buffer = new byte[expected_size];
                bool cache_hit = false;
                if (fout.Length == dataFileMetadata[chunkIndex].rsize)
                {
                    /*
                    // TODO: Cache this result somewhere instead of taking a hash every time!
                    int bytesRead = fout.Read(buffer, 0, buffer.Length);
                    SHA1 sha1 = new SHA1CryptoServiceProvider();
                    string sha1_hash = Convert.ToBase64String(sha1.ComputeHash(buffer, 0 , bytesRead));
                    if (sha1_hash == dataFileMetadata[chunkIndex].sha1)
                    {
                    */
                        cache_hit = true;
                    /*
                    }
                    */
                }

                if (cache_hit)
                {
                    fout.Seek(offsetInChunk, SeekOrigin.Begin);
                    //read file to MemoryStream
                    int minToRead = Math.Min((int)(fout.Length - offsetInChunk) + 1, (int)bytesToRead);
                    byte[] bytes = new byte[minToRead];
                    fout.Read(bytes, 0, minToRead);
                    ret = bytes; //File.ReadAllBytes(DataChunkFileName);
                }
                fout.Close();
                if (logger != null) logger.Log("End ReadFromCache ReadAllBytes");
            }
            catch (DirectoryNotFoundException e)
            {
                if (logger != null) logger.Log("End ReadFromCache Open");
                if (logger != null) logger.Log("End ReadFromCache ReadAllBytes");
                //Console.WriteLine("exception in reading chunk cache: " + e);
                return null;
            }
            catch (FileNotFoundException e)
            {
                if (logger != null) logger.Log("End ReadFromCache Open");
                if (logger != null) logger.Log("End ReadFromCache ReadAllBytes");
                //Console.WriteLine("exception in reading chunk cache: " + e);
                return null;
            }
            catch (Exception e)
            {
                if (logger != null) logger.Log("End ReadFromCache ReadAllBytes");
                Console.WriteLine("exception in reading chunk cache: "+ e);
                ret = null;
            }
            return ret;
        }


        private void UpdateChunkCache(byte[] chunkData, int chunkIndex, bool overwrite=false)
        {

            string DataChunkCacheDir = GetDataChunkCacheDir();
            string DataChunkFileName = GetDataChunkFileName(chunkIndex, DataChunkCacheDir);

            try
            {
                if (logger != null) logger.Log("Start UpdateChunkCache Create Directory");
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
                if (logger != null) logger.Log("End UpdateChunkCache Create Directory");

                // if (!File.Exists(DataChunkFileName) || overwrite)
                //{
                if (logger != null) logger.Log("Start UpdateChunkCache WriteChunk");
                    FileStream fout = new FileStream(DataChunkFileName,
                                            FileMode.OpenOrCreate,
                                            FileAccess.Write,
                                            FileShare.ReadWrite);
                    fout.Write(chunkData, 0, chunkData.Length);
                    fout.Flush(true);
                    fout.Close();
                    if (logger != null) logger.Log("End UpdateChunkCache WriteChunk");
                   // File.WriteAllBytes(DataChunkFileName, chunkData);
                // }
            }
            catch (Exception e)
            {
                Console.WriteLine("exception in updating chunk cache: " + e);
            }


        }

        private String GetDataChunkCacheDir()
        {
            StringBuilder s = new StringBuilder(FqDirName, FqDirName.Length + DataChunkDirPrefix.Length + dataFileName.Length + 10);
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
        private void ClearChunkCache()
        {
            throw new NotImplementedException("cache is never cleared");
        }
        #endregion


        public bool Delete()
        {
            return azureHelper.DeleteContainer();
        }

        public byte[] GetChunkListHash()
        {
            if (this.chunkListHash != null)
                return chunkListHash;
            else
                throw new Exception("attempting to getChunkListHash without calling Sync()");
        }


    }
}
