using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Controls;
using HomeOS.Hub.Tools.PackagerHelper;
using HomeOS.Hub.Tools.UpdateHelper;
using System.IO;
using System.Diagnostics;

namespace HomeOS.Hub.Tools.UpdateManager
{
    public class HubOtherUpdatePanelRowItem
    {
        private System.Windows.Forms.Label labelBinaryName;
        private System.Windows.Forms.Label labelBinaryLatestVer;
        private System.Windows.Forms.Label labelBinaryYourVer;
        private System.Windows.Forms.Button buttonBinaryUpdate;

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private int rowNumber;
        private string binaryType;
        private string binaryRootDir;
        private string repositoryDir;
        private string ftpHost;
        private string ftpPort;
        private string ftpUser;
        private string ftpPassword;
        private NLog.Logger logger;

        public HubOtherUpdatePanelRowItem(
                    System.Windows.Forms.TableLayoutPanel tableLayoutPanel,
                    int rowNumber,
                    string binaryName,
                    string binaryType,
                    string latestVer,
                    string yourVer,
                    bool yourVerInRep,
                    string binaryRootDir,
                    string repositoryDir,
                    string host,
                    string port,
                    string login,
                    string password,
                    NLog.Logger logger
                    )
        {
            this.tableLayoutPanel = tableLayoutPanel;
            this.rowNumber = rowNumber;
            this.binaryType = binaryType;
            this.binaryRootDir = binaryRootDir;
            this.repositoryDir = repositoryDir;
            this.ftpHost = host;
            this.ftpPort = port;
            this.ftpUser = login;
            this.ftpPassword = password;
            this.logger = logger;

            // 
            // labelBinaryName
            // 
            this.labelBinaryName = new System.Windows.Forms.Label();
            this.labelBinaryName.AutoSize = true;
            this.labelBinaryName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelBinaryName.Name = "labelBinaryName" + this.rowNumber.ToString();
            this.labelBinaryName.TabIndex = 5;
            this.labelBinaryName.Text = binaryName;
            this.labelBinaryName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelBinaryName.Margin = new Padding(0);

            // 
            // labelBinaryLatestVer
            // 
            this.labelBinaryLatestVer = new System.Windows.Forms.Label();
            this.labelBinaryLatestVer.AutoSize = false;
            this.labelBinaryLatestVer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelBinaryLatestVer.Name = "labelBinaryLatestVer" + this.rowNumber.ToString();
            this.labelBinaryLatestVer.TabIndex = 6;
            this.labelBinaryLatestVer.Text = latestVer;
            this.labelBinaryLatestVer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelBinaryLatestVer.Margin = new Padding(0);
            // 
            // labelBinaryYourVer
            // 
            this.labelBinaryYourVer = new System.Windows.Forms.Label();
            this.labelBinaryYourVer.AutoSize = false;
            this.labelBinaryYourVer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelBinaryYourVer.Name = "labelBinaryYourVer" + this.rowNumber.ToString();
            this.labelBinaryYourVer.TabIndex = 7;
            this.labelBinaryYourVer.Text = yourVer;
            this.labelBinaryYourVer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelBinaryYourVer.Margin = new Padding(0);
            // 
            // buttonBinaryUpdate
            // 
            this.buttonBinaryUpdate = new System.Windows.Forms.Button();
            this.tableLayoutPanel.SetColumnSpan(this.buttonBinaryUpdate, 2);
            this.buttonBinaryUpdate.AutoSize = false;
            this.buttonBinaryUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonBinaryUpdate.Name = "buttonBinaryUpdate" + this.rowNumber.ToString();
            this.buttonBinaryUpdate.TabIndex = 8;
            if (yourVerInRep)
            {
                this.buttonBinaryUpdate.Text = "Present";
                this.buttonBinaryUpdate.Enabled = false;
            }
            else
            {
                this.buttonBinaryUpdate.Text = "Add";
                this.buttonBinaryUpdate.Enabled = true;
            }
            this.buttonBinaryUpdate.UseVisualStyleBackColor = true;
            this.buttonBinaryUpdate.Margin = new Padding(0);
            this.buttonBinaryUpdate.Click += new System.EventHandler(this.buttonBinaryUpdate_Click);

            this.tableLayoutPanel.Controls.Add(this.labelBinaryName, 0, rowNumber);
            this.tableLayoutPanel.Controls.Add(this.labelBinaryLatestVer, 1, rowNumber);
            this.tableLayoutPanel.Controls.Add(this.labelBinaryYourVer, 2, rowNumber);
            this.tableLayoutPanel.Controls.Add(this.buttonBinaryUpdate, 3, rowNumber);

        }

        private void buttonBinaryUpdate_Click(object sender, EventArgs e)
        {
            TableLayoutControlCollection tableLayoutCtrlColl = this.tableLayoutPanel.Controls;
            System.Windows.Forms.Control labelBinaryName = tableLayoutCtrlColl[this.labelBinaryName.Name];
            System.Windows.Forms.Control labelLatestVer = tableLayoutCtrlColl[this.labelBinaryLatestVer.Name];
            System.Windows.Forms.Control labelCurVer = tableLayoutCtrlColl[this.labelBinaryYourVer.Name];
            string binaryName = labelBinaryName.Text;
            string currentVersion = labelCurVer.Text;

            if (DialogResult.Yes == MessageBox.Show((IWin32Window)sender, string.Format("Are you sure you want to add {0} {1} with version {2} to the repository?",
                                        this.binaryType, binaryName, currentVersion), "Update Repository", MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2))
            {
                string[] filePaths = new string[0];
                bool success = BinaryPackagerHelper.Package(this.binaryRootDir, binaryName, false /*singleBin*/, "dll",
                                                            this.binaryType, this.repositoryDir, ref filePaths, this.logger);
                if (!success)
                {
                    logger.Error("Failed to create {0} update packages", this.binaryType);
                }
                List<MainForm.BinaryPackageItem> binaryPackageItemList = new List<MainForm.BinaryPackageItem>();
                binaryPackageItemList.Add(new MainForm.BinaryPackageItem(this.binaryType, filePaths));
                UpdateRepositoryBinaries(binaryPackageItemList);

            }
        }

        private void UpdateRepositoryBinaries(List<MainForm.BinaryPackageItem> binaryPackageList)
        {
            foreach (MainForm.BinaryPackageItem item in binaryPackageList)
            {
                if (!UpdateRepositoryWithFiles(item.packageFilePaths, item.binType))
                {
                    foreach (string packageFile in item.packageFilePaths)
                    {
                        logger.Error("Failed to update repository for {0} : {1}", item.binType, packageFile);
                    }
                }
            }
        }

        private bool UpdateRepositoryWithFiles(string[] filePaths, string binType)
        {
            bool success = true;
            try
            {
                Version latestVersion = null;
                string binaryDirPath = "";
                for (int i = 0; i < filePaths.Length; ++i)
                {
                    string[] ftpFileLocations = filePaths[i].Split(new[] { this.repositoryDir }, StringSplitOptions.RemoveEmptyEntries);
                    if (ftpFileLocations.Length > 0 && !string.IsNullOrWhiteSpace(ftpFileLocations[0]))
                    {
                        string ftpFilePath = ftpFileLocations[0].Replace("\\", "/");
                        string ftpDirPath = ftpFilePath.Substring(0, ftpFilePath.LastIndexOf("/"));
                        Uri uriFile = new Uri(this.ftpHost + ":" + this.ftpPort + ftpFilePath);
                        string subDirPath = "";
                        bool gotBinaryDirPath = false;
                        foreach (string subdir in ftpDirPath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            subDirPath += ("/" + subdir);
                            if (gotBinaryDirPath)
                            {
                                binaryDirPath = subDirPath;
                                gotBinaryDirPath = false;
                            }
                            else if (binaryDirPath.Length == 0 && subDirPath.ToLower().Contains(binType.ToLower()))
                            {
                                gotBinaryDirPath = true;
                            }
                            else if (subdir.Contains('.'))
                            {
                                // assume this is the version number of the type x.x.x.x
                                latestVersion = new Version(subdir);
                                
                                //fix the bug that causes incorrect path to be formed for Latest dir for x/Gadgeteer/MicrosoftResearch/X  
                                binaryDirPath = subDirPath.Substring(0, subDirPath.LastIndexOf("/")); 

                            }
                            Uri uriDir = new Uri(this.ftpHost + ":" + this.ftpPort + subDirPath);
                            if (!MainForm.IsFtpRemoteDirectoryPresent(uriDir, this.ftpUser, this.ftpPassword))
                            {
                                SecureFtpRepoUpdate.MakeDirectory(uriDir, this.ftpUser, this.ftpPassword, true);
                            }
                        }

                        SecureFtpRepoUpdate.UploadFile(uriFile, filePaths[i], this.ftpUser, this.ftpPassword, true);
                    }
                }
                // update the latest folder for the binary, if this is highest version or if there is no latest folder at all
                
 
                Uri uriBinaryDir = new Uri(this.ftpHost + ":" + this.ftpPort + binaryDirPath);
                string binaryLatestDir = this.ftpHost + ":" + this.ftpPort + binaryDirPath + "/Latest";

                Uri uriBinaryLatestDir = new Uri(binaryLatestDir);
                if (!MainForm.IsFtpRemoteDirectoryPresent(uriBinaryLatestDir, this.ftpUser, this.ftpPassword) ||
                    MainForm.GetFtpHighestVersionFromDir(uriBinaryDir, this.ftpUser, this.ftpPassword, this.logger) == latestVersion)
                {
                    for (int i = 0; i < filePaths.Length; ++i)
                    {
                        string filename = filePaths[i].Substring(filePaths[i].LastIndexOf('\\') + 1);
                        Uri uriFile = new Uri(binaryLatestDir + "/" + filename);
                        if (!MainForm.IsFtpRemoteDirectoryPresent(uriBinaryLatestDir, this.ftpUser, this.ftpPassword))
                        {
                            SecureFtpRepoUpdate.MakeDirectory(uriBinaryLatestDir, this.ftpUser, this.ftpPassword, true);
                        }

                        SecureFtpRepoUpdate.UploadFile(uriFile, filePaths[i], this.ftpUser, this.ftpPassword, true);
                    }

                    
                    //Here we will upload the [binary].dll.config file, which contains the homeosupdate version of the binary, to the Latest dir on the repository   
                     
                    string versionfile = filePaths[0].Substring(filePaths[0].LastIndexOf('\\') + 1).Replace("zip", "dll.config");
                    string binaryname = versionfile.Replace(".dll.config", "");
                    string versionfilepath = this.binaryRootDir + "\\" + binaryname + "\\" + versionfile;
                    
                    Uri uriversionfile = new Uri(binaryLatestDir + "/" + versionfile);

                    SecureFtpRepoUpdate.UploadFile(uriversionfile, versionfilepath, this.ftpUser, this.ftpPassword, true);




                }
            }
            catch (Exception exception)
            {
                this.logger.ErrorException("Exception while trying to update binary package (plus hash file) on the ftp server", exception);

                success = false;
            }

            return success;
        }

    }
}
