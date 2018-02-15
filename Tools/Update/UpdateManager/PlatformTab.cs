using HomeOS.Hub.Common;
using HomeOS.Hub.Tools.PackagerHelper;
using HomeOS.Hub.Tools.UpdateHelper;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HomeOS.Hub.Tools.UpdateManager
{
    public partial class MainForm
    {
        const string PlatformName = "Platform";
        public const string PlatformFilename = "HomeOS.Hub.Platform";
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelPlatform;
        private System.Windows.Forms.Label labelPlatformCurrent;
        private System.Windows.Forms.Label labelPlatformDeployed;

        private System.Windows.Forms.Label labelPlatDeployedVer;
        private System.Windows.Forms.Label labelPlatYourVer;
        private System.Windows.Forms.Button buttonPlatRefresh;
        private System.Windows.Forms.Button buttonPlatUpdate;

        protected string deployedPlatformVersionCached = PackagerHelper.BinaryPackagerHelper.UnknownHomeOSUpdateVersionValue;
        protected string myPlatformVersionCached = PackagerHelper.BinaryPackagerHelper.UnknownHomeOSUpdateVersionValue;

        private string GetMyPlatformVersion(bool useCachedValue)
        {
            string homeOSPlatformVersion = PackagerHelper.BinaryPackagerHelper.UnknownHomeOSUpdateVersionValue;
            if (useCachedValue)
            {
                if (this.myPlatformVersionCached != PackagerHelper.BinaryPackagerHelper.UnknownHomeOSUpdateVersionValue)
                {
                    homeOSPlatformVersion = this.myPlatformVersionCached;
                    goto Exit;
                }
            }
            homeOSPlatformVersion = Utils.GetHomeOSUpdateVersion(this.textBoxSetupWorkingFolder.Text + "\\binaries\\Platform\\" + PlatformFilename + ".exe.config");

        Exit:
            this.myPlatformVersionCached = homeOSPlatformVersion;
            return homeOSPlatformVersion;
        }

        private string GetLatestDeployedPlatformVersion(bool useCachedValue)
        {
            string homeOSPlatformVersion = PackagerHelper.BinaryPackagerHelper.UnknownHomeOSUpdateVersionValue;
            string remoteUsername = Properties.Settings.Default.SetupRepoAccountLogin;
            string remoteUserPassword = SimpleEncryptDecrypt(Properties.Settings.Default.SetupRepoAccountPassword, null);
            string remoteHost = Properties.Settings.Default.SetupRepoAccountHost;
            string remotePort = Properties.Settings.Default.SetupRepoAccountPort;
            string uri = remoteHost + ":" + remotePort + "/HomeOS/Hub/Platform/Latest/" + PlatformFilename + ".zip";
            string localZipFile = ".\\" + PlatformFilename + ".zip";
            string tmpFolder = ".\\" + PlatformFilename + ".tmp";

            if (useCachedValue)
            {
                if (this.deployedPlatformVersionCached != PackagerHelper.BinaryPackagerHelper.UnknownHomeOSUpdateVersionValue)
                {
                    homeOSPlatformVersion = this.deployedPlatformVersionCached;
                    goto Exit;
                }
            }

            if (File.Exists(localZipFile))
            {
                File.Delete(localZipFile);
            }

            if (Directory.Exists(tmpFolder))
            {
                PackagerHelper.PackagerHelper.DeleteFolder(tmpFolder, true);
            }

            if (!SecureFtpRepoUpdate.GetZipFromUrl(new Uri(uri), localZipFile, remoteUsername, remoteUserPassword, true /* enableSSL */))
            {
                logger.Error("Failed to download the latest platform zip");
                outputBox.Text += "Failed to download the latest platform zip\r\n";
                goto Exit;
            }
            if (!PackagerHelper.PackagerHelper.ExtractZipToFolder(localZipFile, tmpFolder))
            {
                string s1 = string.Format("Failed to extract platform zip file {0} to folder location: {1}", localZipFile, tmpFolder);
                logger.Error(s1);
                outputBox.Text += s1;           
                goto Exit;
            }

            homeOSPlatformVersion = Utils.GetHomeOSUpdateVersion(tmpFolder + "\\" + PlatformFilename + ".exe.config");

        Exit:
            this.deployedPlatformVersionCached = homeOSPlatformVersion;
            return homeOSPlatformVersion;
        }

        private void OnShowingPlatformTab()
        {
            outputBox.Text += "Getting Platform Version information \r\n";
            GetLatestDeployedPlatformVersion(false);
            GetMyPlatformVersion(false);

            LoadPlatformTabComponents();
            outputBox.Text += "Platform tab loaded \r\n";
        }

        private void LoadPlatformTabComponents()
        {
            outputBox.Text += "Loading Platform tab components \r\n";
            int currentRow = 0;

            // remove existing tab contents
            if (null != this.tableLayoutPanelPlatform)
            {
                this.tabPlatform.Controls.Remove(this.tableLayoutPanelPlatform);
            }

            // create the components that are not dynamic
            this.tableLayoutPanelPlatform = new System.Windows.Forms.TableLayoutPanel();
            this.labelPlatformCurrent = new System.Windows.Forms.Label();
            this.labelPlatformDeployed = new System.Windows.Forms.Label();

            // suspend all layout
            tabPlatform.SuspendLayout();
            this.tableLayoutPanelPlatform.SuspendLayout();


            this.tableLayoutPanelPlatform.RowCount = 3;

            // initialize the non-dynamic components

            // 
            // tableLayoutPanelPlatform
            // 
            this.tableLayoutPanelPlatform.AutoScroll = true;
            this.tableLayoutPanelPlatform.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            this.tableLayoutPanelPlatform.ColumnCount = 6;
            this.tableLayoutPanelPlatform.AutoSize = true;
            this.tableLayoutPanelPlatform.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelPlatform.Name = "tableLayoutPanelPlatform";
            this.tableLayoutPanelPlatform.MinimumSize = new Size(tabPlatform.Size.Width, tabPlatform.Size.Height);
            this.tableLayoutPanelPlatform.MaximumSize = tabPlatform.Size;
            this.tableLayoutPanelPlatform.TabIndex = 0;

            tabPlatform.Controls.Add(this.tableLayoutPanelPlatform);

            // 
            // labelPlatformCurrent
            // 
            this.labelPlatformCurrent.Dock = System.Windows.Forms.DockStyle.None;
            this.labelPlatformCurrent.AutoSize = false;
            this.labelPlatformCurrent.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPlatformCurrent.Name = "labelPlatformCurrent";
            this.labelPlatformCurrent.TabIndex = 4;
            this.labelPlatformCurrent.Text = "Local";
            this.labelPlatformCurrent.Margin = new Padding(0);
            // 
            // labelPlatformDeployed
            // 
            this.labelPlatformDeployed.Dock = System.Windows.Forms.DockStyle.None;
            this.labelPlatformDeployed.AutoSize = false;
            this.labelPlatformDeployed.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPlatformDeployed.Name = "labelPlatformDeployed";
            this.labelPlatformDeployed.TabIndex = 4;
            this.labelPlatformDeployed.Text = "On FTP";
            this.labelPlatformDeployed.Margin = new Padding(0);


            // add some of the non-dyamic components to the layout panel

            this.tableLayoutPanelPlatform.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelPlatform.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelPlatform.Controls.Add(this.labelPlatformDeployed, 1, currentRow);
            this.tableLayoutPanelPlatform.Controls.Add(this.labelPlatformCurrent, 2, currentRow++);


            // 
            // labelPlatDeployedVer
            // 
            this.labelPlatDeployedVer = new System.Windows.Forms.Label();
            this.labelPlatDeployedVer.AutoSize = false;
            this.labelPlatDeployedVer.Dock = System.Windows.Forms.DockStyle.None;
            //this.labelPlatDeployedVer.Location = new System.Drawing.Point(115, 82);
            this.labelPlatDeployedVer.Name = "labelPlatDeployedVer";
            //this.labelPlatDeployedVer.Size = new System.Drawing.Size(113, 29);
            this.labelPlatDeployedVer.TabIndex = 6;
            this.labelPlatDeployedVer.Text = this.deployedPlatformVersionCached;
            this.labelPlatDeployedVer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelPlatDeployedVer.Margin = new Padding(0);
            // 
            // labelPlatYourVer
            // 
            this.labelPlatYourVer = new System.Windows.Forms.Label();
            this.labelPlatYourVer.AutoSize = false;
            this.labelPlatYourVer.Dock = System.Windows.Forms.DockStyle.None;
            //this.labelPlatYourVer.Location = new System.Drawing.Point(234, 82);
            this.labelPlatYourVer.Name = "labelPlatYourVer";
            //this.labelPlatYourVer.Size = new System.Drawing.Size(112, 29);
            this.labelPlatYourVer.TabIndex = 7;
            this.labelPlatYourVer.Text = this.myPlatformVersionCached;
            this.labelPlatYourVer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelPlatYourVer.Margin = new Padding(0);
            // 
            // buttonPlatRefresh
            // 
            this.buttonPlatRefresh = new System.Windows.Forms.Button();
            this.tableLayoutPanelPlatform.SetColumnSpan(this.buttonPlatRefresh, 2);
            this.buttonPlatRefresh.AutoSize = false;
            this.buttonPlatRefresh.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPlatRefresh.Name = "buttonPlatRefresh";
            this.buttonPlatRefresh.TabIndex = 8;
            this.buttonPlatRefresh.Text = "Refresh";
            this.buttonPlatRefresh.UseVisualStyleBackColor = true;
            this.buttonPlatRefresh.Margin = new Padding(0);
            this.buttonPlatRefresh.Click += buttonPlatRefresh_Click;
            // 
            // buttonPlatUpdate
            // 
            this.buttonPlatUpdate = new System.Windows.Forms.Button();
            this.tableLayoutPanelPlatform.SetColumnSpan(this.buttonPlatUpdate, 2);
            this.buttonPlatUpdate.AutoSize = false;
            this.buttonPlatUpdate.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPlatUpdate.Name = "buttonPlatUpdate";
            this.buttonPlatUpdate.TabIndex = 8;
            this.buttonPlatUpdate.Text = "Update";
            this.buttonPlatUpdate.UseVisualStyleBackColor = true;
            this.buttonPlatUpdate.Margin = new Padding(0);
            this.buttonPlatUpdate.Click += new System.EventHandler(this.buttonPlatUpdate_Click);

            this.tableLayoutPanelPlatform.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelPlatform.Controls.Add(this.labelPlatDeployedVer, 1, currentRow);
            this.tableLayoutPanelPlatform.Controls.Add(this.labelPlatYourVer, 2, currentRow++);
            this.tableLayoutPanelPlatform.Controls.Add(this.buttonPlatRefresh, 0, currentRow);
            this.tableLayoutPanelPlatform.Controls.Add(this.buttonPlatUpdate, 1, currentRow++);


            Debug.Assert(currentRow == this.tableLayoutPanelPlatform.RowCount);

            // resume layout 
            tabPlatform.ResumeLayout(false);
            this.tableLayoutPanelPlatform.ResumeLayout(false);
            this.tableLayoutPanelPlatform.PerformLayout();

        }

        void buttonPlatRefresh_Click(object sender, EventArgs e)
        {
            buttonPlatRefresh.Enabled = false;
            OnShowingPlatformTab();
            buttonPlatRefresh.Enabled = true;
        }

       
        private void buttonPlatUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                
                TableLayoutControlCollection tableLayoutCtrlColl = this.tableLayoutPanelPlatform.Controls;
                System.Windows.Forms.Control labelDepVer = tableLayoutCtrlColl[this.labelPlatDeployedVer.Name];
                System.Windows.Forms.Control labelCurVer = tableLayoutCtrlColl[this.labelPlatYourVer.Name];
                // string hubId = checkbox.Text;
                string deployedVersion = labelDepVer.Text;
                string currentVersion = labelCurVer.Text;
                string platformDirPath = "";
                Version latestVersion = null;
                string platformRootDir = this.textBoxSetupWorkingFolder.Text + "\\binaries\\Platform";
                string repositoryDir = this.textBoxSetupWorkingFolder.Text + "\\HomeStore\\Repository";
                string ftpHost = this.formRepoAccountInfo.RepoAccountHost;
                string ftpPort = this.formRepoAccountInfo.RepoAccountPort;
                string ftpUser = this.formRepoAccountInfo.RepoAccountLogin;
                string ftpPassword = this.formRepoAccountInfo.RepoAccountPassword;

                // make sure you are updating to a higher version
                Version versionDeployed = new Version(deployedVersion);
                Version versionCurrent = new Version(currentVersion);

                if (versionCurrent <= versionDeployed)
                {
                    MessageBox.Show(string.Format("Only Updates to higher versions of the platform are permissable, please update the version!"),
                                    "Update Platform",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation
                                    );
                    return;
                }

                if (DialogResult.Yes == MessageBox.Show((IWin32Window)sender, string.Format("Are you sure you want to update the platform from the deployed version {0} to your current version {1}?", deployedVersion, currentVersion), "Update Platform", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2))
                {
                    string[] filePaths = new string[0];
                    bool success = BinaryPackagerHelper.Package(platformRootDir, MainForm.PlatformFilename, true /*singleBin*/, "exe", "platform", repositoryDir, ref filePaths, this.logger);
                    if (!success)
                    {
                        throw new Exception("Failed to create platform update packages");
                    }

                    Debug.Assert(filePaths.Length == 2);

                    for (int i = 0; i < filePaths.Length; ++i)
                    {
                        string[] ftpFileLocations = filePaths[i].Split(new[] { repositoryDir }, StringSplitOptions.RemoveEmptyEntries);
                        if (ftpFileLocations.Length > 0 && !string.IsNullOrWhiteSpace(ftpFileLocations[0]))
                        {
                            string ftpFilePath = ftpFileLocations[0].Replace("\\", "/");
                            string ftpDirPath = ftpFilePath.Substring(0, ftpFilePath.LastIndexOf("/"));
                            Uri uriFile = new Uri(ftpHost + ":" + ftpPort + ftpFilePath);
                            string subDirPath = "";
                            foreach (string subdir in ftpDirPath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                subDirPath += ("/" + subdir);
                                if (platformDirPath.Length == 0 && subDirPath.ToLower().Contains(PlatformName.ToLower()))
                                {
                                    platformDirPath = subDirPath;
                                }
                                if (subdir.Contains('.'))
                                {
                                    // assume this is the version number of the type x.x.x.x
                                    latestVersion = new Version(subdir);
                                }
                                Uri uriDir = new Uri(ftpHost + ":" + ftpPort + subDirPath);
                                if (!MainForm.IsFtpRemoteDirectoryPresent(uriDir, ftpUser, ftpPassword))
                                {
                                    SecureFtpRepoUpdate.MakeDirectory(uriDir, ftpUser, ftpPassword, true);
                                }
                            }

                            SecureFtpRepoUpdate.UploadFile(uriFile, filePaths[i], ftpUser, ftpPassword, true);
                        }
                    }
                    // update the latest folder for the platform, if this is highest version or if there is no latest folder at all
                    Uri uriPlatformDir = new Uri(ftpHost + ":" + ftpPort + platformDirPath);
                    string platformLatestDir = ftpHost + ":" + ftpPort + platformDirPath + "/Latest";
                    Uri uriPlatformLatestDir = new Uri(platformLatestDir);
                    if (!MainForm.IsFtpRemoteDirectoryPresent(uriPlatformLatestDir, ftpUser, ftpPassword) || MainForm.GetFtpHighestVersionFromDir(uriPlatformDir, ftpUser, ftpPassword, this.logger) == latestVersion)
                    {
                        for (int i = 0; i < filePaths.Length; ++i)
                        {
                            string filename = filePaths[i].Substring(filePaths[i].LastIndexOf('\\') + 1);
                            Uri uriFile = new Uri(platformLatestDir + "/" + filename);
                            if (!MainForm.IsFtpRemoteDirectoryPresent(uriPlatformLatestDir, ftpUser, ftpPassword))
                            {
                                SecureFtpRepoUpdate.MakeDirectory(uriPlatformLatestDir, ftpUser, ftpPassword, true);
                            }

                            SecureFtpRepoUpdate.UploadFile(uriFile, filePaths[i], ftpUser, ftpPassword, true);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                this.logger.ErrorException("Exception while trying to update platform on the ftp server", exception);
                outputBox.Text += "Exception while trying to update platform on the ftp server" + exception + "\r\n";
            }
        }

    }
}
