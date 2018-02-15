using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Tools.PackagerHelper
{
    /// <summary>
    /// Shared Helper code used by the different packagers: ConfigPackager, ModulePackager, PlatformPackager and
    /// ScoutPackager, and the UpdateManager
    /// </summary>
    public class ConfigPackagerHelper
    {
        #region constants
        // version file names
        public const string CurrentVersionFileName = ".currentversion";
        public const string ParentVersionFileName = ".parentversion";
        public const string VersionDefinitionFileName = ".versiondef";

        // file names for actual and desired config zip files
        public const string actualConfigFileName = "actualconfig.zip";
        public const string desiredConfigFileName = "desiredconfig.zip";

        #endregion

        #region method for computing version, reading and writing them
        public static Dictionary<string, string> GetConfigVersion(string configDir)
        {
            Dictionary<string, string> retVal = new Dictionary<string, string>();
            List<string> configFilesToHash = GetFileNamesInVersionDef(configDir);

            foreach (string name in configFilesToHash)
            {
                if (!name.Equals(ConfigPackagerHelper.CurrentVersionFileName, StringComparison.CurrentCultureIgnoreCase)
                    && !name.Equals(ConfigPackagerHelper.ParentVersionFileName, StringComparison.CurrentCultureIgnoreCase)
                    && !name.Equals(ConfigPackagerHelper.VersionDefinitionFileName, StringComparison.CurrentCultureIgnoreCase))
                    retVal.Add(name, PackagerHelper.GetMD5HashOfFile(configDir + "\\" + name));
            }

            return retVal;
            /*
            Dictionary<string, string> retVal = new Dictionary<string, string>();
           
                List<string> configFileNames = ListFiles(configDir);
                configFileNames.Sort();
                foreach (string name in configFileNames)
                {
                    if (!name.Equals(currentVersionFileName, StringComparison.CurrentCultureIgnoreCase) && !name.Equals(parentVersionFileName, StringComparison.CurrentCultureIgnoreCase))
                        retVal.Add(name, GetMD5HashOfFile(configDir + "\\" + name));
                }
                return retVal;*/

        }

        private static List<string> GetFileNamesInVersionDef(string configDir)
        {
            List<string> filesInVersion = Constants.DefaultConfigVersionDefinition.ToList();
            List<string> filesInConfigDir = PackagerHelper.ListFiles(configDir);
            List<string> configFilesToHash = filesInVersion.ToList();

            try
            {
                filesInVersion = GetVersionDef(configDir);
                filesInConfigDir.Sort();
                configFilesToHash = filesInConfigDir.Intersect(filesInVersion.ToList()).ToList();
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + " .GetConfigVersion");
            }

            configFilesToHash.Sort();
            return configFilesToHash;
        }

        private static List<string> GetVersionDef(string configDir)
        {
            List<string> retVal = new List<string>();
            try
            {
                string versionDefinition = PackagerHelper.ReadFile(configDir + "\\" + ConfigPackagerHelper.VersionDefinitionFileName);
                if (!string.IsNullOrEmpty(versionDefinition))
                {
                    retVal = versionDefinition.Split(';').ToList();
                    retVal.Sort();
                }

            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + " .GetVersionDef " + configDir);
            }

            return retVal;
        }

        public static void UpdateVersionFile(Dictionary<string, string> version, string versionFilePath)
        {
            try
            {
                FileStream versionFile = new FileStream(versionFilePath, FileMode.OpenOrCreate);
                foreach (string fileName in version.Keys)
                {
                    string nameHashPair = fileName + "," + version[fileName] + ";";
                    versionFile.Write(System.Text.Encoding.ASCII.GetBytes(nameHashPair), 0, System.Text.Encoding.ASCII.GetByteCount(nameHashPair));
                }
                versionFile.Close();
            }
            catch (Exception e)
            {
                Utils.configLog("E", e.Message + ". UpdateVersionFile, version: " + version.ToString());
            }
        }


        #endregion 

    }
}
