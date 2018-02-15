using HomeOS.Hub.Platform.Views;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common
{
    public class AzureUtils
    {
        #region methods to acquire and relinquich leases on azure blobs; and check if a blob already exists
        public static string AcquireLease(VLogger logger, CloudBlockBlob blob, int AzureBlobLeaseTimeout)
        {
            try
            {
                var creds = blob.ServiceClient.Credentials;
                var transformedUri = new Uri(creds.TransformUri(blob.Uri.ToString()));
                var req = BlobRequest.Lease(transformedUri, AzureBlobLeaseTimeout, // timeout (in seconds)
                    LeaseAction.Acquire, // as opposed to "break" "release" or "renew"
                    null); // name of the existing lease, if any
                blob.ServiceClient.Credentials.SignRequest(req);
                using (var response = req.GetResponse())
                {
                    return response.Headers["x-ms-lease-id"];
                }
            }

            catch (WebException e)
            {
                Utils.structuredLog(logger, "WebException", e.Message + ". AcquireLease, blob: " + blob);
                return null;
            }
        }

        public static void ReleaseLease(VLogger logger, CloudBlob blob, string leaseId, int AzureBlobLeaseTimeout)
        {
            DoLeaseOperation(logger, blob, leaseId, LeaseAction.Release, AzureBlobLeaseTimeout);
        }

        public static void DoLeaseOperation(VLogger logger, CloudBlob blob, string leaseId, LeaseAction action, int AzureBlobLeaseTimeout)
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
                Utils.structuredLog(logger, "WebException", e.Message + ". DoLeaseOperation, blob: " + blob.Name + ", leaseId: " + leaseId + ", action " + action);
            }
        }
        #endregion

        /// <summary>
        /// Method to check if a blockblob exists already
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="blob"></param>
        /// <returns></returns>
        public static bool BlockBlobExists(VLogger logger, CloudBlockBlob blob)
        {
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    Utils.structuredLog(logger, "E", "BlockBlob: " + blob.Name + " does not exist.");
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }
        

        
        
    }
}
