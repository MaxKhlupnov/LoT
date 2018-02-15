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

namespace HDS
{
    public class AzureSynchronizer : ISync, IDisposable
    {
        protected bool disposed;
        protected SyncOrchestrator orchestrator;
        string indexFileName;

        public AzureSynchronizer(RemoteInfo ri, string container, SynchronizeDirection syncDirection)
        {
            disposed = false;
            string _containerName = container;

            //
            // Setup Store and Provider
            //
            CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentialsAccountAndKey(ri.accountName, ri.accountKey), true);
            AzureBlobStore blobStore = new AzureBlobStore(_containerName, storageAccount);
            Console.WriteLine("Successfully created/attached to container {0}.", _containerName);
            AzureBlobSyncProvider azureProvider = new AzureBlobSyncProvider(_containerName, blobStore);
            azureProvider.ApplyingChange += new EventHandler<ApplyingBlobEventArgs>(UploadingFile);

            orchestrator = new SyncOrchestrator();
            orchestrator.RemoteProvider = azureProvider;

            if (syncDirection == SynchronizeDirection.Upload)
                orchestrator.Direction = SyncDirectionOrder.Upload;
            else if (syncDirection == SynchronizeDirection.Download)
                orchestrator.Direction = SyncDirectionOrder.Download;
        }

        public void SetLocalSource(string FqDirName)
        {
            if (!Directory.Exists(FqDirName))
            {
                Console.WriteLine("Please ensure that the local target directory exists.");
                throw new ArgumentException("Please ensure that the local target directory exists.");
            }
            
            string _localPathName = FqDirName;
            FileSyncProvider fileSyncProvider = null;
            FileSyncScopeFilter filter = new FileSyncScopeFilter();
            filter.FileNameExcludes.Add(".*");
            filter.FileNameExcludes.Add("filesync.metadata");


            // TODO: Exclude subdirectories and remove this hack
            for (int i = 0; i < 100; i++)
            {
                filter.SubdirectoryExcludes.Add("" + i);
            }
            try
            {
                fileSyncProvider = new FileSyncProvider(_localPathName, filter, new FileSyncOptions());
            }
            catch (ArgumentException)
            {
                fileSyncProvider = new FileSyncProvider(Guid.NewGuid(), _localPathName, filter, new FileSyncOptions());
            }
            //fileSyncProvider.ApplyingChange += new EventHandler<ApplyingChangeEventArgs>(DownloadingFile);

            orchestrator.LocalProvider = fileSyncProvider;
        }
        
        public bool Sync()
        {
            
            bool status = false;
            if (orchestrator.LocalProvider != null) {
                SyncOperationStatistics sos = orchestrator.Synchronize();
                Console.WriteLine("Synchronization Complete");
                status = true;
            }
            return status;
        }
        public void SetIndexFileName(string indexFileName)
        {
            this.indexFileName = indexFileName;
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

        ~AzureSynchronizer()
        {
            Dispose(false);
        }

        /*
        public static void DownloadingFile(object sender, ApplyingChangeEventArgs args)
        {
            switch (args.ChangeType)
            {
                case ChangeType.Create:
                    Console.WriteLine("Creating File: {0}...", args.NewFileData.Name);
                    break;
                case ChangeType.Delete:
                    Console.WriteLine("Deleting File: {0}...", args.CurrentFileData.Name);
                    break;
                case ChangeType.Rename:
                    Console.WriteLine("Renaming File: {0} to {1}...", args.CurrentFileData.Name, args.NewFileData.Name);
                    break;
                case ChangeType.Update:
                    Console.WriteLine("Updating File: {0}...", args.NewFileData.Name);
                    break;
            }
        }
        */

        public static void UploadingFile(object sender, ApplyingBlobEventArgs args)
        {
            switch (args.ChangeType)
            {
                case ChangeType.Create:
                    Console.WriteLine("Creating Azure Blob: {0}...", args.CurrentBlobName);
                    break;
                case ChangeType.Delete:
                    Console.WriteLine("Deleting Azure Blob: {0}...", args.CurrentBlobName);
                    break;
                case ChangeType.Rename:
                    Console.WriteLine("Renaming Azure Blob: {0} to {1}...", args.CurrentBlobName, args.NewBlobName);
                    break;
                case ChangeType.Update:
                    Console.WriteLine("Updating Azure Blob: {0}...", args.CurrentBlobName);
                    break;
            }
        }



        public void SetDataFileName(string dataFileName)
        {
            throw new NotImplementedException("not implemented ");
        }


        public byte[] ReadData(long offset, long byteCount)
        {
            throw new NotImplementedException("not implemented ");
        }

        public bool Delete()
        {
            //todo 
            return false;
        }

        public byte[] GetChunkListHash()
        {
            throw new NotImplementedException("not implemented ");
        }

        public bool DownloadFile(string blobName, string filePath)
        {
            throw new NotImplementedException("not implemented");
        }
    }
}
