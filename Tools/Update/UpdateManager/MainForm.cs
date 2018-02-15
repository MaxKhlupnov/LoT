using HomeOS.Hub.Common;
using HomeOS.Hub.Tools.UpdateHelper;
using NLog;
using System;
using System.AddIn.Hosting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeOS.Hub.Tools.UpdateManager
{

    public partial class MainForm : Form
    {
        public class BinaryPackageItem
        {
            public BinaryPackageItem(string binType, string[] packageFilePaths)
            {
                this.binType = binType;
                this.packageFilePaths = packageFilePaths;
            }
            public string binType { get; private set; }
            public string[] packageFilePaths { get; private set; }
        }

        public NLog.Logger logger;

        public MainForm()
        {
            InitializeComponent();

            logger = LogManager.GetCurrentClassLogger();

            this.FormClosing += MainForm_FormClosing;

            ShowSetupTab();
        }


        private bool IsValidWorkingDirPresent()
        {
            
            return (!string.IsNullOrWhiteSpace(this.textBoxSetupWorkingFolder.Text) && (new DirectoryInfo(this.textBoxSetupWorkingFolder.Text).Exists));
        }

        private bool IsValidRepositoryAccountPresent()
        {
           
            bool valid = true;
            try
            {
                string[] directories = SecureFtpRepoUpdate.ListDirectory(new Uri(this.formRepoAccountInfo.RepoAccountHost + ":" + this.formRepoAccountInfo.RepoAccountPort),
                                                     this.formRepoAccountInfo.RepoAccountLogin,
                                                     this.formRepoAccountInfo.RepoAccountPassword,
                                                     false,
                                                     true
                                                     );
            }
            catch (Exception ex)
            {
                logger.ErrorException("Failed to list directories on the ftp server", ex);
                valid = false;
            }

            return valid;
        }

        private bool IsValidAzureStorageAcctPresent()
        {
            bool valid = true;
            try
            {      
                Tuple<bool, Exception> tuple = AzureBlobConfigUpdate.IsValidAccount(this.formAzureAccount.AzureAccountName, this.formAzureAccount.AzureAccountKey);
                if (!tuple.Item1)
                {
                    throw tuple.Item2;
                }
               
            }
            catch (Exception ex)
            {
                logger.ErrorException("Failed to validate the azure storage account", ex);
                outputBox.Text += "Failed to validate the azure storage account" + ex + "\r\n";
                valid = false;
            }

            return valid;
        }

        private async Task<bool> TryShowingBinaryTabs()
        {
            bool show = false;
            outputBox.Text += "Checking for Valid Working Directory \r\n";
            bool validWorkingDir = await Task.Run(() => IsValidWorkingDirPresent());
            outputBox.Text += "Checking for Valid Repository Account \r\n";
            bool validRepoAccount = await Task.Run(() => IsValidRepositoryAccountPresent());

            if (validWorkingDir && validRepoAccount)
            {
                if (!this.controlMainFormTab.Controls.Contains(this.tabPlatform))
                {
                    this.controlMainFormTab.Controls.Add(this.tabPlatform);
                }
                if (!this.controlMainFormTab.Controls.Contains(this.tabModuleScouts))
                {
                    this.controlMainFormTab.Controls.Add(this.tabModuleScouts); 
                }
                show = true;
            }
            else
            {
                if (this.controlMainFormTab.Controls.Contains(this.tabPlatform))
                {
                    this.controlMainFormTab.Controls.Remove(this.tabPlatform);
                }
                if (this.controlMainFormTab.Controls.Contains(this.tabModuleScouts))
                {
                    this.controlMainFormTab.Controls.Remove(this.tabModuleScouts);
                }
                show = false;
            }
            return show;
        }

        private async Task<bool> TryShowingConfigsTab()
        {
            outputBox.Text += "Checking for valid azure account \r\n";
            bool show = false;
            bool validAzureStorageAccount = await Task.Run(() => IsValidAzureStorageAcctPresent());

            if (validAzureStorageAccount)
            {
                if (!this.controlMainFormTab.Controls.Contains(this.tabConfigs))
                {
                    this.controlMainFormTab.Controls.Add(this.tabConfigs);
                }
                show = true;
            }
            else
            {
                if (this.controlMainFormTab.Controls.Contains(this.tabConfigs))
                {
                    this.controlMainFormTab.Controls.Remove(this.tabConfigs);
                }
                show = false;
            }
            outputBox.Text += "Found valid azure account \r\n";
            return show;
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSetupSettings();
        }

        internal static bool IsFtpRemoteDirectoryPresent(Uri uriRemoteDirPath, string ftpUser, string ftpPassword)
        {
            bool present = false;
            try
            {
                string[] dirContents = SecureFtpRepoUpdate.ListDirectory(uriRemoteDirPath, ftpUser, ftpPassword, false /*details*/, true /*enableSSL*/);
                present = true;

            }
            catch (Exception)
            {
            }

            return present;
        }

        internal static Version GetFtpHighestVersionFromDir(Uri uriRemoteDirPath, string ftpUser, string ftpPassword, NLog.Logger logger)
        {
            string version = "0.0.0.0";
            Version highest = new Version(version);
            try
            {
                string[] dirContents = SecureFtpRepoUpdate.ListDirectory(uriRemoteDirPath, ftpUser, ftpPassword, false /*details*/, true /*enableSSL*/);
                for (int i = 0; i < dirContents.Length; ++i)
                {
                    if (dirContents[i] == "Latest")
                        continue;

                    Version ver = new Version(dirContents[i]);

                    if (ver > highest)
                        highest = ver;
                }
            }
            catch (Exception e)
            {
                logger.ErrorException("Failed to retrieve the latest version from remote ftp server", e);
             
            }

            return highest;
        }

        internal static bool IsFtpBinaryVersionPresent(Uri uriRemoteDirPath, string version, string ftpUser, string ftpPassword, NLog.Logger logger)
        {
            bool present = false;
            Version versionCheck = new Version(version);
            try
            {
                string[] dirContents = SecureFtpRepoUpdate.ListDirectory(uriRemoteDirPath, ftpUser, ftpPassword, false /*details*/, true /*enableSSL*/);
                for (int i = 0; i < dirContents.Length; ++i)
                {
                    if (dirContents[i] == version)
                        continue;

                    Version ver = new Version(dirContents[i]);

                    if (ver == versionCheck)
                    {
                        present = true;
                        break;
                    }                        
                }
            }
            catch (Exception e)
            {
                logger.ErrorException("Failed to retrieve the latest version from remote ftp server", e);
            }

            return present;
        }

        private bool NeedsRepoUpdate(string binType, string binaryName, string version)
        {
            Uri uriBinaryVersionDir = new Uri(this.formRepoAccountInfo.RepoAccountHost + ":" + this.formRepoAccountInfo.RepoAccountPort + "/" + binaryName.Replace('.', '/') + "/" + version);

            return !IsFtpRemoteDirectoryPresent(uriBinaryVersionDir, this.formRepoAccountInfo.RepoAccountLogin, this.formRepoAccountInfo.RepoAccountPassword);
        }

        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public static string SimpleEncryptDecrypt(string password, string key)
        {
            // get the bytes
            byte[] passwordBytes = GetBytes(password);
            byte[] newPasswordBytes = new byte[passwordBytes.Length];
            byte[] keyBytes = null;
            byte[] actualKeyBytes = null;

            if (string.IsNullOrWhiteSpace(key))
            {
                keyBytes = GetBytes(Utils.HardwareId);
            }
            else
            {
                keyBytes = GetBytes(key);
            }

            if (keyBytes.Length > passwordBytes.Length)
            {
                actualKeyBytes = new byte[passwordBytes.Length];
                Array.Copy(keyBytes, actualKeyBytes, passwordBytes.Length);
            }

            // do an bitwise xor, a byte at a time
            int i = 0;
            while (i < passwordBytes.Length)
            {
                for (int k = 0; (k < actualKeyBytes.Length) && (i < passwordBytes.Length); k++, i++)
                {
                    newPasswordBytes[i] = (byte)(passwordBytes[i] ^ actualKeyBytes[k]);
                }
            }

            return GetString(newPasswordBytes);
        }

    }
}
