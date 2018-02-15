using HomeOS.Hub.Common;
using HomeOS.Hub.Tools.UpdateHelper;
using System;
using System.AddIn.Hosting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace HomeOS.Hub.Tools.UpdateManager
{

    public partial class MainForm
    {

        protected List<HubOtherUpdatePanelRowItem> HubOtherUpdatePanelList;

        protected Dictionary<string /*app, driver, scout*/, List<Tuple<string /*binary name*/,
                                                                      string /*version*/,
                                                                      string /* latest version in Rep */,
                                                                      bool /* needs repo update*/>>> binaryUpdateStatus;


        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelModuleScout;
        private System.Windows.Forms.Label labelModuleScoutLatest;
        private System.Windows.Forms.Label labelModuleScoutYours;
        private System.Windows.Forms.Label labelApps;
        private System.Windows.Forms.Label labelDrivers;
        private System.Windows.Forms.Label labelScouts;
        private System.Windows.Forms.Button buttonModuleScoutRefresh;



        private void OnShowingModuleScoutTab()
        {
            outputBox.Text += "Loading the Module tab components \r\n";
            LoadModuleScoutTabComponents();
            outputBox.Text += "Finished loading Module tab\r\n";
        }

 
        private void LoadModuleScoutTabComponents()
        {
          
            int currentRow = 0;
            int binaryCount = 0;

            string ftpHost = this.formRepoAccountInfo.RepoAccountHost;
            string ftpPort = this.formRepoAccountInfo.RepoAccountPort;
            string ftpUser = this.formRepoAccountInfo.RepoAccountLogin;
            string ftpPassword = this.formRepoAccountInfo.RepoAccountPassword;

            this.binaryUpdateStatus = GenerateBinaryUpdateStatusList(ftpHost, ftpPort, ftpUser, ftpPassword);

            // remove existing tab contents
            if (null != this.tableLayoutPanelModuleScout)
            {
                this.tabModuleScouts.Controls.Remove(this.tableLayoutPanelModuleScout);
            }

            // create the components that are not dynamic
            this.tableLayoutPanelModuleScout = new System.Windows.Forms.TableLayoutPanel();
            this.buttonModuleScoutRefresh = new System.Windows.Forms.Button();
            this.labelModuleScoutLatest = new System.Windows.Forms.Label();
            this.labelModuleScoutYours = new System.Windows.Forms.Label();


            // suspend all layout
            tabModuleScouts.SuspendLayout();
            this.tableLayoutPanelModuleScout.SuspendLayout();

            // calc the total number of rows = # of binaries + 5
            binaryCount += this.binaryUpdateStatus[AppName].Count;
            binaryCount += this.binaryUpdateStatus[DriverName].Count;
            binaryCount += this.binaryUpdateStatus[ScoutName].Count;

            this.tableLayoutPanelModuleScout.RowCount = binaryCount + 5;

            // initialize the non-dynamic components

            // 
            // tableLayoutPanelModuleScout
            // 
            this.tableLayoutPanelModuleScout.AutoScroll = true;
            this.tableLayoutPanelModuleScout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            this.tableLayoutPanelModuleScout.ColumnCount = 6;
            this.tableLayoutPanelModuleScout.AutoSize = true;
            this.tableLayoutPanelModuleScout.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelModuleScout.Name = "tableLayoutPanelModuleScout";
            this.tableLayoutPanelModuleScout.MinimumSize = new Size(this.tabModuleScouts.Size.Width, this.tabModuleScouts.Size.Height);
            this.tableLayoutPanelModuleScout.MaximumSize = this.tabModuleScouts.Size;
            this.tableLayoutPanelModuleScout.TabIndex = 0;

            this.tabModuleScouts.Controls.Add(this.tableLayoutPanelModuleScout);

            // 
            // labelModuleScoutLatest
            // 
            this.labelModuleScoutLatest.Dock = System.Windows.Forms.DockStyle.None;
            this.labelModuleScoutLatest.AutoSize = false;
            this.labelModuleScoutLatest.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelModuleScoutLatest.Name = "labelModuleScoutLatest";
            this.labelModuleScoutLatest.TabIndex = 4;
            this.labelModuleScoutLatest.Text = "On FTP";
            this.labelModuleScoutLatest.Margin = new Padding(0);
            // 
            // labelModuleScoutYours
            // 
            this.labelModuleScoutYours.Dock = System.Windows.Forms.DockStyle.None;
            this.labelModuleScoutYours.AutoSize = false;
            this.labelModuleScoutYours.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelModuleScoutYours.Name = "labelModuleScoutYours";
            this.labelModuleScoutYours.TabIndex = 4;
            this.labelModuleScoutYours.Text = "Local";
            this.labelModuleScoutYours.Margin = new Padding(0);


            // add some of the non-dyamic components to the layout panel

            this.tableLayoutPanelModuleScout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelModuleScout.Controls.Add(this.labelModuleScoutLatest, 1, currentRow);
            this.tableLayoutPanelModuleScout.Controls.Add(this.labelModuleScoutYours, 2, currentRow++);


            // 
            // labelApps
            // 
            this.labelApps = new System.Windows.Forms.Label();
            this.labelApps.AutoSize = false;
            this.labelApps.Dock = System.Windows.Forms.DockStyle.None;
            this.labelApps.Name = "labelApps";
            this.labelApps.Font = new Font(FontFamily.GenericSansSerif, 12F, FontStyle.Bold);
            this.labelApps.TabIndex = 6;
            this.labelApps.Text = AppName.ToUpper();
            this.labelApps.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelApps.Margin = new Padding(0);

            // 
            // labelDrivers
            // 
            this.labelDrivers = new System.Windows.Forms.Label();
            this.labelDrivers.AutoSize = false;
            this.labelDrivers.Dock = System.Windows.Forms.DockStyle.None;
            this.labelDrivers.Name = "labelDrivers";
            this.labelDrivers.Font = new Font(FontFamily.GenericSansSerif, 12F, FontStyle.Bold);
            this.labelDrivers.TabIndex = 6;
            this.labelDrivers.Text = DriverName.ToUpper();
            this.labelDrivers.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelDrivers.Margin = new Padding(0);

            // 
            // labelScouts
            // 
            this.labelScouts = new System.Windows.Forms.Label();
            this.labelScouts.AutoSize = false;
            this.labelScouts.Dock = System.Windows.Forms.DockStyle.None;
            this.labelScouts.Name = "labelScouts";
            this.labelScouts.Font = new Font(FontFamily.GenericSansSerif, 12F, FontStyle.Bold);
            this.labelScouts.TabIndex = 6;
            this.labelScouts.Text = ScoutName.ToUpper();
            this.labelScouts.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelScouts.Margin = new Padding(0);

            // 
            // buttonModuleScoutRefresh
            // 
            this.buttonModuleScoutRefresh.AutoSize = false;
            this.buttonModuleScoutRefresh.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonModuleScoutRefresh.Name = "buttonModuleScoutRefresh";
            this.buttonModuleScoutRefresh.TabIndex = 1;
            this.buttonModuleScoutRefresh.Text = "Refresh";
            this.buttonModuleScoutRefresh.UseVisualStyleBackColor = true;
            this.buttonModuleScoutRefresh.Click += new System.EventHandler(this.buttonModuleScoutRefresh_Click);
            this.buttonModuleScoutRefresh.Margin = new Padding(0);

            // Apps label
            this.tableLayoutPanelModuleScout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelModuleScout.Controls.Add(this.labelApps, 0, currentRow++);

            // create the list of apps rows

            this.HubOtherUpdatePanelList = new List<HubOtherUpdatePanelRowItem>();
            for (int i = 0; i < this.binaryUpdateStatus[AppName].Count; ++i)
            {
                this.tableLayoutPanelModuleScout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
                HubOtherUpdatePanelRowItem hubOtherUpdatePanelRowItem = new HubOtherUpdatePanelRowItem(
                                            this.tableLayoutPanelModuleScout,
                                            currentRow++,
                                            this.binaryUpdateStatus[AppName][i].Item1,
                                            AppName,
                                            this.binaryUpdateStatus[AppName][i].Item3,
                                            this.binaryUpdateStatus[AppName][i].Item2,
                                            !this.binaryUpdateStatus[AppName][i].Item4 /* needs repo update == Item4*/,
                                            this.folderBrowserWorkingDir.SelectedPath + "\\binaries\\Pipeline\\AddIns",
                                            this.folderBrowserWorkingDir.SelectedPath + "\\HomeStore\\Repository",
                                            ftpHost,
                                            ftpPort,
                                            ftpUser,
                                            ftpPassword,
                                            this.logger                                            
                                            );
                this.HubOtherUpdatePanelList.Add(hubOtherUpdatePanelRowItem);
            }

            // Drivers label
            this.tableLayoutPanelModuleScout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelModuleScout.Controls.Add(this.labelDrivers, 0, currentRow++);

            // create the list of driver rows

            this.HubOtherUpdatePanelList = new List<HubOtherUpdatePanelRowItem>();
            for (int i = 0; i < this.binaryUpdateStatus[DriverName].Count; ++i)
            {
                this.tableLayoutPanelModuleScout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
                HubOtherUpdatePanelRowItem hubOtherUpdatePanelRowItem = new HubOtherUpdatePanelRowItem(
                                            this.tableLayoutPanelModuleScout,
                                            currentRow++,
                                            this.binaryUpdateStatus[DriverName][i].Item1,
                                            DriverName,
                                            this.binaryUpdateStatus[DriverName][i].Item3,
                                            this.binaryUpdateStatus[DriverName][i].Item2,
                                            !this.binaryUpdateStatus[DriverName][i].Item4 /* needs repo update == Item4*/,
                                            this.folderBrowserWorkingDir.SelectedPath + "\\binaries\\Pipeline\\AddIns",
                                            this.folderBrowserWorkingDir.SelectedPath + "\\HomeStore\\Repository",
                                            ftpHost,
                                            ftpPort,
                                            ftpUser,
                                            ftpPassword,
                                            this.logger
                                            );
                this.HubOtherUpdatePanelList.Add(hubOtherUpdatePanelRowItem);
            }

            // Scouts label
            this.tableLayoutPanelModuleScout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelModuleScout.Controls.Add(this.labelScouts, 0, currentRow++);

            // create the list of scout rows

            this.HubOtherUpdatePanelList = new List<HubOtherUpdatePanelRowItem>();
            for (int i = 0; i < this.binaryUpdateStatus[ScoutName].Count; ++i)
            {
                this.tableLayoutPanelModuleScout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
                HubOtherUpdatePanelRowItem hubOtherUpdatePanelRowItem = new HubOtherUpdatePanelRowItem(
                                            this.tableLayoutPanelModuleScout,
                                            currentRow++,
                                            this.binaryUpdateStatus[ScoutName][i].Item1,
                                            ScoutName,
                                            this.binaryUpdateStatus[ScoutName][i].Item3,
                                            this.binaryUpdateStatus[ScoutName][i].Item2,
                                            !this.binaryUpdateStatus[ScoutName][i].Item4 /* needs repo update == Item4*/,
                                            this.folderBrowserWorkingDir.SelectedPath + "\\binaries\\Scouts",
                                            this.folderBrowserWorkingDir.SelectedPath + "\\HomeStore\\Repository",
                                            ftpHost,
                                            ftpPort,
                                            ftpUser,
                                            ftpPassword,
                                            this.logger
                                            );
                this.HubOtherUpdatePanelList.Add(hubOtherUpdatePanelRowItem);
            }

            // add the Update button component add the bottom 

            this.tableLayoutPanelModuleScout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelModuleScout.Controls.Add(this.buttonModuleScoutRefresh, 0, currentRow++);

            Debug.Assert(currentRow == this.tableLayoutPanelModuleScout.RowCount);

            // resume layout 
            this.tabModuleScouts.ResumeLayout(false);
            this.tableLayoutPanelModuleScout.ResumeLayout(false);
            this.tableLayoutPanelModuleScout.PerformLayout();
        }
        
        
        
        private string GetLatestVersionInRep(
                            string binType,
                            string binaryName,
                            string remoteHost,
                            string remotePort,
                            string remoteUsername,
                            string remoteUserPassword
                            )
        {
            string homeOSBinaryVersion = PackagerHelper.BinaryPackagerHelper.UnknownHomeOSUpdateVersionValue;
            string uri = remoteHost.Replace("ftp","https") + ":" + "/" + binaryName.Replace(".", "/") + "/Latest/" + binaryName + ".dll.config";
            string localZipFile = ".\\" + binaryName + ".zip";
            string tmpFolder = ".\\" + binaryName + ".tmp";

            homeOSBinaryVersion = Utils.GetHomeOSUpdateVersion(uri);

     
            

        
            return homeOSBinaryVersion;
        }

        private Dictionary<string /*binary type*/,
                List<Tuple<string /*binary name*/,
                string /*version*/,
                string /*latestVersion*/,
                bool /*needs repo update*/>>> GenerateBinaryUpdateStatusList(string ftpHost, string ftpPort, string ftpUser, string ftpPassword)
        {
            Dictionary<string, List<Tuple<string, string, string, bool>>> binaryStatusList = new Dictionary<string, List<Tuple<string, string, string, bool>>>();
            Dictionary<string /*binaryName*/, string /*version*/> modules = Platform.Platform.GetAllModuleBinaries(this.folderBrowserWorkingDir.SelectedPath);
            Dictionary<string /*scoutName*/, string /*version*/> scouts = Platform.Platform.GetAllScoutBinaries(this.folderBrowserWorkingDir.SelectedPath);
            // let's get apps and drivers
            binaryStatusList.Add(AppName, new List<Tuple<string, string, string, bool>>());
            binaryStatusList.Add(DriverName, new List<Tuple<string, string, string, bool>>());
            foreach (string moduleName in modules.Keys)
            {
                if (moduleName.ToLower().Contains(AppName))
                {
                    binaryStatusList[AppName].Add(new Tuple<string, string, string, bool>(moduleName, modules[moduleName],
                                                    GetLatestVersionInRep(AppName, moduleName, ftpHost, ftpPort, ftpUser, ftpPassword),
                                                    NeedsRepoUpdate(AppName, moduleName, modules[moduleName])));
                }
                else if (moduleName.ToLower().Contains(DriverName))
                {
                    binaryStatusList[DriverName].Add(new Tuple<string, string, string, bool>(moduleName, modules[moduleName],
                                                    GetLatestVersionInRep(DriverName, moduleName, ftpHost, ftpPort, ftpUser, ftpPassword),
                                                    NeedsRepoUpdate(DriverName,moduleName, modules[moduleName])));
                }
            }

            // let's get the scouts next
            binaryStatusList.Add(ScoutName, new List<Tuple<string, string, string, bool>>());
            foreach (string scoutName in scouts.Keys)
            {
                binaryStatusList[ScoutName].Add(new Tuple<string, string, string, bool>(scoutName, scouts[scoutName],
                                                    GetLatestVersionInRep(ScoutName, scoutName, ftpHost, ftpPort, ftpUser, ftpPassword),
                                                    NeedsRepoUpdate(ScoutName, scoutName, scouts[scoutName])));
            }

            return binaryStatusList;
        }


        private bool PackageModule(string addInRoot, string repoDir, string moduleName, string binType, List<BinaryPackageItem> binPackagelist)
        {
            Collection<AddInToken> tokens = Utils.GetAddInTokens(addInRoot, moduleName);

            bool packagedSomething = false;

            foreach (AddInToken token in tokens)
            {
                if (string.IsNullOrWhiteSpace(moduleName) ||
                    token.Name.Equals(moduleName))
                {
                    string[] filePaths = new string[0];
                    PackagerHelper.BinaryPackagerHelper.Package(addInRoot + "\\AddIns", token.Name, false /*singleBin*/, "dll", "module", repoDir, ref filePaths);
                    packagedSomething = true;
                    if (null != binPackagelist)
                    {
                        binPackagelist.Add(new BinaryPackageItem(binType, filePaths));
                    }

                }
            }

            return packagedSomething;
        }

        private bool PackageScout(string ScoutsRootDir, string repoDir, string scoutName, List<BinaryPackageItem> binPackagelist)
        {
            //get the scouts
            List<string> scoutsList = GetScouts(ScoutsRootDir, scoutName);

            bool packagedSomething = false;

            foreach (string scout in scoutsList)
            {
                if (string.IsNullOrWhiteSpace(scoutName) ||
                    scout.Equals(scoutName))
                {
                    string[] filePaths = new string[0];
                    PackagerHelper.BinaryPackagerHelper.Package(ScoutsRootDir, scout, false /* singleBin */, "dll", "scout", repoDir, ref filePaths);
                    packagedSomething = true;
                    if (null != binPackagelist)
                    {
                        binPackagelist.Add(new BinaryPackageItem(ScoutName, filePaths));
                    }
                }
            }

            if (!packagedSomething)
            {
                logger.Error("I did not package anything. Did you supply the correct ScoutsRootDir ({0})?", ScoutsRootDir);
                if (!string.IsNullOrWhiteSpace(scoutName))
                    logger.Error("Is there a views dll in the output directory of {0}", scoutName);
            }

            return packagedSomething;
        }

        private static List<string> GetScouts(string ScoutsRootDir, string scoutName)
        {

            // Search for add-ins of type VModule
            List<string> scoutsFullPath = Directory.GetDirectories(ScoutsRootDir).ToList();
            List<string> scoutNames = new List<string>();
            foreach (string scoutPath in scoutsFullPath)
            {
                scoutNames.Add(Path.GetFileName(scoutPath));
            }

            return scoutNames;
        }

        private List<BinaryPackageItem> PackageBinaries()
        {
            List<BinaryPackageItem> binaryPackageList = new List<BinaryPackageItem>();
            for (int k = 0; k < binaryUpdateStatus.Keys.Count; ++k)
            {
                string binType = binaryUpdateStatus.Keys.ElementAt(k);
                var tupleList = binaryUpdateStatus[binType];
                for (int i = 0; i < tupleList.Count; ++i)
                {
                    // only package if an update to the repo is needed
                    if (!tupleList[i].Item4 /* needs repo update flag */)
                    {
                        continue;
                    }
                    if (binType != ScoutName)
                    {
                        PackageModule(folderBrowserWorkingDir.SelectedPath + "\\binaries\\pipeline",
                                      folderBrowserWorkingDir.SelectedPath + "\\HomeStore\\Repository",
                                      tupleList[i].Item1, binType, binaryPackageList);
                    }
                    else
                    {
                        PackageScout(folderBrowserWorkingDir.SelectedPath + "\\binaries\\Scouts",
                                     folderBrowserWorkingDir.SelectedPath + "\\HomeStore\\Repository",
                                     tupleList[i].Item1, binaryPackageList);
                    }
                }
            }
            return binaryPackageList;
        }


        private void buttonModuleScoutRefresh_Click(object sender, EventArgs e)
        {
            buttonModuleScoutRefresh.Enabled = false;
            OnShowingModuleScoutTab();
            buttonModuleScoutRefresh.Enabled = true;
        }

    }
}
