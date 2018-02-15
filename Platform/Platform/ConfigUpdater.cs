
namespace HomeOS.Hub.Platform
{
    using HomeOS.Hub.Common;
    using HomeOS.Hub.Platform.Views;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using Microsoft.WindowsAzure.StorageClient.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.ServiceModel;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// ConfigUpdater periodically uploads current config to an Azure Blob as a Zip
    /// something happens here
    /// Platform is notified to re-initiate from the downloaded configuration.
    /// </summary>
    public sealed class ConfigUpdater
    {

        // Azure constants
        private const string DataStoreAccountName = "DataStoreAccountName";
        private const string DataStoreAccountKey = "DataStoreAccountKey";
        private const string AzureConfigContainerName = "configs";

        private const int AzureBlobLeaseTimeout = 60; //seconds. Max amount of time needed for uploading data.



        private int frequency;            // in milliseconds
        private Delegate methodToInvoke; // method in platform that is to be invoked if hash matches
        private Platform platform;       // a back pointer to the platform object
        private TimerCallback tcb;
        private Timer timer;
        private static string temporaryZipLocation = Environment.CurrentDirectory + "\\temp"; // temporary location where zip is stored
        private VLogger logger;


        private const string OldConfigSuffix = "_old";
        private const string NewConfigSuffix = "_new";

        private const string CurrentVersionFileName = ".currentversion";
        private const string ParentVersionFileName = ".parentversion";
        private const string CurrentConfigZipName = "actualconfig.zip";
        private const string DownloadedConfigZipName = "desiredconfig.zip";
        private const string VersionDefinitionFileName = ".versiondef";


        private ServiceHost serviceHost;
        private UpdateStatus status; // for reporting to the web service

        private Configuration config;

        public ConfigUpdater(Configuration config, VLogger log, int frequency, Delegate method, Platform platform)
        {
            this.config = config;
            this.logger = log;
            this.frequency = frequency;
            this.methodToInvoke = method;
            this.platform = platform;

            tcb = ConfigSync;
            timer = new Timer(tcb, null, 500, frequency);

            if (System.IO.Directory.Exists(temporaryZipLocation)) // creating temporary directory location for downloading and holding zips
                Utils.CleanDirectory(logger, temporaryZipLocation);
            Utils.CreateDirectory(logger, temporaryZipLocation);

            this.status = new UpdateStatus(this.frequency);

            ConfigUpdaterWebService webService = new ConfigUpdaterWebService(logger, this);

            string homeIdPart = "";
            if (!string.IsNullOrWhiteSpace(Settings.HomeId))
                homeIdPart = "/" + Settings.HomeId;

            string url = Constants.InfoServiceAddress + homeIdPart + "/config";
            serviceHost = ConfigUpdaterWebService.CreateServiceHost(webService, new Uri(url));
            serviceHost.Open();
            Utils.structuredLog(logger, "I", "ConfigUpdaterWebService initiated at " + url);

        }

        public void setConfig(Configuration config)
        {
            this.config = config;
        }

        private void ConfigSync(Object stateInfo)
        {
            Tuple<string, string> configToUpload;

            if (config == null)
            {
                Utils.structuredLog(logger, "W", "config is null; aborting ConfigSync");
                return;
            }

            status.lastConfigSync = DateTime.Now;
            Utils.structuredLog(logger, "I", "initiating ConfigSync");

            string AzureAccountName = config.GetConfSetting(DataStoreAccountName);
            string AzureAccountKey = config.GetConfSetting(DataStoreAccountKey);

            if (string.IsNullOrEmpty(AzureAccountKey) || string.IsNullOrEmpty(AzureAccountName))
            {
                Utils.structuredLog(logger, "E", "AzureAccountKey or AzureAccountName is null; aborting ConfigSync");
                return;
            }

            configToUpload = PrepareCurrentConfig();

            if (!string.IsNullOrEmpty(configToUpload.Item1))
            {
                if (UploadConfig_Azure(configToUpload.Item1, AzureAccountName, AzureAccountKey))
                {
                    status.lastConfigUpload = DateTime.Now;
                    status.versionUploaded = configToUpload.Item2;
                    Utils.structuredLog(logger, "ConfigUpload", ActualConfigBlobName(), configToUpload.Item2);
                }
                else
                    Utils.structuredLog(logger, "E", "config upload failed", ActualConfigBlobName(), configToUpload.Item2);
            }
            Utils.DeleteFile(logger, configToUpload.Item1);

            string downloadedConfigZipPath = temporaryZipLocation + "\\" + DownloadedConfigZipName;

            bool configReloadedThisRound = false;

            if (DownloadConfig_Azure(downloadedConfigZipPath, AzureAccountName, AzureAccountKey))
            {

                status.lastConfigDownload = DateTime.Now;
                Tuple<bool, string> configDeploy = ConfigReloadNeeded(downloadedConfigZipPath);
                if (configDeploy.Item1)
                {

                    string tempConfigDir = Settings.ConfigDir + "\\..\\Config" + NewConfigSuffix;
                    Utils.CleanDirectory(logger, tempConfigDir);

                    if (!Utils.UnpackZip(logger, downloadedConfigZipPath, tempConfigDir))
                        Utils.structuredLog(logger, "E", "unpacking failed", downloadedConfigZipPath);

                    Utils.CopyDirectory(logger, Settings.ConfigDir, Settings.ConfigDir + "\\..\\Config" + OldConfigSuffix);// stash away existing copy
                    Utils.CopyDirectory(logger, tempConfigDir, Settings.ConfigDir);
                    Utils.DeleteFile(logger, downloadedConfigZipPath);
                    Utils.DeleteDirectory(logger, tempConfigDir);
                    Utils.DeleteDirectory(logger, Settings.ConfigDir + "\\..\\Config" + OldConfigSuffix);
                    methodToInvoke.DynamicInvoke(Settings.ConfigDir);
                    Utils.structuredLog(logger, "I", "config reloading");

                    configReloadedThisRound = true;

                    // update status

                }
                else
                {
                    Utils.structuredLog(logger, "ER", "Config Reload Failed", configDeploy.Item2);
                    Utils.DeleteFile(logger, downloadedConfigZipPath);
                }
            }
            else
                Utils.structuredLog(logger, "ConfigDownload", "failed");


            //if config reload was not needed in this round, check that we have the latest version of all modules running
            //              .... this step is not needed otherwise, as config reload will ensure that we are running the latest version
            if (!configReloadedThisRound)
            {

                //1. get the list of running modules from platform
                //2. for each module check the running and repository version
                //3. if a module is found for which the repository version is newer, do reload

                var runningModules = platform.GetRunningModuleInfos();

                foreach (ModuleInfo moduleInfo in runningModules)
                {
                    //this process is needed only for modules that don't have a specific desired version number
                    if (moduleInfo.GetDesiredVersion() != Constants.UnknownHomeOSUpdateVersionValue)
                        continue;

                    Version versionRep = new Version(platform.GetVersionFromRep(Settings.RepositoryURIs, moduleInfo.BinaryName()));
                    Version versionLocal = new Version(Utils.GetHomeOSUpdateVersion((Utils.GetAddInConfigFilepath(moduleInfo.BinaryName())), logger));

                    if (versionRep.CompareTo(versionLocal) > 0)
                    {
                        Utils.structuredLog(logger, "I", String.Format("ConfigUpdater found a newer version on the repository ({0} > local version {1}) for {2}", versionRep.ToString(), versionLocal.ToString(), moduleInfo.BinaryName()));

                        methodToInvoke.DynamicInvoke(Settings.ConfigDir);
                        Utils.structuredLog(logger, "I", "config reloading");

                        configReloadedThisRound = true;

                        //config re-load will ensure that other moduels if newer will also be updated
                        break;
                    }
                }
            }

            // finally, do the same for scouts
            if (!configReloadedThisRound)
            {
                //1. get the list of running scouts from platform
                //2. for each scout check the running and repository version
                //3. if a module is found for which the repository version is newer, do reload

                var runningScouts = platform.GetRunningScoutInfos();

                foreach (DeviceScout.ScoutInfo scoutInfo in runningScouts)
                {
                    //this process is needed only for scouts that don't have a specific desired version number
                    if (scoutInfo.DesiredVersion != Constants.UnknownHomeOSUpdateVersionValue)
                        continue;

                    Version versionRep = new Version(platform.GetVersionFromRep(Settings.RepositoryURIs, scoutInfo.DllName));
                    Version versionLocal = new Version(Utils.GetHomeOSUpdateVersion((Utils.GetScoutConfigFilepath(scoutInfo.DllName)), logger));

                    if (versionRep.CompareTo(versionLocal) > 0)
                    {
                        Utils.structuredLog(logger, "I", String.Format("ConfigUpdater found a newer version on the repository ({0} > local version {1}) for {2}", versionRep.ToString(), versionLocal.ToString(), scoutInfo.DllName));

                        methodToInvoke.DynamicInvoke(Settings.ConfigDir);
                        Utils.structuredLog(logger, "I", "config reloading");

                        configReloadedThisRound = true;

                        //config re-load will ensure that other moduels if newer will also be updated
                        break;
                    }
                }
            }
        }

        private Tuple<string, string> PrepareCurrentConfig()
        {
            bool zipPacked = false;
            Dictionary<string, string> currentVersion;
            lock (config) // lock config so that no one else can change it. then, compute the version, zip the files, copy zip a location and relinquish lock.
            {
                currentVersion = GetConfigVersion(Settings.ConfigDir);
                UpdateCurrentVersionFile(currentVersion);
                string temporaryDirectoryToUpload = temporaryZipLocation + "\\" + CurrentConfigZipName.Replace(".zip", "");
                Utils.CreateDirectory(logger, temporaryDirectoryToUpload);

                foreach (string file in GetFileNamesInVersionDef(logger, Settings.ConfigDir))
                {
                    Utils.CopyFile(logger, Settings.ConfigDir + "\\" + file, temporaryDirectoryToUpload + "\\" + file);
                }

                if (File.Exists(Settings.ConfigDir + "\\" + CurrentVersionFileName))
                    Utils.CopyFile(logger, Settings.ConfigDir + "\\" + CurrentVersionFileName, temporaryDirectoryToUpload + "\\" + CurrentVersionFileName);

                if (File.Exists(Settings.ConfigDir + "\\" + ParentVersionFileName))
                    Utils.CopyFile(logger, Settings.ConfigDir + "\\" + ParentVersionFileName, temporaryDirectoryToUpload + "\\" + ParentVersionFileName);

                if (File.Exists(Settings.ConfigDir + "\\" + VersionDefinitionFileName))
                    Utils.CopyFile(logger, Settings.ConfigDir + "\\" + VersionDefinitionFileName, temporaryDirectoryToUpload + "\\" + VersionDefinitionFileName);

                zipPacked = Utils.PackZip(logger, temporaryDirectoryToUpload, temporaryZipLocation + "\\" + CurrentConfigZipName);
                Utils.DeleteDirectory(logger, temporaryDirectoryToUpload);
            }

            if (zipPacked)
                return new Tuple<string, string>(temporaryZipLocation + "\\" + CurrentConfigZipName, ConvertVersionToString(currentVersion));

            return new Tuple<string, string>("", "");
        }

        private Tuple<bool, string> ConfigReloadNeeded(string downloadedConfigZip)
        {
            String newconfigdir = Utils.CreateDirectory(logger, downloadedConfigZip.Replace(".zip", ""));

            if (string.IsNullOrEmpty(newconfigdir))
                return new Tuple<bool, string>(false, "directory creation for unzipping downloaded config failed");

            if (!Utils.UnpackZip(logger, downloadedConfigZip, newconfigdir))
                return new Tuple<bool, string>(false, "unpacking of downloaded config failed");

            string downloadedVersion = ConvertVersionToString(GetConfigVersion(newconfigdir));
            status.versionDownloaded = downloadedVersion;
            if (File.Exists(newconfigdir + "\\" + CurrentVersionFileName))
            {
                string versionFilecontent = File.ReadAllText(newconfigdir + "\\" + CurrentVersionFileName);
                if (!versionFilecontent.Equals(downloadedVersion, StringComparison.CurrentCultureIgnoreCase))
                    Utils.structuredLog(logger, "W", "version file of downloaded config has INCORRECT version");
            }

            if (File.Exists(newconfigdir + "\\" + ParentVersionFileName))
            {
                string parentVersionFilecontent = File.ReadAllText(newconfigdir + "\\" + ParentVersionFileName);
                string currentVersion;

                lock (config)
                {
                    currentVersion = ConvertVersionToString(GetConfigVersion(Settings.ConfigDir));
                }

                string currentVersionDef = string.Join(",", GetVersionDef(logger, Settings.ConfigDir));
                string newVersionDef = string.Join(",", GetVersionDef(logger, newconfigdir));

                if (currentVersion.Equals(ConvertVersionToString(GetConfigVersion(newconfigdir))) && currentVersionDef.Equals(newVersionDef, StringComparison.CurrentCultureIgnoreCase))
                {
                    Utils.DeleteDirectory(logger, newconfigdir);
                    return new Tuple<bool, string>(false, "version of current config and downloaded config match");
                }
                else if (currentVersion.Equals(ConvertVersionToString(GetConfigVersion(newconfigdir))))
                {
                    Utils.CopyFile(logger, newconfigdir + "\\" + VersionDefinitionFileName, Settings.ConfigDir + "\\" + VersionDefinitionFileName);
                    Utils.DeleteDirectory(logger, newconfigdir);
                    return new Tuple<bool, string>(false, "config reload not needed. updating config version definition");
                }

                if (currentVersion.Equals(parentVersionFilecontent, StringComparison.CurrentCultureIgnoreCase))
                {
                    Utils.DeleteDirectory(logger, newconfigdir);
                    return new Tuple<bool, string>(true, "");
                }
                else
                {
                    Utils.DeleteDirectory(logger, newconfigdir);
                    return new Tuple<bool, string>(false, "parent of downloaded config does not match current config");
                }

            }

            Utils.DeleteDirectory(logger, newconfigdir);
            return new Tuple<bool, string>(false, "downloaded config does not have parent config version file");
        }

        private bool UploadConfig_Azure(string configZipPath, string AzureAccountName, string AzureAccountKey)
        {
            CloudStorageAccount storageAccount = null;
            CloudBlobClient blobClient = null;
            CloudBlobContainer container = null;
            CloudBlockBlob blockBlob = null;
            string leaseId = null;

            try
            {
                storageAccount = new CloudStorageAccount(new StorageCredentialsAccountAndKey(AzureAccountName, AzureAccountKey), true);
                blobClient = storageAccount.CreateCloudBlobClient();
                container = blobClient.GetContainerReference(AzureConfigContainerName);
                container.CreateIfNotExist();
                blockBlob = container.GetBlockBlobReference(ActualConfigBlobName());

                bool blobExists = AzureUtils.BlockBlobExists(logger, blockBlob);

                if (blobExists)
                    leaseId = AzureUtils.AcquireLease(logger, blockBlob, AzureBlobLeaseTimeout); // Acquire Lease on Blob
                else
                    blockBlob.Container.CreateIfNotExist();

                if (blobExists && leaseId == null)
                {
                    Utils.structuredLog(logger, "ER", "AcquireLease on Blob: " + ActualConfigBlobName() + " Failed");
                    return false;
                }

                string url = blockBlob.Uri.ToString();
                if (blockBlob.ServiceClient.Credentials.NeedsTransformUri)
                {
                    url = blockBlob.ServiceClient.Credentials.TransformUri(url);
                }

                var req = BlobRequest.Put(new Uri(url), AzureBlobLeaseTimeout, new BlobProperties(), BlobType.BlockBlob, leaseId, 0);

                using (var writer = new BinaryWriter(req.GetRequestStream()))
                {
                    writer.Write(File.ReadAllBytes(configZipPath));
                    writer.Close();
                }

                blockBlob.ServiceClient.Credentials.SignRequest(req);
                req.GetResponse().Close();
                AzureUtils.ReleaseLease(logger, blockBlob, leaseId, AzureBlobLeaseTimeout); // Release Lease on Blob
                return true;
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger, "E", e.Message + ". UploadConfig_Azure, configZipPath: " + configZipPath + ". " + e);
                AzureUtils.ReleaseLease(logger, blockBlob, leaseId, AzureBlobLeaseTimeout);
                return false;
            }
        }

        private bool DownloadConfig_Azure(string downloadedZipPath, string AzureAccountName, string AzureAccountKey)
        {
            CloudStorageAccount storageAccount = null;
            CloudBlobClient blobClient = null;
            CloudBlobContainer container = null;
            CloudBlockBlob blockBlob = null;
            string leaseId = null;

            try
            {
                storageAccount = new CloudStorageAccount(new StorageCredentialsAccountAndKey(AzureAccountName, AzureAccountKey), true);
                blobClient = storageAccount.CreateCloudBlobClient();
                container = blobClient.GetContainerReference(AzureConfigContainerName);
                container.CreateIfNotExist();
                blockBlob = container.GetBlockBlobReference(DesiredConfigBlobName());

                bool blobExists = AzureUtils.BlockBlobExists(logger, blockBlob);

                if (blobExists)
                    leaseId = AzureUtils.AcquireLease(logger, blockBlob, AzureBlobLeaseTimeout); // Acquire Lease on Blob
                else
                    return false;

                if (blobExists && leaseId == null)
                {
                    Utils.structuredLog(logger, "ER", "AcquireLease on Blob: " + DesiredConfigBlobName() + " Failed");
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

                AzureUtils.ReleaseLease(logger, blockBlob, leaseId, AzureBlobLeaseTimeout); // Release Lease on Blob
                return true;
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger, "E", e.Message + ". DownloadConfig_Azure, downloadZipPath: " + downloadedZipPath + ". " + e);
                AzureUtils.ReleaseLease(logger, blockBlob, leaseId, AzureBlobLeaseTimeout);
                return false;
            }
        }

        private string ActualConfigBlobName()
        {
            return "/" + Settings.OrgId + "/" + Settings.StudyId + "/" + Settings.HomeId + "/config/actual/" + CurrentConfigZipName;
        }

        private string DesiredConfigBlobName()
        {
            return "/" + Settings.OrgId + "/" + Settings.StudyId + "/" + Settings.HomeId + "/config/desired/" + DownloadedConfigZipName;
        }


        #region Methods to compute version of config dirs

        private Dictionary<string, string> GetConfigVersion(string configDir)
        {
            Dictionary<string, string> retVal = new Dictionary<string, string>();
            List<string> configFilesToHash = GetFileNamesInVersionDef(logger, configDir);

            foreach (string name in configFilesToHash)
            {
                if (!name.Equals(CurrentVersionFileName, StringComparison.CurrentCultureIgnoreCase)
                && !name.Equals(ParentVersionFileName, StringComparison.CurrentCultureIgnoreCase)
                && !name.Equals(VersionDefinitionFileName, StringComparison.CurrentCultureIgnoreCase))
                    retVal.Add(name, Utils.GetMD5HashOfFile(logger, configDir + "\\" + name));
            }

            return retVal;
        }

        public static  List<string> GetFileNamesInVersionDef(VLogger logger, string configDir)
        {
            List<string> filesInVersion = Constants.DefaultConfigVersionDefinition.ToList();
            List<string> filesInConfigDir = Utils.ListFiles(logger, configDir);
            List<string> configFilesToHash = filesInVersion.ToList();

            try
            {
                filesInVersion = GetVersionDef(logger, configDir);
                filesInConfigDir.Sort();
                configFilesToHash = filesInConfigDir.Intersect(filesInVersion.ToList()).ToList();
            }
            catch (Exception e)
            {
                if (null != logger)
                {
                    Utils.structuredLog(logger, "E", e.Message + " .GetConfigVersion");
                }
            }

            configFilesToHash.Sort();
            return configFilesToHash;
        }

        public static List<string> GetVersionDef(VLogger logger, string configDir)
        {
            List<string> retVal = new List<string>();
            try
            {
                string versionDefinition = Utils.ReadFile(logger, configDir + "\\" + VersionDefinitionFileName);
                if (!string.IsNullOrEmpty(versionDefinition))
                {
                    retVal = versionDefinition.Split(';').ToList();
                    retVal.Sort();
                }

            }
            catch (Exception e)
            {
                if (null != logger)
                {
                    Utils.structuredLog(logger, "E", e.Message + " .GetVersionDef " + configDir);
                }
            }

            return retVal;
        }

        private string ConvertVersionToString(Dictionary<string, string> version)
        {
            StringBuilder ret = new StringBuilder();
            foreach (string fileName in version.Keys)
            {
                string nameHashPair = fileName + "," + version[fileName] + ";";
                ret.Append(nameHashPair);
            }
            return ret.ToString();
        }

        private void UpdateCurrentVersionFile(Dictionary<string, string> version)
        {
            try
            {
                Utils.DeleteFile(logger, Settings.ConfigDir + "\\" + CurrentVersionFileName);
                FileStream versionFile = new FileStream(Settings.ConfigDir + "\\" + CurrentVersionFileName, FileMode.OpenOrCreate);
                foreach (string fileName in version.Keys)
                {
                    string nameHashPair = fileName + "," + version[fileName] + ";";
                    versionFile.Write(System.Text.Encoding.ASCII.GetBytes(nameHashPair), 0, System.Text.Encoding.ASCII.GetByteCount(nameHashPair));
                }
                versionFile.Close();
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger, "E", e.Message + ". UpdateCurrentVersionFile, version: " + version.ToString());
            }
        }

        #endregion

        public void Reset(Configuration config, VLogger log, int freq, Delegate method)
        {
            lock (this)
            {
                this.config = config;
                this.logger = log;
                this.frequency = freq;
                this.timer.Change(this.frequency, this.frequency);
                this.methodToInvoke = method;

                this.serviceHost.Close();

                string homeIdPart = "";
                if (!string.IsNullOrWhiteSpace(Settings.HomeId))
                    homeIdPart = "/" + Settings.HomeId;
                ConfigUpdaterWebService webService = new ConfigUpdaterWebService(logger, this);
                string url = Constants.InfoServiceAddress + homeIdPart + "/config";
                serviceHost = ConfigUpdaterWebService.CreateServiceHost(webService, new Uri(url));
                serviceHost.Open();
                Utils.structuredLog(logger, "I", "ConfigUpdaterWebService initiated at " + url);
            }
        }

        public UpdateStatus LastStatus()
        {
            lock (this.status)
            {
                return this.status;
            }
        }

        public bool SetDueTime(int dueTime)
        {
            lock (this.timer)
            {
                return timer.Change(dueTime, this.frequency);
            }

        }

        public void Dispose()
        {
            this.timer.Dispose();
            this.serviceHost.Close();
            GC.SuppressFinalize(this);
        }
    }
}
