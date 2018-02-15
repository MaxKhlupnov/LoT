using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using HomeOS.Hub.Tools.UpdateHelper;
using System.IO;

namespace HomeOS.Hub.Tools.UpdateManager
{
    public class AzureHubConfigDataLocalCache : IDisposable
    {
        private const string ActualConfigZipName = "actualconfig.zip";
        private const string DesiredConfigDownloadedZipName = "desiredconfig_downloaded.zip";
        private const string DesiredConfigLocalZipName = "desiredconfig_local.zip";
        private const string CacheFolder = "config_cache";
        private const string ActualFolder = "actual";
        private const string DesiredFolder = "desired";
        private const string DownloadedFolder = "downloaded";
        private const string LocalFolder = "local";

        protected NLog.Logger logger;

        public class TripleId : Tuple<string, string, string>
        {
            public TripleId(string orgId, string studyId, string hubId)
                : base(orgId, studyId, hubId)
            {
            }

            public string OrgId
            {
                get
                {
                    return this.Item1;
                }
            }

            public string StudyId
            {
                get
                {
                    return this.Item2;
                }
            }

            public string HubId
            {
                get
                {
                    return this.Item3;
                }
            }
        }

        protected string AzureAccount;
        protected string AzureKey;
        protected Dictionary<string /*orgId*/, Dictionary<string /*studyId*/, Dictionary<string /*hubId*/, string /*configFilePath*/>>> tripleIdLookUp;
        protected bool cacheBuilt;
        protected bool disposed;

        public AzureHubConfigDataLocalCache(string accountName, string accountKey, Logger loggerIn)
        {
            this.AzureAccount = accountName;
            this.AzureKey = accountKey;
            this.logger = loggerIn;

            this.tripleIdLookUp = new Dictionary<string /*orgId*/, Dictionary<string /*studyId*/, Dictionary<string /*hubId*/, string /*configFilePath*/>>>();

            this.cacheBuilt = false;
            this.disposed = false;
        }

        private void ExtractConfigPackage(string orgId, string studyId, string hubId, string parentFolder, string zipPackFilePath, bool deletePackage = true)
        {
            PackagerHelper.PackagerHelper.ExtractZipToFolder(zipPackFilePath, parentFolder);
            this.tripleIdLookUp[orgId][studyId][hubId] = parentFolder;
            if (deletePackage)
            {
                File.Delete(zipPackFilePath);
            }
        }

        public void BuildCache()
        {
            if (this.cacheBuilt)
                return;

            // get all the orgs
            Tuple<bool, List<string>> orgListTuple = HomeOS.Hub.Tools.UpdateHelper.AzureBlobConfigUpdate.listOrgs(this.AzureAccount, this.AzureKey);
            if (!orgListTuple.Item1)
            {
                logger.Error(orgListTuple.Item2);
                return;
            }

            foreach (string orgId in orgListTuple.Item2)
            {
                this.tripleIdLookUp[orgId] = new Dictionary<string, Dictionary<string, string>>();
                bool hubListFailed = false;
                Tuple<bool, List<string>> studyListTuple = HomeOS.Hub.Tools.UpdateHelper.AzureBlobConfigUpdate.listStudies(this.AzureAccount, this.AzureKey, orgId);
                if (!orgListTuple.Item1)
                {
                    logger.Error(orgListTuple.Item2);
                    break;
                }
                foreach (string studyId in studyListTuple.Item2)
                {
                    this.tripleIdLookUp[orgId][studyId] = new Dictionary<string, string>();
                    Tuple<bool, List<string>> hubListTuple = UpdateHelper.AzureBlobConfigUpdate.listHubs(this.AzureAccount, this.AzureKey, orgId, studyId);
                    if (!studyListTuple.Item1)
                    {
                        logger.Error(studyListTuple.Item2);
                        hubListFailed = true;
                        break;
                    }
                    foreach (string hubId in hubListTuple.Item2)
                    {
                        string tmpFolder = ".\\" + CacheFolder + "\\" + orgId + "\\" + studyId + "\\" + hubId;
                        PackagerHelper.PackagerHelper.CreateFolder(tmpFolder + "\\" + ActualFolder); // deletes existing if present
                        PackagerHelper.PackagerHelper.CreateFolder(tmpFolder + "\\" + DesiredFolder + "\\" + DownloadedFolder); // deletes existing if present
                        PackagerHelper.PackagerHelper.CreateFolder(tmpFolder + "\\" + DesiredFolder + "\\" + LocalFolder, false); // don't delete existing if present
                        string actualZipFilePath = tmpFolder + "\\" + ActualConfigZipName;
                        string desiredDownloadZipFilePath = tmpFolder + "\\" + DesiredConfigDownloadedZipName;
                        string desiredLocalZipFilePath = tmpFolder + "\\" + DesiredConfigLocalZipName;

                        this.tripleIdLookUp[orgId][studyId][hubId] = "";
                        if (AzureBlobConfigUpdate.DownloadConfig(actualZipFilePath, this.AzureAccount, this.AzureKey, orgId, studyId, hubId,
                            PackagerHelper.ConfigPackagerHelper.actualConfigFileName, this.logger))
                        {
                            ExtractConfigPackage(orgId, studyId, hubId, tmpFolder + "\\" + ActualFolder, actualZipFilePath, false /*deletePackage*/);
                            this.tripleIdLookUp[orgId][studyId][hubId] = tmpFolder;
                        }
                        if (AzureBlobConfigUpdate.DownloadConfig(desiredDownloadZipFilePath, this.AzureAccount, this.AzureKey, orgId, studyId, hubId,
                            PackagerHelper.ConfigPackagerHelper.desiredConfigFileName, this.logger))
                        {
                            ExtractConfigPackage(orgId, studyId, hubId, tmpFolder + "\\" + DesiredFolder + "\\" + DownloadedFolder, desiredDownloadZipFilePath, false /*deletePackage*/);
                            this.tripleIdLookUp[orgId][studyId][hubId] = tmpFolder;
                        }
                    }
                }
                if (hubListFailed)
                    break;
            }

        }

        public List<string> GetOrgIdList()
        {
            return this.tripleIdLookUp.Keys.ToList();
        }

        public List<string> GetStudyIdListForOrg(string OrgId)
        {
            return this.tripleIdLookUp[OrgId].Keys.ToList();
        }

        public List<string> GetHubIdListForOrgStudyId(string orgId, string studyId)
        {
            return this.tripleIdLookUp[orgId][studyId].Keys.ToList();
        }

        public string GetRootConfigFolderPath(string orgId, string studyId, string hubId)
        {
            return this.tripleIdLookUp[orgId][studyId][hubId];
        }

        public string GetActualConfigFolderPath(string orgId, string studyId, string hubId)
        {
            return this.tripleIdLookUp[orgId][studyId][hubId] + "\\" + ActualFolder;
        }

        public string GetDesiredConfigDownloadedFolderPath(string orgId, string studyId, string hubId)
        {
            return this.tripleIdLookUp[orgId][studyId][hubId] + "\\" + DesiredFolder + "\\" + DownloadedFolder;
        }

        public string GetDesiredConfigLocalFolderPath(string orgId, string studyId, string hubId)
        {
            return this.tripleIdLookUp[orgId][studyId][hubId] + "\\" + DesiredFolder + "\\" + LocalFolder;
        }

        public string GetActualConfigZipFilePath(string orgId, string studyId, string hubId)
        {
            return this.tripleIdLookUp[orgId][studyId][hubId] + "\\" + ActualConfigZipName;
        }

        public string GetDesiredConfigDownloadedZipFilePath(string orgId, string studyId, string hubId)
        {
            return this.tripleIdLookUp[orgId][studyId][hubId] + "\\" + DesiredConfigDownloadedZipName;
        }

        public string GetDesiredConfigLocalZipFilePath(string orgId, string studyId, string hubId)
        {
            return this.tripleIdLookUp[orgId][studyId][hubId] + "\\" + DesiredConfigLocalZipName;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.tripleIdLookUp.Clear();
                }

                this.disposed = true;
            }
        }
    }
}
