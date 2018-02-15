using System;
using System.IO;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public interface ISync
    {

        void SetDataFileName(string dataFileName);
        byte[] ReadData(long offset, long size);

        void SetIndexFileName(string indexFileName);
        void SetLocalSource(string FqDirName);

        /* Sync Data */
        bool Sync();

        bool Delete();

        void Dispose();

        byte[] GetChunkListHash();

        bool DownloadFile(string blobName, string filePath);
    }
}
