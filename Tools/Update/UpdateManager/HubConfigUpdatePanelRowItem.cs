using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Tools.UpdateManager
{
    public class HubConfigUpdatePanelRowItem
    {
        private System.Windows.Forms.CheckBox checkBoxConfigsHubId;
        private System.Windows.Forms.Button buttonConfigsUpdateActualConfigs;
        private System.Windows.Forms.Button buttonConfigsUpdateDesiredConfigs;

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelConfigDiffLinks;

        private FileSystemWatcher watcher;

        private int rowNumber;
        private string localConfigFolder;
        private string actualConfigFolder;
        private string[] configDiffsDisplayed;

        public string hubId { get; private set; }

        public event EventHandler HubSelectionChange;
        private NLog.Logger logger;

        public HubConfigUpdatePanelRowItem(System.Windows.Forms.TableLayoutPanel tableLayoutPanel, string localConfigFolder, string actualConfigFolder, int rowNumber, string hubId, NLog.Logger logger)
        {
            this.logger = logger;
            this.tableLayoutPanel = tableLayoutPanel;
            this.rowNumber = rowNumber;
            this.localConfigFolder = localConfigFolder;
            this.actualConfigFolder = actualConfigFolder;
            this.configDiffsDisplayed = new string[0];
            this.hubId = hubId;

            // 
            // checkBoxConfigsHubId
            // 
            this.checkBoxConfigsHubId = new System.Windows.Forms.CheckBox();
            this.checkBoxConfigsHubId.AutoSize = true;
            this.checkBoxConfigsHubId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConfigsHubId.Name = "checkBoxConfigsHubId" + this.rowNumber.ToString();
            this.checkBoxConfigsHubId.TabIndex = 12;
            this.checkBoxConfigsHubId.Text = hubId;
            this.checkBoxConfigsHubId.UseVisualStyleBackColor = true;
            this.checkBoxConfigsHubId.Margin = new Padding(0);
            this.checkBoxConfigsHubId.CheckedChanged += new System.EventHandler(this.checkBoxConfigsHubId_CheckedChanged);
            // 
            // buttonConfigsUpdateActualConfigs
            // 
            this.buttonConfigsUpdateActualConfigs = new System.Windows.Forms.Button();
            this.buttonConfigsUpdateActualConfigs.AutoSize = false;
            this.buttonConfigsUpdateActualConfigs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonConfigsUpdateActualConfigs.Name = "buttonConfigsUpdateActualConfigs" + this.rowNumber.ToString();
            this.buttonConfigsUpdateActualConfigs.TabIndex = 13;
            this.buttonConfigsUpdateActualConfigs.Text = "Actual";
            this.buttonConfigsUpdateActualConfigs.UseVisualStyleBackColor = true;
            this.buttonConfigsUpdateActualConfigs.Margin = new Padding(0);
            this.buttonConfigsUpdateActualConfigs.Click += new System.EventHandler(this.buttonConfigsUpdateActualConfigs_Click);
            // 
            // buttonConfigsUpdateDesiredConfigs
            // 
            this.buttonConfigsUpdateDesiredConfigs = new System.Windows.Forms.Button();
            this.buttonConfigsUpdateDesiredConfigs.AutoSize = false;
            this.buttonConfigsUpdateDesiredConfigs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonConfigsUpdateDesiredConfigs.Name = "buttonConfigsUpdateDesiredConfigs" + this.rowNumber.ToString();
            this.buttonConfigsUpdateDesiredConfigs.TabIndex = 14;
            this.buttonConfigsUpdateDesiredConfigs.Text = "Desired";
            this.buttonConfigsUpdateDesiredConfigs.UseVisualStyleBackColor = true;
            this.buttonConfigsUpdateDesiredConfigs.Margin = new Padding(0);
            this.buttonConfigsUpdateDesiredConfigs.Click += new System.EventHandler(this.buttonConfigsUpdateDesiredConfigs_Click);


            // config diff links child layout panel
            this.tableLayoutPanelConfigDiffLinks = new System.Windows.Forms.TableLayoutPanel();

            this.tableLayoutPanelConfigDiffLinks.RowCount = 1;
            this.tableLayoutPanelConfigDiffLinks.ColumnCount = 8;

            this.tableLayoutPanelConfigDiffLinks.AutoScroll = false;
            this.tableLayoutPanelConfigDiffLinks.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            this.tableLayoutPanelConfigDiffLinks.AutoSize = true;
            this.tableLayoutPanelConfigDiffLinks.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelConfigDiffLinks.Name = "tableLayoutPanelConfigDiffLinks";
            this.tableLayoutPanelConfigDiffLinks.TabIndex = 0;

            string[] configDiffs = null;
            List<string> configDiffList = CheckConfigDifferences(Path.GetFullPath(this.actualConfigFolder), Path.GetFullPath(this.localConfigFolder));
            if (null != configDiffList)
            {
                configDiffs = configDiffList.ToArray();
            }
            else
            {
                configDiffs = new string[0];
            }

            HandleConfigDiffsDisplay(configDiffs);

            // 
            // linkLabelConfigsApps
            // 

            this.tableLayoutPanel.Controls.Add(this.checkBoxConfigsHubId, 0, rowNumber);
            this.tableLayoutPanel.Controls.Add(this.buttonConfigsUpdateActualConfigs, 1, rowNumber);
            this.tableLayoutPanel.Controls.Add(this.buttonConfigsUpdateDesiredConfigs, 2, rowNumber);
            this.tableLayoutPanel.Controls.Add(this.tableLayoutPanelConfigDiffLinks, 3, rowNumber);
            this.tableLayoutPanel.SetColumnSpan(this.tableLayoutPanelConfigDiffLinks, 3);

            StartWatchingDesiredLocalConfigChanges(Path.GetFullPath(this.localConfigFolder));
        }

        private List<string> CheckConfigDifferences(string actualConfigFolderPath, string desiredConfigFolderPath)
        {
            List<string> diffConfigXmlFiles = null; 
            List<string> actualConfigs = Utils.ListFiles(null, actualConfigFolderPath);
            List<string> desiredConfigs = Utils.ListFiles(null, desiredConfigFolderPath);

            if (desiredConfigs.Count == 0)
            {
                DirectoryInfo diActual = new DirectoryInfo(actualConfigFolderPath);
                if (diActual.Exists && diActual.GetFiles().Count() > 0)
                {
                    PackagerHelper.PackagerHelper.CopyFolder(actualConfigFolderPath, desiredConfigFolderPath);
                    desiredConfigs = Utils.ListFiles(null, desiredConfigFolderPath);
                }
            }

            // remove version related files from the comparison
            List<string> temp = new List<string>();
            foreach (string s in actualConfigs)
            {
                if (Path.GetExtension(actualConfigFolderPath + "\\" + s).ToLower() == ".xml")
                    temp.Add(s);
            }
            actualConfigs = temp;
            temp = new List<string>();
            foreach (string s in desiredConfigs)
            {
                if (Path.GetExtension(desiredConfigFolderPath + "\\" + s).ToLower() == ".xml")
                    temp.Add(s);
            }
            desiredConfigs = temp;


            Debug.Assert(actualConfigs.Count == desiredConfigs.Count);
            foreach (string config in actualConfigs)
            {
                string actual = File.ReadAllText(actualConfigFolderPath + "\\" + config);
                string match = desiredConfigs.Find(s => { return (s == config); });
                if (null == match || match != config)
                {
                    break;
                }

                string desired = File.ReadAllText(desiredConfigFolderPath + "\\" + match);
                if (desired != actual)
                {
                    if (null == diffConfigXmlFiles)
                    {
                        diffConfigXmlFiles = new List<string>();
                    }
                    diffConfigXmlFiles.Add(config);
                }
            }

            return diffConfigXmlFiles;
        }

        private void HandleConfigDiffsDisplay(string[] configDiffs)
        {
            bool different = false;
            if (configDiffs.Length > 0)
                Array.Sort(configDiffs);

            // compare the currently displayed config differences with that being passed in
            different = configDiffsDisplayed.Length != configDiffs.Length;
            if (!different)
            {
                for (int c = 0; c < configDiffsDisplayed.Length; ++c)
                {
                    if (configDiffsDisplayed[c] != configDiffs[c])
                    {
                        different = true;
                        break;
                    }
                }
            }

            // nothing to do
            if (!different)
            {
                return;
            }

            for (int i = 0; i < configDiffs.Length; ++i)
            {
                System.Windows.Forms.LinkLabel linkLabelConfigsDiffConfig = new System.Windows.Forms.LinkLabel();
                linkLabelConfigsDiffConfig.AutoSize = true;
                linkLabelConfigsDiffConfig.Enabled = false; // TODO: If you have a text diffing tool then you should set this true
                linkLabelConfigsDiffConfig.Dock = System.Windows.Forms.DockStyle.Fill;
                linkLabelConfigsDiffConfig.Name = "linkLabelConfigs" + configDiffs[i] + this.rowNumber.ToString();
                linkLabelConfigsDiffConfig.TabIndex = 16;
                linkLabelConfigsDiffConfig.TabStop = true;
                linkLabelConfigsDiffConfig.Text = configDiffs[i];
                linkLabelConfigsDiffConfig.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                linkLabelConfigsDiffConfig.Margin = new Padding(0);
                linkLabelConfigsDiffConfig.AutoEllipsis = true;
                linkLabelConfigsDiffConfig.Links[0].LinkData = configDiffs[i];

                linkLabelConfigsDiffConfig.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(linkLabelConfigsDiffConfig_LinkClicked);
                this.tableLayoutPanelConfigDiffLinks.Controls.Add(linkLabelConfigsDiffConfig, i, rowNumber);
            }

            this.configDiffsDisplayed = configDiffs;
        }

        private void StartWatchingDesiredLocalConfigChanges(string watchFolder)
        {
            try
            {
                this.watcher = new FileSystemWatcher();
                watcher.Path = watchFolder;

                /* Watch for changes in LastWrite times, and the renaming of files or directories. */
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                // Only watch xml files.
                watcher.Filter = "*.xml";

                // Add event handlers.
                watcher.Changed += new FileSystemEventHandler(OnEditingChanges);
                watcher.Created += new FileSystemEventHandler(OnNonEditingChanges);
                watcher.Deleted += new FileSystemEventHandler(OnNonEditingChanges);
                watcher.Renamed += new RenamedEventHandler(OnNonEditingChanges);


                // Send events.
                watcher.EnableRaisingEvents = true;

                watcher.BeginInit();
            }
            catch (Exception exception)
            {
                this.logger.ErrorException(string.Format("Exception while trying to watch folder {0} for changes", watchFolder), exception);
            }
        }

        // Define the event handlers. 
        private void OnEditingChanges(object source, FileSystemEventArgs e)
        {
            string[] configDiffs = null;
            List<string> configDiffList = CheckConfigDifferences(Path.GetFullPath(this.actualConfigFolder), Path.GetFullPath(this.localConfigFolder));
            if (null != configDiffList)
            {
                configDiffs = configDiffList.ToArray();
            }
            else
            {
                configDiffs = new string[0];
            }

            HandleConfigDiffsDisplay(configDiffs);
        }

        private void AlertNonEditingChanges()
        {
            MessageBox.Show(string.Format("Add, removing or renaming of congiguration files is not unexpected!", this.hubId),
                            "Edit Desired Hub Configuration",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation
                            );
        }

        private void OnNonEditingChanges(object source, FileSystemEventArgs e)
        {
            AlertNonEditingChanges();
        }
        private void OnNonEditingChanges(object source, RenamedEventArgs e)
        {
            AlertNonEditingChanges();
        }

        public bool IsChecked()
        {
            return this.checkBoxConfigsHubId.Checked;
        }
        public void Check()
        {
            this.checkBoxConfigsHubId.Checked = true;
        }

        public void UnCheck()
        {
            this.checkBoxConfigsHubId.Checked = false;
        }

        private void checkBoxConfigsHubId_CheckedChanged(object sender, EventArgs e)
        {
            EventHandler handler = HubSelectionChange;
            if (null != handler)
            {
                handler(this, new EventArgs());
            }
        }

        private void buttonConfigsUpdateActualConfigs_Click(object sender, EventArgs e)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(this.actualConfigFolder);
                if (di.Exists && di.GetFiles().Count() > 0)
                {
                    Process.Start("Explorer.exe", this.actualConfigFolder);
                }
                else
                {
                    throw new DirectoryNotFoundException();
                }
            }
            catch(Exception)
            {
                MessageBox.Show(string.Format("Actual Deployed Configuration is not present on the server for {0}!", this.hubId),
                                "View Actual Hub Configuration",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation
                                );
            }
        }

        private void buttonConfigsUpdateDesiredConfigs_Click(object sender, EventArgs e)
        {
            try
            {
                DirectoryInfo diActual = new DirectoryInfo(this.actualConfigFolder);
                DirectoryInfo diLocal = new DirectoryInfo(this.localConfigFolder);
                if (diLocal.Exists && diLocal.GetFiles().Count() > 0)
                {
                    Process.Start("Explorer.exe", this.localConfigFolder);
                }
                else if (diActual.Exists && diActual.GetFiles().Count() > 0)
                {
                    PackagerHelper.PackagerHelper.CopyFolder(this.actualConfigFolder, this.localConfigFolder);
                    Process.Start("Explorer.exe", this.localConfigFolder);
                }
                else
                {
                    throw new DirectoryNotFoundException();
                }
            }
            catch (Exception)
            {
                MessageBox.Show(string.Format("Actual Deployed Configuration is not present on the server for {0}!", this.hubId),
                                "Edit Desired Hub Configuration",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation
                                );
            }

        }

        private void linkLabelConfigsDiffConfig_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string configXmlName = e.Link.LinkData as string;
            string cachefolder = Path.GetFullPath(this.actualConfigFolder);
            string localfolder = Path.GetFullPath(this.localConfigFolder);
            MessageBox.Show(string.Format("Now imagine we are Diffing... {0} with {1}!", cachefolder + "\\" + configXmlName + ".xml", this.localConfigFolder + "\\" + configXmlName + ".xml"),
                            "Diff Hub Configuration",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation
                            );

        }

    }
}
