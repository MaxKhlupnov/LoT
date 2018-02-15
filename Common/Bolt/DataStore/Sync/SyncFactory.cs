using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public enum SynchronizerType : byte { None = 0, Azure, AmazonS3 }
    public enum SynchronizeDirection : byte { Upload = 0, Download }

    public enum CompressionType : byte { None = 0 , BZip2, GZip}
    public enum EncryptionType : byte { None = 0, AES }

   
    public sealed class SyncFactory
    {
        public static readonly string[] FilesExcludedFromSync = { ".kr" , ".pubkey", ".key" }; 

        private static volatile SyncFactory instance;
        private static object syncRoot = new Object();

        private SyncFactory() { }

        public static SyncFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new SyncFactory();
                    }
                }

                return instance;
            }
        }

        public ISync CreateSynchronizer(LocationInfo Li, string container, Logger log, SynchronizeDirection syncDirection = SynchronizeDirection.Upload, CompressionType compressionType = CompressionType.None, int ChunkSizeForUpload = 4*1024*1024, int ThreadPoolSize =1 ,   EncryptionType encryptionType = EncryptionType.None , byte[] encryptionKey = null, byte[] initializationVector =null)
        {
            ISync isync = null;
            switch (Li.st)
            {
                case SynchronizerType.Azure:
                    isync = CreateAzureSynchronizer(new RemoteInfo(Li.accountName, Li.accountKey), container, log, syncDirection, compressionType, ChunkSizeForUpload, ThreadPoolSize, encryptionType, encryptionKey, initializationVector);
                    break;
                case SynchronizerType.AmazonS3:
                    isync = CreateAmazonS3Synchronizer(new RemoteInfo(Li.accountName, Li.accountKey), container, log, syncDirection, compressionType, ChunkSizeForUpload, ThreadPoolSize, encryptionType, encryptionKey, initializationVector);
                    break;
                default:
                    isync = null;
                    break;
            }
            return isync;
        }
        
        private ISync CreateAzureSynchronizer(RemoteInfo ri, string container, Logger log, SynchronizeDirection syncDirection, CompressionType compressionType,int ChunkSizeForUpload, int ThreadPoolSize, EncryptionType encryptionType, byte[] encryptionKey, byte[] initializationVector)
        {

            return new AzureChunkSynchronizer(ri, container, syncDirection, compressionType, encryptionType, encryptionKey, initializationVector, log, ChunkSizeForUpload, ThreadPoolSize);
        }

        private ISync CreateAmazonS3Synchronizer(RemoteInfo ri, string container, Logger log, SynchronizeDirection syncDirection, CompressionType compressionType, int ChunkSizeForUpload, int ThreadPoolSize, EncryptionType encryptionType, byte[] encryptionKey, byte[] initializationVector)
        {

            return new AmazonS3Synchronizer(ri, container, syncDirection, compressionType, encryptionType, encryptionKey, initializationVector, log, ChunkSizeForUpload, ThreadPoolSize);
        }

        public static SynchronizerType GetSynchronizerType(string location)
        {
            if (location == "None")
                return SynchronizerType.None;
            else if (location == "Azure")
                return SynchronizerType.Azure;
            else
                return SynchronizerType.AmazonS3;
        }

        public static CompressionType GetCompressionType(string compressionType)
        {
            switch (compressionType)
            {
                case "none":
                    return CompressionType.None;
                case "gzip":
                    return CompressionType.GZip;
                case "bzip2":
                    return CompressionType.BZip2;
                default:
                    throw new Exception("unknown compression type: " + compressionType);
            }

        }

        public static  string GetCompressionTypeAsString(CompressionType compressionType)
        {
            switch (compressionType)
            {
                case CompressionType.None:
                    return "none";
                case CompressionType.GZip:
                    return "gzip";
                case CompressionType.BZip2:
                    return "bzip2";
                default:
                    throw new Exception("unknown compression type: " + compressionType);
            }
        }

        public static EncryptionType GetEncryptionType(string encryptionType)
        {
            switch (encryptionType)
            {
                case "none":
                    return EncryptionType.None;
                case "aes":
                    return EncryptionType.AES;
                default:
                    throw new Exception("unknown encryption type: " + encryptionType);
            }

        }

        public static string GetEncryptionTypeAsString(EncryptionType encryptionType)
        {
            switch (encryptionType)
            {
                case EncryptionType.None:
                    return "none";
                case EncryptionType.AES:
                    return "aes";
                default:
                    throw new Exception("unknown encryption type: " + encryptionType);
            }
        }


        ////////////////////////////////////////////////////////////////////////////////
        // Legacy Comaptibility Calls
        //  - depricated (uses sync framework)
        //  - do not use unless you know what you are doing
        ////////////////////////////////////////////////////////////////////////////////
        public ISync CreateLogSynchronizer(LocationInfo Li, string container)
        {
            ISync isync = null;
            switch (Li.st)
            {
                case SynchronizerType.Azure:
                    isync = new HDS.AzureSynchronizer(new RemoteInfo(Li.accountName, Li.accountKey), container, SynchronizeDirection.Upload);
                    break;
                default:
                    isync = null;
                    break;
            }
            return isync;
        }

    }
}
