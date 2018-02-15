using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Common;
using NLog;
using HomeOS.Hub.Tools.PackagerHelper;

namespace HomeOS.Hub.Tools.UpdateHelper
{
    public class AzureBlobConfigUpdate
    {
        // paths for storage on the blob store
        private const string actualConfigFilePathInHubFolder = "/config/actual/";
        private const string desiredConfigFilePathInHubFolder = "/config/desired/";
        private const string AzureConfigContainerName = "configs";

        private const int AzureBlobLeaseTimeout = 60; 

        public static bool UploadConfig(string configZipPath, string AzureAccountName, string AzureAccountKey, string orgID, string studyID, string homeID, string desiredConfigFilename, NLog.Logger logger = null)
        {
            Microsoft.WindowsAzure.CloudStorageAccount storageAccount = null;
            Microsoft.WindowsAzure.StorageClient.CloudBlobClient blobClient = null;
            Microsoft.WindowsAzure.StorageClient.CloudBlobContainer container = null;
            Microsoft.WindowsAzure.StorageClient.CloudBlockBlob blockBlob = null;
            string leaseId = null;

            try
            {
                storageAccount = new Microsoft.WindowsAzure.CloudStorageAccount(new Microsoft.WindowsAzure.StorageCredentialsAccountAndKey(AzureAccountName, AzureAccountKey), true);
                blobClient = storageAccount.CreateCloudBlobClient();
                container = blobClient.GetContainerReference(AzureConfigContainerName);
                container.CreateIfNotExist();
                blockBlob = container.GetBlockBlobReference(DesiredConfigBlobName(orgID, studyID, homeID, desiredConfigFilename));

                bool blobExists = BlockBlobExists(blockBlob);

                if (blobExists)
                    leaseId = AcquireLease(blockBlob, logger); // Acquire Lease on Blob
                else
                    blockBlob.Container.CreateIfNotExist();

                if (blobExists && leaseId == null)
                {
                    if (null != logger)
                    {
                        logger.Error("AcquireLease on Blob: " + DesiredConfigBlobName(orgID, studyID, homeID, desiredConfigFilename) + " Failed");
                    }
                    return false;
                }

                string url = blockBlob.Uri.ToString();
                if (blockBlob.ServiceClient.Credentials.NeedsTransformUri)
                {
                    url = blockBlob.ServiceClient.Credentials.TransformUri(url);
                }

                var req = BlobRequest.Put(new Uri(url), AzureBlobLeaseTimeout, new Microsoft.WindowsAzure.StorageClient.BlobProperties(), Microsoft.WindowsAzure.StorageClient.BlobType.BlockBlob, leaseId, 0);

                using (var writer = new BinaryWriter(req.GetRequestStream()))
                {
                    writer.Write(File.ReadAllBytes(configZipPath));
                    writer.Close();
                }

                blockBlob.ServiceClient.Credentials.SignRequest(req);
                req.GetResponse().Close();
                ReleaseLease(blockBlob, leaseId); // Release Lease on Blob
                return true;
            }
            catch (Exception e)
            {
                if (null != logger)
                {
                    logger.ErrorException("UploadConfig_Azure, configZipPath: " + configZipPath, e);
                }
                ReleaseLease(blockBlob, leaseId);
                return false;
            }
        }

        public static string DesiredConfigBlobName(string orgID, string studyID, string homeID, string desiredConfigFilename)
        {
            return "/" + orgID + "/" + studyID + "/" + homeID + desiredConfigFilePathInHubFolder + desiredConfigFilename;
        }

        public static bool DownloadConfig(string downloadedZipPath, string AzureAccountName, string AzureAccountKey, string orgID, string studyID, string homeID, string configFilename, NLog.Logger logger=null)
        {

            Microsoft.WindowsAzure.CloudStorageAccount storageAccount = null;
            Microsoft.WindowsAzure.StorageClient.CloudBlobClient blobClient = null;
            Microsoft.WindowsAzure.StorageClient.CloudBlobContainer container = null;
            Microsoft.WindowsAzure.StorageClient.CloudBlockBlob blockBlob = null;
            string leaseId = null;

            try
            {
                storageAccount = new Microsoft.WindowsAzure.CloudStorageAccount(new Microsoft.WindowsAzure.StorageCredentialsAccountAndKey(AzureAccountName, AzureAccountKey), true);
                blobClient = storageAccount.CreateCloudBlobClient();
                container = blobClient.GetContainerReference(AzureConfigContainerName);

                if (configFilename == PackagerHelper.ConfigPackagerHelper.actualConfigFileName)
                {
                    blockBlob = container.GetBlockBlobReference(ActualConfigBlobName(orgID, studyID, homeID, configFilename));
                }
                else if (configFilename == PackagerHelper.ConfigPackagerHelper.desiredConfigFileName)
                {
                    blockBlob = container.GetBlockBlobReference(DesiredConfigBlobName(orgID, studyID, homeID, configFilename));
                }

                bool blobExists = BlockBlobExists(blockBlob);

                if (blobExists)
                    leaseId = AcquireLease(blockBlob, logger); // Acquire Lease on Blob
                else
                    return false;

                if (blobExists && leaseId == null)
                {
                    if (null != logger)
                    {
                        logger.Error("AcquireLease on Blob: " + ActualConfigBlobName(orgID, studyID, homeID, configFilename) + " Failed");
                    }
                    return false;
                }

                string url = blockBlob.Uri.ToString();
                if (blockBlob.ServiceClient.Credentials.NeedsTransformUri)
                {
                    url = blockBlob.ServiceClient.Credentials.TransformUri(url);
                }

                var req = BlobRequest.Get(new Uri(url), AzureBlobLeaseTimeout, null, leaseId);
                blockBlob.ServiceClient.Credentials.SignRequest(req);

                using (var reader = new BinaryReader(req.GetResponse().GetResponseStream()))
                {
                    FileStream zipFile = new FileStream(downloadedZipPath, FileMode.OpenOrCreate);
                    reader.BaseStream.CopyTo(zipFile);
                    zipFile.Close();
                }
                req.GetResponse().GetResponseStream().Close();

                ReleaseLease(blockBlob, leaseId); // Release Lease on Blob
                return true;
            }
            catch (Exception e)
            {
                if (null != logger)
                {
                    logger.ErrorException("DownloadConfig_Azure, downloadZipPath: " + downloadedZipPath, e);
                }
                ReleaseLease(blockBlob, leaseId);
                return false;
            }
        }

        private static string ActualConfigBlobName(string orgID, string studyID, string homeID, string actualConfigFilename)
        {
            return "/" + orgID + "/" + studyID + "/" + homeID + actualConfigFilePathInHubFolder + actualConfigFilename;
        }

        public static Tuple<bool, Exception> IsValidAccount(string account, string accountKey)
        {
            bool accountValid = true;
            Exception exception = null;
            try
            {
                Microsoft.WindowsAzure.CloudStorageAccount storageAccount = null;
                Microsoft.WindowsAzure.StorageClient.CloudBlobClient blobClient = null;
                Microsoft.WindowsAzure.StorageClient.CloudBlobContainer container = null;

                storageAccount = new Microsoft.WindowsAzure.CloudStorageAccount(new Microsoft.WindowsAzure.StorageCredentialsAccountAndKey(account, accountKey), true);
                blobClient = storageAccount.CreateCloudBlobClient();
                container = blobClient.GetContainerReference(AzureConfigContainerName);
                container.CreateIfNotExist();
            }
            catch(Exception e)
            {
                accountValid = false;
                exception = e;
            }

            return new Tuple<bool, Exception>(accountValid, exception);
        }

        public static Tuple<bool, List<string>> listStudies(string account, string accountKey, string orgId)
        {
            try
            {
                CloudStorageAccount storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(account, accountKey), true);
                Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer storageContainer = blobClient.GetContainerReference(AzureConfigContainerName);

                List<String> orgList = lsDirectory(storageContainer.ListBlobs(), "/" + orgId + "/");

                return new Tuple<bool, List<string>>(true, orgList);

            }
            catch (Exception e)
            {
                return new Tuple<bool, List<string>>(false, new List<string>() { e.Message });
            }

        }

        public static Tuple<bool, List<string>> listOrgs(string account, string accountKey)
        {

            try
            {
                CloudStorageAccount storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(account, accountKey), true);
                Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer storageContainer = blobClient.GetContainerReference(AzureConfigContainerName);

                List<String> orgList = lsDirectory(storageContainer.ListBlobs(), "/");

                return new Tuple<bool, List<string>>(true, orgList);

            }
            catch (Exception e)
            {
                return new Tuple<bool, List<string>>(false, new List<string>() { e.Message });
            }

        }

        public static Tuple<bool, List<string>> listHubs(string account, string accountKey, string orgID, string studyID)
        {

            try
            {
                CloudStorageAccount storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(account, accountKey), true);
                Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer storageContainer = blobClient.GetContainerReference(AzureConfigContainerName);

                List<String> hubList = lsDirectory(storageContainer.ListBlobs(), "/" + orgID + "/" + studyID + "/");

                return new Tuple<bool, List<string>>(true, hubList);

            }
            catch (Exception e)
            {
                return new Tuple<bool, List<string>>(false, new List<string>() { e.Message });
            }

        }

        private static List<string> lsDirectory(IEnumerable<Microsoft.WindowsAzure.Storage.Blob.IListBlobItem> enumerable, string dirPath)
        {
            List<string> ret = new List<string>();

            foreach (var item in enumerable.Where((blobItem, type) => blobItem is Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob))
            {
                var blobFile = item as Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob;
                string outputFileName = blobFile.Name;
                if (blobFile.Parent.Prefix.Equals(dirPath))
                    ret.Add(outputFileName);
            }

            foreach (var item in enumerable.Where((blobItem, type) => blobItem is Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory))
            {
                var directory = item as Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory;
                string directoryName = directory.Prefix;

                if (directoryName.Equals(dirPath))
                    ret = ret.Concat(lsDirectory(directory.ListBlobs(), dirPath)).ToList();

                else if (directory.Parent != null && directory.Parent.Prefix.Equals(dirPath))
                {
                    directoryName = directoryName.Replace(directory.Parent.Prefix, "");
                    directoryName = directoryName.Replace("/", "");
                    ret.Add(directoryName);
                }
                else
                    ret = ret.Concat(lsDirectory(directory.ListBlobs(), dirPath)).ToList();
            }
            return ret;
        }


        #region methods to acquire and relinquich leases on azure blobs; and check if a blob already exists
        private static string AcquireLease(Microsoft.WindowsAzure.StorageClient.CloudBlockBlob blob, NLog.Logger logger)
        {
            try
            {
                var creds = blob.ServiceClient.Credentials;
                var transformedUri = new Uri(creds.TransformUri(blob.Uri.ToString()));
                var req = BlobRequest.Lease(transformedUri, AzureBlobLeaseTimeout, // timeout (in seconds)
                    Microsoft.WindowsAzure.StorageClient.Protocol.LeaseAction.Acquire, // as opposed to "break" "release" or "renew"
                    null); // name of the existing lease, if any
                blob.ServiceClient.Credentials.SignRequest(req);
                using (var response = req.GetResponse())
                {
                    return response.Headers["x-ms-lease-id"];
                }
            }

            catch (WebException e)
            {
                if (null != logger)
                {
                    logger.ErrorException("AcquireLease, blob: " + blob, e);
                }
                return null;
            }
        }

        public static void ReleaseLease(CloudBlob blob, string leaseId, NLog.Logger logger = null)
        {
            DoLeaseOperation(blob, leaseId, Microsoft.WindowsAzure.StorageClient.Protocol.LeaseAction.Release, logger);
        }

        private static void DoLeaseOperation(CloudBlob blob, string leaseId, Microsoft.WindowsAzure.StorageClient.Protocol.LeaseAction action, NLog.Logger logger)
        {
            try
            {
                if (blob == null || leaseId == null)
                    return;
                var creds = blob.ServiceClient.Credentials;
                var transformedUri = new Uri(creds.TransformUri(blob.Uri.ToString()));
                var req = BlobRequest.Lease(transformedUri, AzureBlobLeaseTimeout, action, leaseId);
                creds.SignRequest(req);
                req.GetResponse().Close();
            }
            catch (WebException e)
            {
                if (null != logger)
                {
                    logger.ErrorException("DoLeaseOperation, blob: " + blob.Name + ", leaseId: " + leaseId + ", action " + action, e);
                }
            }
        }
        private static bool BlockBlobExists(Microsoft.WindowsAzure.StorageClient.CloudBlockBlob blob)
        {
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (Microsoft.WindowsAzure.StorageClient.StorageClientException e)
            {
                if (e.ErrorCode == Microsoft.WindowsAzure.StorageClient.StorageErrorCode.ResourceNotFound)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        #endregion     
    }
}
