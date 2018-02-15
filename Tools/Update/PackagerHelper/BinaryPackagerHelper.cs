using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using System.Xml.Linq;
using HomeOS.Hub.Common;
using NLog;

namespace HomeOS.Hub.Tools.PackagerHelper
{
    /// <summary>
    /// Shared Helper code used by the different packagers: ConfigPackager, ModulePackager, PlatformPackager and
    /// ScoutPackager, and the UpdateManager
    /// </summary>
    public class BinaryPackagerHelper
    {
        public const string UnknownHomeOSUpdateVersionValue = Constants.UnknownHomeOSUpdateVersionValue;
        public const string ConfigAppSettingKeyHomeOSUpdateVersion = Constants.ConfigAppSettingKeyHomeOSUpdateVersion;

        #region spew
        private static void DisplayError(string error, NLog.Logger logger = null)
        {
            if (null != logger)
            {
                logger.Error(error);
            }
            else
            {
                Console.Error.WriteLine(error);
            }
        }
        private static void DisplayInfo(string info, NLog.Logger logger = null)
        {
            if (null != logger)
            {
                logger.Info(info);
            }
            else
            {
                Console.WriteLine(info);
            }
        }
        #endregion 


        #region main package function
        public static bool Package(string binRootDir, string binName, bool singleBin, string binType, string packType, string repoDir, ref string[] filePaths, NLog.Logger logger = null)
        {
            bool success = true;
            //get the binary directory
            string binDir = singleBin ? binRootDir : binRootDir + "\\" + binName;

            if (!Directory.Exists(binDir))
            {
                string error = string.Format("{0} directory {1} does not exist. Is there a mismatch in {0} name and its location?", packType, binDir);
                DisplayError(error, logger);
                success = false;
                goto Exit;
            }

            //get the zip dir
            string zipDir = repoDir;

            string[] parts = binName.Split('.');

            foreach (var part in parts)
                zipDir += "\\" + part;

            // Use HomeOSUpdateVersion from App.Config

            string file = binDir + "\\" + binName + "." + binType + ".config";
            string homeosUpdateVersion = Utils.GetHomeOSUpdateVersion(file);
            if (homeosUpdateVersion == UnknownHomeOSUpdateVersionValue)
            {
                string error = string.Format("Warning didn't find {0} version in {1}, defaulting to {2}", packType, file, homeosUpdateVersion);
                DisplayError(error, logger);
            }

            zipDir += "\\" + homeosUpdateVersion;
            Directory.CreateDirectory(zipDir);

            //get the name of the zip file and pack it
            string zipFile = zipDir + "\\" + binName + ".zip";
            string hashFile = zipDir + "\\" + binName + ".md5";

            bool result = PackagerHelper.PackZip(binDir, zipFile);

            if (!result)
            {
                string error = string.Format("Failed to pack zip for {0}. Quitting", binName);
                DisplayError(error);
                success = false;
                goto Exit;
            }

            string md5hash = PackagerHelper.GetMD5HashOfFile(zipFile);

            if (string.IsNullOrWhiteSpace(md5hash))
            {
                string error = string.Format("Failed to generated MD5 hash for file {0}. Quitting", zipFile);
                DisplayError(error);
                success = false;
                goto Exit;
            }

            try
            {
                File.WriteAllText(hashFile, md5hash);
            }
            catch (Exception)
            {
                string error = string.Format("Failed to write hash file {0}. Quitting", hashFile);
                DisplayError(error);
                success = false;
                goto Exit;
            }

            Array.Resize(ref filePaths, 2);
            filePaths[0] = zipFile;
            filePaths[1] = hashFile;
            string info = string.Format("Prepared {0} package: {1}.\n", packType, zipFile);
            DisplayInfo(info, logger);

        Exit:
            return success;
        }
        #endregion
    }
}
