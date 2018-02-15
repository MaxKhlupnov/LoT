using System;
using System.Windows.Forms;

namespace HomeOS.Hub.Tools.UpdateManager
{
    public partial class MainForm
    {
        protected RepoAccountInfoForm formRepoAccountInfo;
        protected AzureAccountForm formAzureAccount;

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelSetup;
        private System.Windows.Forms.TextBox textBoxAzureAcctInfo;
        private System.Windows.Forms.Button btnSetupEditAzureAcctInfo;
        private System.Windows.Forms.Button btnSetupRemoveAzureAcctInfo;
        private System.Windows.Forms.Label labelSetupWorkingFolderHelp;
        private System.Windows.Forms.TextBox textBoxSetupWorkingFolder;
        private System.Windows.Forms.TextBox textBoxSetupRepoAcctInfo;
        private System.Windows.Forms.Button btnSetupEditRepoAcctInfo;
        private System.Windows.Forms.Button btnSetupRemoveRepoAcctInfo;
        private System.Windows.Forms.Button btnSetupBrowseFolder;
        private System.Windows.Forms.Label labelSetupAzureAccount;
        private System.Windows.Forms.Label labelSetupRepoAccount;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserWorkingDir;
        private System.Windows.Forms.Button buttonSetupLoad;


        private void ShowSetupTab()
        {
            this.formRepoAccountInfo = new RepoAccountInfoForm();
            this.formAzureAccount = new AzureAccountForm();

            this.tableLayoutPanelSetup = new System.Windows.Forms.TableLayoutPanel();
            this.labelSetupWorkingFolderHelp = new System.Windows.Forms.Label();
            this.textBoxSetupWorkingFolder = new System.Windows.Forms.TextBox();
            this.textBoxSetupRepoAcctInfo = new System.Windows.Forms.TextBox();
            this.textBoxAzureAcctInfo = new System.Windows.Forms.TextBox();
            this.btnSetupRemoveRepoAcctInfo = new System.Windows.Forms.Button();
            this.btnSetupRemoveAzureAcctInfo = new System.Windows.Forms.Button();
            this.labelSetupAzureAccount = new System.Windows.Forms.Label();
            this.labelSetupRepoAccount = new System.Windows.Forms.Label();
            this.btnSetupBrowseFolder = new System.Windows.Forms.Button();
            this.btnSetupEditRepoAcctInfo = new System.Windows.Forms.Button();
            this.btnSetupEditAzureAcctInfo = new System.Windows.Forms.Button();
            this.buttonSetupLoad = new System.Windows.Forms.Button();
            this.folderBrowserWorkingDir = new System.Windows.Forms.FolderBrowserDialog();

            LoadSetupSettings();

            this.tabSetup.SuspendLayout();
            this.tableLayoutPanelSetup.SuspendLayout();


            // 
            // tableLayoutPanelSetup
            // 
            this.tableLayoutPanelSetup.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelSetup.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.tableLayoutPanelSetup.ColumnCount = 5;
            this.tableLayoutPanelSetup.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelSetup.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelSetup.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.tableLayoutPanelSetup.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 78F));
            this.tableLayoutPanelSetup.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 157F));
            this.tableLayoutPanelSetup.Controls.Add(this.labelSetupWorkingFolderHelp, 0, 0);
            this.tableLayoutPanelSetup.Controls.Add(this.textBoxSetupWorkingFolder, 0, 1);
            this.tableLayoutPanelSetup.Controls.Add(this.btnSetupBrowseFolder, 2, 1);
            this.tableLayoutPanelSetup.Controls.Add(this.labelSetupRepoAccount, 0, 2);
            this.tableLayoutPanelSetup.Controls.Add(this.textBoxSetupRepoAcctInfo, 0, 3);
            this.tableLayoutPanelSetup.Controls.Add(this.btnSetupEditRepoAcctInfo, 2, 3);
            this.tableLayoutPanelSetup.Controls.Add(this.btnSetupRemoveRepoAcctInfo, 3, 3);
            this.tableLayoutPanelSetup.Controls.Add(this.labelSetupAzureAccount, 0, 4);
            this.tableLayoutPanelSetup.Controls.Add(this.textBoxAzureAcctInfo, 0, 5);
            this.tableLayoutPanelSetup.Controls.Add(this.btnSetupEditAzureAcctInfo, 2, 5);
            this.tableLayoutPanelSetup.Controls.Add(this.btnSetupRemoveAzureAcctInfo, 3, 5);
            this.tableLayoutPanelSetup.Controls.Add(this.buttonSetupLoad, 0, 6);
            this.tableLayoutPanelSetup.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelSetup.Name = "tableLayoutPanelSetup";
            this.tableLayoutPanelSetup.RowCount = 7;
            this.tableLayoutPanelSetup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.72464F));
            this.tableLayoutPanelSetup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 49.27536F));
            this.tableLayoutPanelSetup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 73F));
            this.tableLayoutPanelSetup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 91F));
            this.tableLayoutPanelSetup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.tableLayoutPanelSetup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 125F));
            this.tableLayoutPanelSetup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22));

            this.tableLayoutPanelSetup.Size = new System.Drawing.Size(635, 398);
            this.tableLayoutPanelSetup.TabIndex = 0;

            this.tabSetup.Controls.Add(this.tableLayoutPanelSetup);

            // 
            // labelSetupWorkingFolderHelp
            // 
            this.labelSetupWorkingFolderHelp.AutoSize = true;
            this.tableLayoutPanelSetup.SetColumnSpan(this.labelSetupWorkingFolderHelp, 5);
            this.labelSetupWorkingFolderHelp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelSetupWorkingFolderHelp.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSetupWorkingFolderHelp.Location = new System.Drawing.Point(3, 0);
            this.labelSetupWorkingFolderHelp.Name = "labelSetupWorkingFolderHelp";
            this.labelSetupWorkingFolderHelp.Size = new System.Drawing.Size(629, 25);
            this.labelSetupWorkingFolderHelp.TabIndex = 9;
            this.labelSetupWorkingFolderHelp.Text = "Local working folder location (must be the output folder)";
            this.labelSetupWorkingFolderHelp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxSetupWorkingFolder
            // 
            this.textBoxSetupWorkingFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelSetup.SetColumnSpan(this.textBoxSetupWorkingFolder, 2);
            this.textBoxSetupWorkingFolder.Location = new System.Drawing.Point(3, 33);
            this.textBoxSetupWorkingFolder.Name = "textBoxSetupWorkingFolder";
            this.textBoxSetupWorkingFolder.Size = new System.Drawing.Size(318, 20);
            this.textBoxSetupWorkingFolder.TabIndex = 6;
            // 
            // textBoxSetupRepoAcctInfo
            // 
            this.tableLayoutPanelSetup.SetColumnSpan(this.textBoxSetupRepoAcctInfo, 2);
            this.textBoxSetupRepoAcctInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxSetupRepoAcctInfo.Location = new System.Drawing.Point(3, 135);
            this.textBoxSetupRepoAcctInfo.Name = "textBoxSetupRepoAcctInfo";
            this.textBoxSetupRepoAcctInfo.Size = new System.Drawing.Size(318, 20);
            this.textBoxSetupRepoAcctInfo.TabIndex = 3;
            // 
            // textBoxAzureAcctInfo
            // 
            this.tableLayoutPanelSetup.SetColumnSpan(this.textBoxAzureAcctInfo, 2);
            this.textBoxAzureAcctInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxAzureAcctInfo.Location = new System.Drawing.Point(3, 255);
            this.textBoxAzureAcctInfo.Name = "textBoxAzureAcctInfo";
            this.textBoxAzureAcctInfo.Size = new System.Drawing.Size(318, 20);
            this.textBoxAzureAcctInfo.TabIndex = 0;
            // 
            // btnSetupRemoveRepoAcctInfo
            // 
            this.btnSetupRemoveRepoAcctInfo.Location = new System.Drawing.Point(403, 135);
            this.btnSetupRemoveRepoAcctInfo.Name = "btnSetupRemoveRepoAcctInfo";
            this.btnSetupRemoveRepoAcctInfo.Size = new System.Drawing.Size(72, 20);
            this.btnSetupRemoveRepoAcctInfo.TabIndex = 5;
            this.btnSetupRemoveRepoAcctInfo.Text = "Remove";
            this.btnSetupRemoveRepoAcctInfo.UseVisualStyleBackColor = true;
            this.btnSetupRemoveRepoAcctInfo.Click += new System.EventHandler(this.btnSetupRemoveRepoAcctInfo_Click);
            // 
            // btnSetupRemoveAzureAcctInfo
            // 
            this.btnSetupRemoveAzureAcctInfo.Location = new System.Drawing.Point(403, 255);
            this.btnSetupRemoveAzureAcctInfo.Name = "btnSetupRemoveAzureAcctInfo";
            this.btnSetupRemoveAzureAcctInfo.Size = new System.Drawing.Size(72, 20);
            this.btnSetupRemoveAzureAcctInfo.TabIndex = 2;
            this.btnSetupRemoveAzureAcctInfo.Text = "Remove";
            this.btnSetupRemoveAzureAcctInfo.UseVisualStyleBackColor = true;
            this.btnSetupRemoveAzureAcctInfo.Click += new System.EventHandler(this.btnSetupRemoveAzureAcctInfo_Click);
            // 
            // labelSetupAzureAccount
            // 
            this.tableLayoutPanelSetup.SetColumnSpan(this.labelSetupAzureAccount, 2);
            this.labelSetupAzureAccount.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelSetupAzureAccount.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSetupAzureAccount.Location = new System.Drawing.Point(3, 229);
            this.labelSetupAzureAccount.Name = "labelSetupAzureAccount";
            this.labelSetupAzureAccount.Size = new System.Drawing.Size(318, 25);
            this.labelSetupAzureAccount.TabIndex = 12;
            this.labelSetupAzureAccount.Text = "Azure Storage Account";
            // 
            // labelSetupRepoAccount
            // 
            this.tableLayoutPanelSetup.SetColumnSpan(this.labelSetupRepoAccount, 5);
            this.labelSetupRepoAccount.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelSetupRepoAccount.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSetupRepoAccount.Location = new System.Drawing.Point(3, 107);
            this.labelSetupRepoAccount.Name = "labelSetupRepoAccount";
            this.labelSetupRepoAccount.Size = new System.Drawing.Size(629, 25);
            this.labelSetupRepoAccount.TabIndex = 11;
            this.labelSetupRepoAccount.Text = "Repository Account (Secure FTP required)";
            // 
            // btnSetupBrowseFolder
            // 
            this.btnSetupBrowseFolder.Location = new System.Drawing.Point(350, 45);
            this.btnSetupBrowseFolder.Name = "btnSetupBrowseFolder";
            this.btnSetupBrowseFolder.Size = new System.Drawing.Size(70, 20);
            this.btnSetupBrowseFolder.TabIndex = 7;
            this.btnSetupBrowseFolder.Text = "Browse";
            this.btnSetupBrowseFolder.UseVisualStyleBackColor = true;
            this.btnSetupBrowseFolder.Click += new System.EventHandler(this.btnSetupBrowseFolder_Click);
            // 
            // btnSetupEditRepoAcctInfo
            // 
            this.btnSetupEditRepoAcctInfo.Location = new System.Drawing.Point(327, 135);
            this.btnSetupEditRepoAcctInfo.Name = "btnSetupEditRepoAcctInfo";
            this.btnSetupEditRepoAcctInfo.Size = new System.Drawing.Size(70, 20);
            this.btnSetupEditRepoAcctInfo.TabIndex = 4;
            this.btnSetupEditRepoAcctInfo.Text = "Edit";
            this.btnSetupEditRepoAcctInfo.UseVisualStyleBackColor = true;
            this.btnSetupEditRepoAcctInfo.Click += new System.EventHandler(this.btnSetupEditRepoAcctInfo_Click);
            // 
            // btnSetupEditAzureAcctInfo
            // 
            this.btnSetupEditAzureAcctInfo.Location = new System.Drawing.Point(327, 255);
            this.btnSetupEditAzureAcctInfo.Name = "btnSetupEditAzureAcctInfo";
            this.btnSetupEditAzureAcctInfo.Size = new System.Drawing.Size(70, 20);
            this.btnSetupEditAzureAcctInfo.TabIndex = 1;
            this.btnSetupEditAzureAcctInfo.Text = "Edit";
            this.btnSetupEditAzureAcctInfo.UseVisualStyleBackColor = true;
            this.btnSetupEditAzureAcctInfo.Click += new System.EventHandler(this.btnSetupEditAzureAcctInfo_Click);

            // 
            // buttonSetupLoad
            // 
            this.labelSetupAzureAccount.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonSetupLoad.Size = new System.Drawing.Size(70, 20);
            this.buttonSetupLoad.Name = "buttonSetupLoad";
            this.buttonSetupLoad.TabIndex = 8;
            this.buttonSetupLoad.Text = "Load";
            this.buttonSetupLoad.UseVisualStyleBackColor = true;
            this.buttonSetupLoad.Margin = new Padding(0);
            this.buttonSetupLoad.Click += buttonSetupLoad_Click;


            // 
            // folderBrowserWorkingDir
            // 
            this.folderBrowserWorkingDir.Description = "Please navigate to the working folder directory";
            this.folderBrowserWorkingDir.RootFolder = System.Environment.SpecialFolder.MyComputer;
            this.folderBrowserWorkingDir.ShowNewFolderButton = false;

            this.tabSetup.ResumeLayout(false);
            this.tableLayoutPanelSetup.ResumeLayout(false);
            this.tableLayoutPanelSetup.PerformLayout();

            PopulateRepoAccountInfoTitle();
            PopulateAzureAccountInfoTitle();

        }

        private void PopulateRepoAccountInfoTitle()
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SetupRepoAcctInfo))
            {
                this.textBoxSetupRepoAcctInfo.Text = Properties.Settings.Default.SetupRepoAcctInfo;
                if (string.IsNullOrWhiteSpace(this.formRepoAccountInfo.RepoAccountHost) || string.IsNullOrWhiteSpace(this.formRepoAccountInfo.RepoAccountPort))
                {
                    this.textBoxSetupRepoAcctInfo.Text += "   [***FTP Host Info Required***]";
                }
                else if (string.IsNullOrWhiteSpace(this.formRepoAccountInfo.RepoAccountLogin))
                {
                    this.textBoxSetupRepoAcctInfo.Text += "   [***Login Required***]";
                }
                else if (string.IsNullOrWhiteSpace(this.formRepoAccountInfo.RepoAccountPassword))
                {
                    this.textBoxSetupRepoAcctInfo.Text += "   [***Password Required***]";
                }
                //AJB added so that the repo account name shows up. For this and PopulateAzureAccountInfoTitle, I think the first line is incorrect, since default will always have a value
                //and else at the end of method will never be called. However, the goal and posisble uses cases are large enough that I am making only this small local change. 
                else if (!string.IsNullOrWhiteSpace(this.formRepoAccountInfo.RepoAccountHost))
                {
                    this.textBoxSetupRepoAcctInfo.Text = this.formRepoAccountInfo.RepoAccountHost + "[Login=" + this.formRepoAccountInfo.RepoAccountLogin + "]";
                }

            }
            else
            {
                if (!string.IsNullOrWhiteSpace(this.formRepoAccountInfo.RepoAccountHost))
                {
                    this.textBoxSetupRepoAcctInfo.Text = this.formRepoAccountInfo.RepoAccountHost + "[Login=" + this.formRepoAccountInfo.RepoAccountLogin + "]";
                }
            }
        }

        private void PopulateAzureAccountInfoTitle()
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SetupAzureAccountTitle))
            {
                this.textBoxAzureAcctInfo.Text = Properties.Settings.Default.SetupAzureAccountTitle;
                if (string.IsNullOrWhiteSpace(this.formAzureAccount.AzureAccountName) || string.IsNullOrWhiteSpace(this.formAzureAccount.AzureAccountKey))
                {
                    this.textBoxAzureAcctInfo.Text += "   [***Account Info Required***]";
                }
                else if (string.IsNullOrWhiteSpace(this.formAzureAccount.AzureAccountName))
                {
                    this.textBoxAzureAcctInfo.Text += "   [***Account Name Required***]";
                }
                else if (string.IsNullOrWhiteSpace(this.formAzureAccount.AzureAccountKey))
                {
                    this.textBoxAzureAcctInfo.Text += "   [***Account Key Required***]";
                }
                    //AJB added so that the azure account name shows up not the default "Azure Storage Account Title" This is a workaround
                //I think the problem is the first line is this methods because the default will never be null, but I don't understand what the goal was enough to make a larger change
                //if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SetupAzureAccountTitle))
                else if (!string.IsNullOrWhiteSpace(this.formAzureAccount.AzureAccountName))
                {
                    this.textBoxAzureAcctInfo.Text = this.formAzureAccount.AzureAccountName;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(this.formAzureAccount.AzureAccountName))
                {
                    this.textBoxAzureAcctInfo.Text = this.formAzureAccount.AzureAccountName;
                }
            }

        }

        private void LoadSetupSettings()
        {
            // load last browsed location, if available
            this.folderBrowserWorkingDir.RootFolder = Environment.SpecialFolder.DesktopDirectory;
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SetupWorkingDirUserSelectedPath))
            {
                this.folderBrowserWorkingDir.SelectedPath = Properties.Settings.Default.SetupWorkingDirUserSelectedPath;
                this.textBoxSetupWorkingFolder.Text = Properties.Settings.Default.SetupWorkingDirUserSelectedPath;
            }
        }

        private void SaveSetupSettings()
        {
            // save last working working dir
            if (!string.IsNullOrWhiteSpace(this.folderBrowserWorkingDir.SelectedPath))
            {
                Properties.Settings.Default.SetupWorkingDirUserSelectedPath = this.folderBrowserWorkingDir.SelectedPath;
            }
            if (!string.IsNullOrWhiteSpace(this.textBoxSetupRepoAcctInfo.Text))
            {
                Properties.Settings.Default.SetupRepoAcctInfo = this.textBoxSetupRepoAcctInfo.Text.Split('[')[0];
            }

            Properties.Settings.Default.Save();
        }

        private void btnSetupBrowseFolder_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserWorkingDir.ShowDialog() == DialogResult.OK)
            {
                this.textBoxSetupWorkingFolder.Text = folderBrowserWorkingDir.SelectedPath;
            }
        }

        private void btnSetupEditRepoAcctInfo_Click(object sender, EventArgs e)
        {
            if (this.formRepoAccountInfo.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                PopulateRepoAccountInfoTitle();
            }
        }

        private void DeleteAzureAcctInfo()
        {
            Properties.Settings.Default.SetupAzureAccountTitle = "";
            Properties.Settings.Default.SetupAzureAccountName = "";
            Properties.Settings.Default.SetupAzureAccountKey = "";

            Properties.Settings.Default.Save();

            this.textBoxAzureAcctInfo.Text = "";

            this.formAzureAccount.Dispose();
            this.formAzureAccount = new AzureAccountForm();
        }

        private void DeleteRepoAcctInfo()
        {
            Properties.Settings.Default.SetupRepoAcctInfo = "";
            Properties.Settings.Default.SetupRepoAccountHost = "";
            Properties.Settings.Default.SetupRepoAccountPort = "";
            Properties.Settings.Default.SetupRepoAccountLogin = "";
            Properties.Settings.Default.SetupRepoAccountPassword = "";

            Properties.Settings.Default.Save();

            this.textBoxSetupRepoAcctInfo.Text = "";

            this.formRepoAccountInfo.Dispose();
            this.formRepoAccountInfo = new RepoAccountInfoForm();
        }

        private void btnSetupRemoveRepoAcctInfo_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes == MessageBox.Show(this, "Remove the FTP Account Information?", "Remove Repository Account", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2))
            {
                DeleteRepoAcctInfo();
            }
        }

        private void btnSetupEditAzureAcctInfo_Click(object sender, EventArgs e)
        {
            if (this.formAzureAccount.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                PopulateAzureAccountInfoTitle();
            }
        }

        private void btnSetupRemoveAzureAcctInfo_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes == MessageBox.Show(this, "Remove the Azure Account Information?", "Remove Azure Account", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2))
            {
                DeleteAzureAcctInfo();
            }
        }

        async void buttonSetupLoad_Click(object sender, EventArgs e)
        {
            //disable the UI
            outputBox.Text += "Retrieving config, module and platform information\r\n";
            btnSetupBrowseFolder.Enabled = false;
            btnSetupEditRepoAcctInfo.Enabled = false;
            btnSetupEditAzureAcctInfo.Enabled = false;
            btnSetupRemoveRepoAcctInfo.Enabled = false;
            btnSetupRemoveAzureAcctInfo.Enabled = false;
            buttonSetupLoad.Enabled = false;
            // after loading the persisted state we should see if we are setup to show the update tab
            if (await TryShowingConfigsTab())
            {
                OnShowingConfigsTab();
            }
            if (await TryShowingBinaryTabs())
            {
                OnShowingPlatformTab();
                OnShowingModuleScoutTab();
            }
            //enable UI
            outputBox.Text += "Done loading configs, modules and platform information\r\n";
            btnSetupBrowseFolder.Enabled = true;
            btnSetupEditRepoAcctInfo.Enabled = true;
            btnSetupEditAzureAcctInfo.Enabled = true;
            btnSetupRemoveRepoAcctInfo.Enabled = true;
            btnSetupRemoveAzureAcctInfo.Enabled = true;
            buttonSetupLoad.Enabled = true;
        }


    }
}
