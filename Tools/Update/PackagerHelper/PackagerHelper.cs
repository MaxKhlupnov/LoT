using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Tools.PackagerHelper
{
    public class PackagerHelper
    {
        #region MD5 hashing
        public static string GetMD5HashOfFile(string filePath)
        {
            try
            {
                FileStream file = new FileStream(filePath, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("E", e.Message + ". GetMD5HashOfFile(), file" + filePath);
                return "";
            }
        }
        #endregion

        #region pack, unpack zips
        public static bool UnpackZip(String zipPath, string extractPath)
        {
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
                return true;
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + ". UnpackZip, zipPath: " + zipPath + ", extractPath:" + extractPath);
                return false;
            }

        }

        public static bool PackZip(string startPath, String zipPath)
        {
            try
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
                System.IO.Compression.ZipFile.CreateFromDirectory(startPath, zipPath);
                return true;
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + ". PackZip, startPath: " + startPath + ", zipPath:" + zipPath);
                return false;
            }

        }
        #endregion

        #region file and directory handlers
        public static List<string> ListFiles(string directory)
        {
            List<string> retVal = new List<string>();
            try
            {
                foreach (string f in Directory.GetFiles(directory))
                    retVal.Add(Path.GetFileName(f));
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + ". ListFiles, directory: " + directory);
            }
            return retVal;
        }

        public static List<string> ListDirectories(string directory)
        {
            List<string> retVal = new List<string>();
            try
            {
                foreach (string f in Directory.GetDirectories(directory))
                    retVal.Add(Path.GetFileName(f));
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + ". ListFiles, directory: " + directory);
            }
            return retVal;
        }

        public static string ReadFile(string filePath)
        {
            try
            {

                System.IO.StreamReader myFile = new System.IO.StreamReader(filePath);
                string myString = myFile.ReadToEnd();
                myFile.Close();
                return myString;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in reading file " + filePath + "! " + e.Message);
                return "";
            }
        }

        public static void DeleteFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + ". DeleteFile, filePath:" + filePath);
            }
        }

        public static void CopyFile(string file1, string file2)
        {
            try
            {
                if (File.Exists(file2))
                    File.Delete(file2);

                File.Copy(file1, file2);
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + ". CopyFile, filePath1: {0} filePath2: {1}", file1, file2);
            }
        }

        public static void MoveFile(string file1, string file2)
        {
            try
            {
                if (File.Exists(file2))
                    File.Delete(file2);

                File.Move(file1, file2);
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + ". MoveFile, filePath1: {0} filePath2: {1}", file1, file2);
            }
        }


        public static string CreateDirectory(String completePath)
        {
            try
            {
                if (Directory.Exists(completePath))
                {
                    CleanDirectory(completePath);
                    Directory.Delete(completePath, true);
                }

                Directory.CreateDirectory(completePath);
                DirectoryInfo info = new DirectoryInfo(completePath);
                System.Security.AccessControl.DirectorySecurity security = info.GetAccessControl();
                security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(Environment.UserName, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.InheritanceFlags.ContainerInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
                security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(Environment.UserName, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.InheritanceFlags.ContainerInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
                info.SetAccessControl(security);
                return completePath;
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + ". CreateDirectory, completePath:" + completePath);
            }

            return null;
        }

        public static void CleanDirectory(string directory)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(directory);
                foreach (FileInfo fi in dir.GetFiles())
                {
                    fi.Delete();
                }

                foreach (DirectoryInfo di in dir.GetDirectories())
                {
                    CleanDirectory(di.FullName);
                    di.Delete();
                }
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + ". CleanDirectory, directory:" + directory);
            }
        }

        public static void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);

                int numTries = 3;
                while (numTries > 0)
                {
                    try
                    {
                        numTries--;

                        File.Copy(file, dest, true);

                        break;
                    }
                    catch (Exception e)
                    {
                        if (numTries > 0)
                            System.Threading.Thread.Sleep(5 * 1000);
                        else
                            Console.WriteLine("Failed to copy " + file + "\n" + e.ToString(), true);
                    }
                }
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                try
                {
                    CopyFolder(folder, dest);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.ToString(), true);
                }
            }
        }

        public static void CreateFolder(String folder, bool deleteIfExists = true)
        {
            if (Directory.Exists(folder) && deleteIfExists)
                Directory.Delete(folder, true);

            Directory.CreateDirectory(folder);
        }

        public static bool ExtractZipToFolder(string zipFile, string tmpBinFolder)
        {
            CreateFolder(tmpBinFolder);
            System.IO.Compression.ZipFile.ExtractToDirectory(zipFile, tmpBinFolder);
            return true;
        }

        public static void DeleteFolder(string folder, bool recursive = false)
        {
            Exception exception = null;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Directory.Delete(folder, recursive);

                    return;
                }
                catch (Exception e)
                {
                    exception = e;
                }

                //lets wait 5 seconds and then we try to delete again
                System.Threading.Thread.Sleep(5 * 1000);
            }

            throw exception;
        }


        #endregion
    }
}
