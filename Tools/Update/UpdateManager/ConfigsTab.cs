using HomeOS.Hub.Common;
using HomeOS.Hub.Tools.UpdateHelper;
using System;
using System.Collections.Generic;
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
        public partial class ValidationStatusForm : Form
        {
            private System.Windows.Forms.Label labelValidationStatus;
            private System.Windows.Forms.Button buttonOK;


            public ValidationStatusForm(List<string> statusLines)
            {
                this.labelValidationStatus = new System.Windows.Forms.Label();

                // 
                this.labelValidationStatus.Dock = System.Windows.Forms.DockStyle.Fill;
                this.labelValidationStatus.AutoSize = true;
                this.labelValidationStatus.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                this.labelValidationStatus.Name = "labelValidationStatus";
                this.labelValidationStatus.TabIndex = 9;
                foreach (string sl in statusLines)
                {
                    this.labelValidationStatus.Text += sl;
                }
                this.labelValidationStatus.TextAlign = ContentAlignment.TopLeft;
                this.labelValidationStatus.Margin = new Padding(0);

                this.buttonOK = new System.Windows.Forms.Button();
                this.buttonOK.AutoSize = true;
                this.buttonOK.Dock = System.Windows.Forms.DockStyle.Bottom;
                this.buttonOK.Name = "buttonOK";
                this.buttonOK.Text = "OK";
                this.buttonOK.UseVisualStyleBackColor = true;
                this.buttonOK.Margin = new Padding(0);
                this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.buttonOK.Click += buttonOK_Click;

                this.Controls.Add(this.labelValidationStatus);
                this.Controls.Add(this.buttonOK);

                this.AutoSize = true;

            }

            void buttonOK_Click(object sender, EventArgs e)
            {
            }

        }

        public const string AppName = "apps";
        public const string DriverName = "drivers";
        public const string ScoutName = "scouts";
        public const string BinaryStatus_OK = "OK";
        public const string BinaryStatus_Repo_Update = "Repo Update";
        public bool EnableValidation = false;

        protected AzureHubConfigDataLocalCache configDataCache;
        protected List<HubConfigUpdatePanelRowItem> HubConfigUpdatePanelList;

        protected Dictionary<string /*app, driver, scout*/, List<Tuple<string /*binary name*/,
                                                                      object /*ModuleInfo or ScoutInfo*/,
                                                                      bool /* needs repo update*/>>> configUpdateStatus;

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelConfigs;
        private System.Windows.Forms.Button btnConfigsRefresh;
        private System.Windows.Forms.Button btnConfigsValidate;
        private System.Windows.Forms.Button btnConfigsUpdate;

        private System.Windows.Forms.ComboBox comboBoxConfigsOrgID;
        private System.Windows.Forms.Label labelConfigsOrgID;
        private System.Windows.Forms.Label labelConfigsStudyID;
        private System.Windows.Forms.ComboBox comboBoxConfigsStudyID;
        private System.Windows.Forms.Label labelConfigsHomeId;

        private System.Windows.Forms.CheckBox checkBoxConfigsAll;
        private System.Windows.Forms.Label labelConfigsWhatsNew;


        // ux state
        string currentOrgItem;
        string currentStudyItem;
        bool dontProcessItemSelectionChange;

        private void BuildCacheAndLoadConfigComponents(int selectedOrgIndex, int selectedStudyIndex)
        {
            if (null != this.configDataCache)
            {
                this.configDataCache.Dispose();
            }
            this.configDataCache = new AzureHubConfigDataLocalCache(this.formAzureAccount.AzureAccountName, this.formAzureAccount.AzureAccountKey, logger);
            this.configDataCache.BuildCache();

            if (this.configDataCache.GetOrgIdList().Count > 0)
            {
                string orgId = this.configDataCache.GetOrgIdList()[0];
                if (this.configDataCache.GetStudyIdListForOrg(orgId).Count > 0)
                {
                    LoadConfigsTabComponents(selectedOrgIndex, selectedStudyIndex);
                }
            }

        }
        private void OnShowingConfigsTab()
        {
            outputBox.Text += "Loading configuration components\r\n";
            BuildCacheAndLoadConfigComponents(0, 0);
            outputBox.Text += "Finished loading configuraiton components \r\n";
        }

        private void LoadConfigsTabComponents(int selectedOrgIndex, int selectedStudyIndex)
        {
            int currentRow = 0;
            int hubCount = 0;
            string orgId = null;
            string studyId = null;

            // remove existing tab contents
            if (null != this.tableLayoutPanelConfigs)
            {
                this.tabConfigs.Controls.Remove(this.tableLayoutPanelConfigs);
            }

            // create the components that are not dynamic
            this.tableLayoutPanelConfigs = new System.Windows.Forms.TableLayoutPanel();
            this.labelConfigsOrgID = new System.Windows.Forms.Label();
            this.comboBoxConfigsOrgID = new System.Windows.Forms.ComboBox();
            this.labelConfigsStudyID = new System.Windows.Forms.Label();
            this.comboBoxConfigsStudyID = new System.Windows.Forms.ComboBox();
            this.labelConfigsHomeId = new System.Windows.Forms.Label();
            this.labelConfigsWhatsNew = new System.Windows.Forms.Label();
            this.checkBoxConfigsAll = new System.Windows.Forms.CheckBox();
            this.btnConfigsRefresh = new System.Windows.Forms.Button();
            if (EnableValidation)
            {
                this.btnConfigsValidate = new System.Windows.Forms.Button();
            }
            this.btnConfigsUpdate = new System.Windows.Forms.Button();

            // suspend all layout
            tabConfigs.SuspendLayout();
            this.tableLayoutPanelConfigs.SuspendLayout();

            // calc the total number of rows = # of hubs + 4
            if (this.configDataCache.GetOrgIdList().Count > 0)
            {
                orgId = this.configDataCache.GetOrgIdList()[selectedOrgIndex];
                if (this.configDataCache.GetStudyIdListForOrg(orgId).Count > 0)
                {
                    studyId = this.configDataCache.GetStudyIdListForOrg(orgId)[selectedStudyIndex];
                    hubCount = this.configDataCache.GetHubIdListForOrgStudyId(orgId, studyId).Count;
                }
            }

            this.tableLayoutPanelConfigs.RowCount = hubCount + 4;

            // initialize the non-dynamic components

            // 
            // tableLayoutPanelConfigs
            // 
            this.tableLayoutPanelConfigs.AutoScroll = true;
            this.tableLayoutPanelConfigs.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            this.tableLayoutPanelConfigs.ColumnCount = 6;
            this.tableLayoutPanelConfigs.AutoSize = true;
            this.tableLayoutPanelConfigs.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelConfigs.Name = "tableLayoutPanelConfigs";
            this.tableLayoutPanelConfigs.MinimumSize = new Size(tabConfigs.Size.Width, tabConfigs.Size.Height);
            this.tableLayoutPanelConfigs.MaximumSize = tabConfigs.Size;
            this.tableLayoutPanelConfigs.TabIndex = 0;

            tabConfigs.Controls.Add(this.tableLayoutPanelConfigs);

            // 
            // labelConfigsOrgID
            // 
            this.labelConfigsOrgID.AutoSize = false;
            this.labelConfigsOrgID.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelConfigsOrgID.Name = "labelConfigsOrgID";
            this.labelConfigsOrgID.TabIndex = 1;
            this.labelConfigsOrgID.Text = "Org ID";
            this.labelConfigsOrgID.Margin = new Padding(0);
            // 
            // comboBoxConfigsOrgID
            // 
            this.tableLayoutPanelConfigs.SetColumnSpan(this.comboBoxConfigsOrgID, 2);
            this.comboBoxConfigsOrgID.AutoSize = false;
            this.comboBoxConfigsOrgID.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxConfigsOrgID.FormattingEnabled = true;
            this.comboBoxConfigsOrgID.Name = "comboBoxConfigsOrgID";
            this.comboBoxConfigsOrgID.TabIndex = 0;
            this.comboBoxConfigsOrgID.Margin = new Padding(0);
            this.comboBoxConfigsOrgID.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBoxConfigsOrgID.DataSource = this.configDataCache.GetOrgIdList();
            this.comboBoxConfigsOrgID.SelectedIndexChanged += new System.EventHandler(this.comboBoxConfigsOrgID_SelectedIndexChanged);
            // 
            // labelConfigsStudyID
            // 
            this.labelConfigsStudyID.AutoSize = false;
            this.labelConfigsStudyID.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelConfigsStudyID.Name = "labelConfigsStudyID";
            this.labelConfigsStudyID.TabIndex = 3;
            this.labelConfigsStudyID.Text = "Study ID";
            this.labelConfigsStudyID.Margin = new Padding(0);

            // 
            // comboBoxConfigsStudyID
            // 
            this.tableLayoutPanelConfigs.SetColumnSpan(this.comboBoxConfigsStudyID, 2);
            this.comboBoxConfigsStudyID.AutoSize = false;
            this.comboBoxConfigsStudyID.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxConfigsStudyID.FormattingEnabled = true;
            this.comboBoxConfigsStudyID.Name = "comboBoxConfigsStudyID";
            this.comboBoxConfigsStudyID.TabIndex = 2;
            this.comboBoxConfigsStudyID.DropDownStyle = ComboBoxStyle.DropDownList;
            string selectedOrg = this.configDataCache.GetOrgIdList()[selectedOrgIndex];
            this.comboBoxConfigsStudyID.DataSource = this.configDataCache.GetStudyIdListForOrg(selectedOrg);
            this.comboBoxConfigsStudyID.SelectedIndexChanged += new System.EventHandler(this.comboBoxConfigsStudyID_SelectedIndexChanged);
            this.comboBoxConfigsStudyID.Margin = new Padding(0);
            // labelConfigsHomeId
            // 
            this.labelConfigsHomeId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelConfigsHomeId.AutoSize = false;
            this.labelConfigsHomeId.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelConfigsHomeId.Name = "labelConfigsHomeId";
            this.labelConfigsHomeId.TabIndex = 4;
            this.labelConfigsHomeId.Text = "Home ID";
            this.labelConfigsHomeId.Margin = new Padding(0);
            // 
            // checkBoxConfigsAll
            // 
            this.checkBoxConfigsAll.AutoSize = false;
            this.checkBoxConfigsAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConfigsAll.Name = "checkBoxConfigsAll";
            this.checkBoxConfigsAll.TabIndex = 10;
            this.checkBoxConfigsAll.Text = "All";
            this.checkBoxConfigsAll.UseVisualStyleBackColor = true;
            this.checkBoxConfigsAll.CheckedChanged += new System.EventHandler(this.checkBoxConfigsAll_CheckedChanged);
            this.checkBoxConfigsAll.Margin = new Padding(0);
            // 
            // labelConfigsWhatsNew
            // 
            this.labelConfigsWhatsNew.AutoSize = false;
            this.labelConfigsWhatsNew.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelConfigsWhatsNew.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelConfigsWhatsNew.Name = "labelConfigsWhatsNew";
            this.labelConfigsWhatsNew.TabIndex = 11;
            this.labelConfigsWhatsNew.Text = "What has changed?";
            this.labelConfigsWhatsNew.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelConfigsWhatsNew.Margin = new Padding(0);
            // 
            // btnConfigsRefresh
            // 
            this.btnConfigsRefresh.AutoSize = false;
            this.btnConfigsRefresh.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnConfigsRefresh.Name = "btnConfigsRefresh";
            this.btnConfigsRefresh.TabIndex = 1;
            this.btnConfigsRefresh.Text = "Refresh ";
            this.btnConfigsRefresh.UseVisualStyleBackColor = true;
            this.btnConfigsRefresh.Click += buttonConfigsRefresh_Click;
            this.btnConfigsRefresh.Margin = new Padding(0);
            // 
            // btnConfigsValidate
            // 
            if (EnableValidation)
            {
                this.btnConfigsValidate.AutoSize = false;
                this.btnConfigsValidate.Dock = System.Windows.Forms.DockStyle.Bottom;
                this.btnConfigsValidate.Name = "btnConfigsValidate";
                this.btnConfigsValidate.TabIndex = 8;
                this.btnConfigsValidate.Text = "Validate";
                this.btnConfigsValidate.UseVisualStyleBackColor = true;
                this.btnConfigsValidate.Click += new System.EventHandler(this.btnSetupValidate_Click);
                this.btnConfigsValidate.Margin = new Padding(0);
            }
            // 
            // btnConfigsUpdate
            // 
            this.btnConfigsUpdate.Enabled = false; // get's enabled when there is atleast one hub selected
            this.btnConfigsUpdate.AutoSize = false;
            this.btnConfigsUpdate.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnConfigsUpdate.Name = "btnConfigsUpdate";
            this.btnConfigsUpdate.TabIndex = 1;
            this.btnConfigsUpdate.Text = "Update";
            this.btnConfigsUpdate.UseVisualStyleBackColor = true;
            this.btnConfigsUpdate.Click += new System.EventHandler(this.buttonConfigsUpdate_Click);
            this.btnConfigsUpdate.Margin = new Padding(0);


            // add some of the non-dyamic components to the layout panel

            this.tableLayoutPanelConfigs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelConfigs.Controls.Add(this.labelConfigsOrgID, 0, currentRow);
            this.tableLayoutPanelConfigs.Controls.Add(this.comboBoxConfigsOrgID, 1, currentRow++);
            this.tableLayoutPanelConfigs.SetColumnSpan(this.comboBoxConfigsOrgID, 2);

            this.tableLayoutPanelConfigs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelConfigs.Controls.Add(this.labelConfigsStudyID, 0, currentRow);
            this.tableLayoutPanelConfigs.Controls.Add(this.comboBoxConfigsStudyID, 1, currentRow++);
            this.tableLayoutPanelConfigs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelConfigs.SetColumnSpan(this.comboBoxConfigsStudyID, 2);
            this.tableLayoutPanelConfigs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));

            // add some more of the non dynamic components of the layout

            this.tableLayoutPanelConfigs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelConfigs.Controls.Add(this.labelConfigsHomeId, 0, currentRow++);
            this.tableLayoutPanelConfigs.SetColumnSpan(this.labelConfigsHomeId, 6);

            this.tableLayoutPanelConfigs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            this.tableLayoutPanelConfigs.Controls.Add(this.checkBoxConfigsAll, 0, currentRow);
            this.tableLayoutPanelConfigs.Controls.Add(this.labelConfigsWhatsNew, 3, currentRow++);
            this.tableLayoutPanelConfigs.SetColumnSpan(this.labelConfigsWhatsNew, 3);


            // create and initialize the dynamic components - Other update rows

            this.HubConfigUpdatePanelList = new List<HubConfigUpdatePanelRowItem>();
            for (int i = 0; i < hubCount; ++i)
            {
                this.tableLayoutPanelConfigs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
                string hubId = this.configDataCache.GetHubIdListForOrgStudyId(orgId, studyId)[i];
                HubConfigUpdatePanelRowItem hubConfigsUpdatePanelRowItem = new HubConfigUpdatePanelRowItem(
                                            this.tableLayoutPanelConfigs,
                                            this.configDataCache.GetDesiredConfigLocalFolderPath(orgId, studyId, hubId),
                                            this.configDataCache.GetActualConfigFolderPath(orgId, studyId, hubId),
                                            currentRow++,
                                            hubId,
                                            this.logger
                                            );
                hubConfigsUpdatePanelRowItem.HubSelectionChange += hubConfigsUpdatePanelRowItem_HubSelectionChange;
                this.HubConfigUpdatePanelList.Add(hubConfigsUpdatePanelRowItem);
            }

            // add the Refresh, Validate and Update button component add the bottom 

            this.tableLayoutPanelConfigs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));
            int col = 0;
            this.tableLayoutPanelConfigs.Controls.Add(this.btnConfigsRefresh, col++, currentRow);
            if (EnableValidation)
            {
                this.tableLayoutPanelConfigs.Controls.Add(this.btnConfigsValidate, col++, currentRow);
            }
            this.tableLayoutPanelConfigs.Controls.Add(this.btnConfigsUpdate, col, currentRow);

            Debug.Assert(currentRow == this.tableLayoutPanelConfigs.RowCount);

            // resume layout 
            tabConfigs.ResumeLayout(false);
            this.tableLayoutPanelConfigs.ResumeLayout(false);
            this.tableLayoutPanelConfigs.PerformLayout();

            // set back the index selection for the org and study since we have rebuilt all the components for this tab
            this.dontProcessItemSelectionChange = true;
            if (null == this.currentOrgItem)
            {
                this.currentOrgItem = this.configDataCache.GetOrgIdList()[selectedOrgIndex];
            }
            if (null == this.currentStudyItem && null != this.currentOrgItem)
            {
                this.currentStudyItem = this.configDataCache.GetStudyIdListForOrg(this.currentOrgItem)[selectedStudyIndex];
            }

            if (null != this.currentOrgItem)
            {
                this.comboBoxConfigsOrgID.SelectedItem = this.currentOrgItem;
            }
            if (null != this.currentStudyItem)
            {
                this.comboBoxConfigsStudyID.SelectedItem = this.currentStudyItem;
            }
            this.dontProcessItemSelectionChange = false; ;

        }

        private new void Refresh()
        {
            btnConfigsRefresh.Enabled = false;
            outputBox.Text += "Refreshing Config information\r\n";
            int currentOrgIndex = this.comboBoxConfigsOrgID.SelectedIndex;
            int currentStudyIndex = GetIndexOfStudyId((string)this.comboBoxConfigsOrgID.SelectedItem, (string)this.comboBoxConfigsStudyID.SelectedItem);
            this.currentOrgItem = (string)this.comboBoxConfigsOrgID.SelectedItem;
            this.currentStudyItem = (string)this.comboBoxConfigsStudyID.SelectedItem;
            BuildCacheAndLoadConfigComponents(currentOrgIndex, currentStudyIndex);
            outputBox.Text += "Done refreshing Config information\r\n";
            btnConfigsRefresh.Enabled = true;
        }

        void buttonConfigsRefresh_Click(object sender, EventArgs e)
        {
            Refresh();
        }

        void hubConfigsUpdatePanelRowItem_HubSelectionChange(object sender, EventArgs e)
        {
            bool hubSelected = false;
            for (int i = 0; i < HubConfigUpdatePanelList.Count; ++i)
            {
                HubConfigUpdatePanelRowItem panelRowItem = this.HubConfigUpdatePanelList[i];
                if (panelRowItem.IsChecked())
                {
                    hubSelected = true;
                    break;
                }
            }

            this.btnConfigsUpdate.Enabled = hubSelected;
        }

        private Dictionary<string /*binary type*/,
                List<Tuple<string /*binary name*/,
                object /*ModuleInfo or ScoutInfo*/,
                bool /*needs repo update*/>>> GenerateConfigUpdateStatusList()
        {
            Dictionary<string, List<Tuple<string, object, bool>>> configStatusList = new Dictionary<string, List<Tuple<string, object, bool>>>();
            Dictionary<string, ModuleInfo> modules = Platform.Platform.GetConfigModules(this.folderBrowserWorkingDir.SelectedPath + "\\Configs" + "\\Config");
            List<Platform.DeviceScout.ScoutInfo> scouts = Platform.Platform.GetConfigScouts(this.folderBrowserWorkingDir.SelectedPath + "\\Configs" + "\\Config");

            // let's get apps and drivers
            configStatusList.Add(AppName, new List<Tuple<string, object, bool>>());
            configStatusList.Add(DriverName, new List<Tuple<string, object, bool>>());
            foreach (string moduleName in modules.Keys)
            {
                if (modules[moduleName].BinaryName().ToLower().Contains(AppName))
                {
                    configStatusList[AppName].Add(new Tuple<string, object, bool>(moduleName, modules[moduleName], NeedsRepoUpdate(AppName, modules[moduleName].BinaryName(), modules[moduleName].GetDesiredVersion())));
                }
                else if (modules[moduleName].BinaryName().ToLower().Contains(DriverName))
                {
                    configStatusList[DriverName].Add(new Tuple<string, object, bool>(moduleName, modules[moduleName], NeedsRepoUpdate(DriverName, modules[moduleName].BinaryName(), modules[moduleName].GetDesiredVersion())));
                }
            }

            // let's get the scouts next
            configStatusList.Add(ScoutName, new List<Tuple<string, object, bool>>());
            foreach (Platform.DeviceScout.ScoutInfo scoutInfo in scouts)
            {
                configStatusList[ScoutName].Add(new Tuple<string, object, bool>(scoutInfo.Name, scoutInfo, NeedsRepoUpdate(ScoutName, scoutInfo.DllName, scoutInfo.DesiredVersion)));
            }

            return configStatusList;
        }

        private bool ContainsInvalidStatus(List<string> displayLines)
        {
            foreach (string dl in displayLines)
            {
                if (dl.Contains(BinaryStatus_Repo_Update))
                {
                    return true;
                }
            }
            return false;
        }

        private bool RunValidation(bool showUI = true)
        {
            bool valid = false;
            List<string> displayLines = new List<string>();
            displayLines.Add(string.Format("{0,-10} {1,-20} {2,-40} {3,-15} {4,-15}\n", "Type", "Name", "Binary", "Version", "Status"));
            if (null != configUpdateStatus)
            {
                this.configUpdateStatus.Clear();
            }
            this.configUpdateStatus = GenerateConfigUpdateStatusList();

            for (int k = 0; k < configUpdateStatus.Keys.Count; ++k)
            {
                string binType = configUpdateStatus.Keys.ElementAt(k);
                var tupleList = configUpdateStatus[binType];
                for (int i = 0; i < tupleList.Count; ++i)
                {
                    if (binType != ScoutName)
                    {
                        ModuleInfo mi = tupleList[i].Item2 as ModuleInfo;
                        displayLines.Add(string.Format("{0,-10} {1,-20} {2,-40} {3,-15} {4,-15}\n",
                            binType, tupleList[i].Item1, mi.BinaryName(), mi.GetDesiredVersion() == null ? "na" : mi.GetDesiredVersion(), tupleList[i].Item3 ? BinaryStatus_Repo_Update : BinaryStatus_OK));
                    }
                    else
                    {
                        Platform.DeviceScout.ScoutInfo si = tupleList[i].Item2 as Platform.DeviceScout.ScoutInfo;
                        displayLines.Add(string.Format("{0,-10} {1,-20} {2,-40} {3,-15} {4,-15}\n",
                            binType, tupleList[i].Item1, si.DllName, si.DesiredVersion == null ? "na" : si.DesiredVersion, tupleList[i].Item3 ? BinaryStatus_Repo_Update : BinaryStatus_OK));
                    }
                }
            }

            if (displayLines.Count > 1)
            {
                if (showUI)
                {
                    ValidationStatusForm validationStatusForm = new ValidationStatusForm(displayLines);
                    if (validationStatusForm.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                    }
                }

                valid = !ContainsInvalidStatus(displayLines);
            }

            return valid;
        }

        private void btnSetupValidate_Click(object sender, EventArgs e)
        {
            RunValidation(true /*showUI*/);
        }



        private void comboBoxConfigsOrgID_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dontProcessItemSelectionChange)
                return;
            if (this.configDataCache.GetOrgIdList().Count > 0)
            {
                this.comboBoxConfigsStudyID.DataSource = this.configDataCache.GetStudyIdListForOrg((string)this.comboBoxConfigsOrgID.SelectedItem);
            }
        }

        private int GetIndexOfStudyId(string orgId, string studyId)
        {
            List<string> studyIdList = this.configDataCache.GetStudyIdListForOrg(orgId);
            return studyIdList.FindIndex(s => s == studyId);
        }

        private void comboBoxConfigsStudyID_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dontProcessItemSelectionChange)
                return;

            Refresh();
        }

        private void checkBoxConfigsAll_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < HubConfigUpdatePanelList.Count; ++i)
            {
                HubConfigUpdatePanelRowItem panelRowItem = this.HubConfigUpdatePanelList[i];
                if (this.checkBoxConfigsAll.Checked)
                {
                    panelRowItem.Check();
                }
                else
                {
                    panelRowItem.UnCheck();
                }
            }
        }

        private void UpdateConfigsForSelectedHubs()
        {
            for (int i = 0; i < HubConfigUpdatePanelList.Count; ++i)
            {
                HubConfigUpdatePanelRowItem panelRowItem = this.HubConfigUpdatePanelList[i];
                if (panelRowItem.IsChecked())
                {
                    string zipPath_desired = this.configDataCache.GetDesiredConfigLocalFolderPath(
                                                                            (string)this.currentOrgItem,
                                                                            (string)this.currentStudyItem,
                                                                            panelRowItem.hubId);

                    string zipPath_actual = this.configDataCache.GetActualConfigFolderPath((string)this.currentOrgItem, (string)this.currentStudyItem, panelRowItem.hubId);

                    string currentVersionFilename = zipPath_actual + "\\" + PackagerHelper.ConfigPackagerHelper.CurrentVersionFileName;
                    string parentVersionFilename = zipPath_desired + "\\" + PackagerHelper.ConfigPackagerHelper.ParentVersionFileName;
                    if (File.Exists(currentVersionFilename))
                    {
                        string s = string.Format("Copying {0} to {1} ", currentVersionFilename, parentVersionFilename);
                        logger.Info(s, 1);
                        outputBox.Text += s + "\n";
                        PackagerHelper.PackagerHelper.CopyFile(currentVersionFilename, parentVersionFilename);
                    }
                    else
                    {
                        string s1 = string.Format("Writing version of config in {0} to {1} ", zipPath_actual, parentVersionFilename);
                        logger.Info(s1, 1);
                        outputBox.Text += s1 + "\n";
                        PackagerHelper.ConfigPackagerHelper.UpdateVersionFile(PackagerHelper.ConfigPackagerHelper.GetConfigVersion(zipPath_actual), parentVersionFilename);
                    }

                    string currentVersionFilenameDesired = zipPath_desired + "\\" + PackagerHelper.ConfigPackagerHelper.CurrentVersionFileName;
                    string s3 = string.Format("Writing version of config in {0} to {1} ", zipPath_desired, currentVersionFilenameDesired);
                    logger.Info(s3, 1);
                    outputBox.Text += s3 + "\n";
                    Dictionary<string, string> currentVersion_desired = PackagerHelper.ConfigPackagerHelper.GetConfigVersion(zipPath_desired);
                    PackagerHelper.ConfigPackagerHelper.UpdateVersionFile(currentVersion_desired, currentVersionFilenameDesired);

                    string zipFilePathDesiredLocal = this.configDataCache.GetDesiredConfigLocalZipFilePath((string)this.currentOrgItem, (string)this.currentStudyItem, panelRowItem.hubId);
                    File.Delete(zipFilePathDesiredLocal);
                    PackagerHelper.PackagerHelper.PackZip(zipPath_desired, zipFilePathDesiredLocal);

                    string s4 = string.Format("Uploading desired config for homeID {0} ", panelRowItem.hubId);
                    logger.Info(s4, 1);
                    outputBox.Text += s4 + "\n";

                    if (!AzureBlobConfigUpdate.UploadConfig(
                            zipFilePathDesiredLocal,
                            this.formAzureAccount.AzureAccountName,
                            this.formAzureAccount.AzureAccountKey,
                            (string)this.currentOrgItem,
                            (string)this.currentStudyItem,
                            panelRowItem.hubId,
                            PackagerHelper.ConfigPackagerHelper.desiredConfigFileName,
                            logger))
                    {
                        string s5 = string.Format("WARNING! unable to upload config for homeID: " + panelRowItem.hubId);
                        logger.Warn(s5);
                        outputBox.Text += s5 + "\n";


                    }
                }
            }
        }

        private void buttonConfigsUpdate_Click(object sender, EventArgs e)
        {
            // 1. Validate the configuration
            // running the validation rebuilds the binary status
            if (EnableValidation)
            {
                if (!RunValidation(false))
                {
                    this.logger.Error("Validation Failed!");
                    outputBox.Text += "Validation Failed!\n";
                    return;
                }
            }
            else
            {
                if (null != this.configUpdateStatus)
                {
                    this.configUpdateStatus.Clear();
                }
                this.configUpdateStatus = GenerateConfigUpdateStatusList();
            }

            // 2. Update the Configuration for the specified hubs
            UpdateConfigsForSelectedHubs();
        }

    }
}
