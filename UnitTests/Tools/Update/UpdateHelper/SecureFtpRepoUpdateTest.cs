using System;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeOS.Hub.Tools.UpdateHelper;

namespace HomeOS.Hub.UnitTests.Tools.Update.UpdateHelper
{
    [TestClass]
    public class SecureFtpRepoUpdateTest
    {
        const string dataFilePath = @"..\..\..\UnitTests\Tools\Update\Data\repo.zip";
        const string dataDownloadFilePath = @"..\..\..\UnitTests\Tools\Update\Data\repo.download.zip";
        const string remoteHostname = @"repository.lab-of-things.net";
        const int remoteHostPort = 2500;
        const string remoteUsername = @"vm_admin";
        const string remoteUserPassword = @"HomeLab2013";

        static Uri uriFile = new Uri("ftp://" + remoteHostname + ":" + remoteHostPort + "/UnitTestDirectory/" + Path.GetFileName(dataFilePath));
        static Uri uriDir = new Uri("ftp://" + remoteHostname + ":" + remoteHostPort + "/UnitTestDirectory");

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            Cleanup();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            try
            {
                SecureFtpRepoUpdate.DeleteFile(uriFile, remoteUsername, remoteUserPassword, true /*enableSSL*/);
            }
            catch (Exception)
            {
                // it's ok for this calls to fail, if the file is not present, ignoring...
            }
            try
            {
                SecureFtpRepoUpdate.RemoveDirectory(uriDir, remoteUsername, remoteUserPassword, true /*enableSSL*/);
            }
            catch (Exception)
            {
                // it's ok for this calls to fail, if the directory is not present, ignoring...
            }

            string fullpath = Path.GetFullPath(dataDownloadFilePath);
            if (File.Exists(fullpath))
            {
                File.Delete(fullpath);
            }

        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_UploadFile_UsingFtp()
        {
            SecureFtpRepoUpdate.UploadFile(uriFile, Path.GetFullPath(dataFilePath), remoteUsername, remoteUserPassword, true /*enableSSL*/);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_UploadFile_UsingFtps()
        {
            SecureFtpRepoUpdate.UploadFile(uriFile, Path.GetFullPath(dataFilePath), remoteUsername, remoteUserPassword, true /*enableSSL*/);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_DownloadFile_UsingFtp()
        {
            string downloadfileFullPath = Path.GetFullPath(dataDownloadFilePath);
            SecureFtpRepoUpdate.DownloadFile(uriFile, downloadfileFullPath, remoteUsername, remoteUserPassword, true /*enableSSL*/);

            Assert.IsTrue(File.Exists(downloadfileFullPath));
            Assert.IsTrue((new FileInfo(downloadfileFullPath).Length) == (new FileInfo(dataFilePath).Length));
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_DownloadFile_UsingFtps()
        {
            string downloadfileFullPath = Path.GetFullPath(dataDownloadFilePath);
            SecureFtpRepoUpdate.DownloadFile(uriFile, downloadfileFullPath, remoteUsername, remoteUserPassword, true /*enableSSL*/);

            Assert.IsTrue(File.Exists(downloadfileFullPath));
            Assert.IsTrue((new FileInfo(downloadfileFullPath).Length) == (new FileInfo(dataFilePath).Length));
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_DeleteFile_UsingFtp()
        {
            SecureFtpRepoUpdate.DeleteFile(uriFile, remoteUsername, remoteUserPassword, true /*enableSSL*/);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_DeleteFile_UsingFtps()
        {
            SecureFtpRepoUpdate.DeleteFile(uriFile, remoteUsername, remoteUserPassword, true /*enableSSL*/);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_ListDirectory_UsingFtp()
        {
            string[] files = SecureFtpRepoUpdate.ListDirectory(uriDir, remoteUsername, remoteUserPassword, true /*details*/, true /*enableSSL*/);
            Assert.IsTrue(files.GetLength(0) > 0);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_ListDirectory_UsingFtps()
        {
            string[] files = SecureFtpRepoUpdate.ListDirectory(uriDir, remoteUsername, remoteUserPassword, true /*details*/, true /*enableSSL*/);
            Assert.IsTrue(files.GetLength(0) > 0);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_ListDirectoryDetails_UsingFtp()
        {
            string[] files = SecureFtpRepoUpdate.ListDirectory(uriDir, remoteUsername, remoteUserPassword, true /*details*/, true /*enableSSL*/);
            Assert.IsTrue(files.GetLength(0) > 0);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_ListDirectoryDetails_UsingFtps()
        {
            string[] files = SecureFtpRepoUpdate.ListDirectory(uriDir, remoteUsername, remoteUserPassword, true /*details*/, true /*enableSSL*/);
            Assert.IsTrue(files.GetLength(0) > 0);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_GetFileSize_UsingFtp()
        {
            long fileSize = SecureFtpRepoUpdate.GetFileSize(uriFile, remoteUsername, remoteUserPassword, true /*enableSSL*/);
            Assert.IsTrue(fileSize > 0);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_GetFileSize_UsingFtps()
        {
            long fileSize = SecureFtpRepoUpdate.GetFileSize(uriFile, remoteUsername, remoteUserPassword, true /*enableSSL*/);
            Assert.IsTrue(fileSize > 0);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_GetFileDateTimeStamp_UsingFtp()
        {
            string fileDateTimeStamp = SecureFtpRepoUpdate.GetFileDateTimeStamp(uriFile, remoteUsername, remoteUserPassword, true /*enableSSL*/);
            Assert.IsTrue(fileDateTimeStamp.Length > 0);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_GetFileDateTimeStamp_UsingFtps()
        {
            string fileDateTimeStamp = SecureFtpRepoUpdate.GetFileDateTimeStamp(uriFile, remoteUsername, remoteUserPassword, true /*enableSSL*/);
            Assert.IsTrue(fileDateTimeStamp.Length > 0);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_MakeDirectory_UsingFtp()
        {
            SecureFtpRepoUpdate.MakeDirectory(uriDir, remoteUsername, remoteUserPassword, true /*enableSSL*/);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_MakeDirectory_UsingFtps()
        {
            SecureFtpRepoUpdate.MakeDirectory(uriDir, remoteUsername, remoteUserPassword, true /*enableSSL*/);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_RemoveDirectory_UsingFtp()
        {
            SecureFtpRepoUpdate.RemoveDirectory(uriDir, remoteUsername, remoteUserPassword, true /*enableSSL*/);
        }

        [TestMethod]
        public void SecureFtpRepoUpdateTest_RemoveDirectory_UsingFtps()
        {
            SecureFtpRepoUpdate.RemoveDirectory(uriDir, remoteUsername, remoteUserPassword, true /*enableSSL*/);
        }

    }
}
