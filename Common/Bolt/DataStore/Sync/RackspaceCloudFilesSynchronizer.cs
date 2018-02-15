using net.openstack.Core.Domain;
using net.openstack.Providers.Rackspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    class RackspaceCloudFilesSynchronizer : ISync, IDisposable
    {
        protected bool disposed;
        private string localSource;
        private string username;
        private string apiKey;
        private SynchronizeDirection syncDirection;
        private string container,indexFileName;

        public RackspaceCloudFilesSynchronizer(RemoteInfo ri, string container, SynchronizeDirection syncDirection)
        {
            this.disposed = false;
            this.username = ri.accountName;
            this.apiKey = ri.accountKey;
            this.syncDirection = syncDirection;
            this.container = container;
            try
            {
                var cloudIdentity = new CloudIdentity() { APIKey = this.apiKey, Username = this.username };
                var cloudFilesProvider = new CloudFilesProvider(cloudIdentity);
                ObjectStore createContainerResponse = cloudFilesProvider.CreateContainer(container);// assume default region for now

                if (!createContainerResponse.Equals(ObjectStore.ContainerCreated) && !createContainerResponse.Equals(ObjectStore.ContainerExists))
                    Console.WriteLine("Container creation failed! Response: " + createContainerResponse.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in creating container: " + e);
            }

        }

        public bool Sync()
        {

            if (syncDirection == SynchronizeDirection.Upload)
                return UploadToRackSpaceCloudFiles();

            if (syncDirection == SynchronizeDirection.Download)
                return DownloadFromRackSpaceCloudFiles();

            return true;
        }

        public bool DownloadFromRackSpaceCloudFiles()
        {
            bool syncSucceeded = true;
            try
            {
                var cloudIdentity = new CloudIdentity() { APIKey = this.apiKey, Username = this.username };
                var cloudFilesProvider = new CloudFilesProvider(cloudIdentity);
                IEnumerable<ContainerObject> containerObjectList = cloudFilesProvider.ListObjects(container);

                foreach (ContainerObject containerObject in containerObjectList)
                {
                    cloudFilesProvider.GetObjectSaveToFile(container, localSource, containerObject.Name, containerObject.Name);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in downloading from rackspace: " + e);
                syncSucceeded = false; 
            }
            return syncSucceeded;
        }

        public bool UploadToRackSpaceCloudFiles()
        {
            bool syncSucceeded = true;
            try
            {
                var cloudIdentity = new CloudIdentity() { APIKey = this.apiKey, Username = this.username };
                var cloudFilesProvider = new CloudFilesProvider(cloudIdentity);
                
                List<string> fileList = AmazonS3Helper.ListFiles(localSource);

                foreach (string file in fileList)
                {
                    cloudFilesProvider.CreateObjectFromFile(container, file, Path.GetFileName(file));
                    // assuming this overwrites file if it exists.
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in uploading to rackspace: "+e);
                syncSucceeded = false; 
            }

            return syncSucceeded;
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
                    
                }
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
