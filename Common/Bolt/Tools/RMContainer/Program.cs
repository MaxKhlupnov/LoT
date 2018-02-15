using Microsoft.WindowsAzure;

using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMContainer
{
    class Program
    {
        static void Main(string[] args)
        {
            CleanAccount( ConfigurationManager.AppSettings.Get("AccountName"),  ConfigurationManager.AppSettings.Get("AccountKey"));
        }

        public static void CleanAccount(String accountName, String accountKey)
        {
            CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentialsAccountAndKey(accountName, accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            foreach (CloudBlobContainer blobcontainer in blobClient.ListContainers())
            {
                if (blobcontainer.Name.Contains(ConfigurationManager.AppSettings.Get("Prefix")))
                {
                    blobcontainer.Delete();
                    Console.WriteLine("Deleting container: "+blobcontainer.Name);
                }
            }

        }
    }
}
