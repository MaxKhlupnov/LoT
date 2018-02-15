using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeOS.Hub.Tools.UpdateManager
{
    public partial class AzureAccountForm : Form
    {
        private bool formUpdated;

        public AzureAccountForm()
        {
            InitializeComponent();
            this.FormClosing += AzureAccountForm_FormClosing;
            LoadPersistedSettings();

        }

        // cached information to avoid loading persisted values
        public string AzureAccountName { get; private set; }
        public string AzureAccountKey { get; private set; }

        void AzureAccountForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SavePersistedSettings();
        }

        private void LoadPersistedSettings()
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SetupAzureAccountName))
            {
                this.textBoxAzureAccountName.Text = Properties.Settings.Default.SetupAzureAccountName;
                this.AzureAccountName = this.textBoxAzureAccountName.Text;
            }
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SetupAzureAccountKey))
            {
                this.textBoxAzureAccountKey.Text = Properties.Settings.Default.SetupAzureAccountKey;
                this.AzureAccountKey = this.textBoxAzureAccountKey.Text;
            }

            this.formUpdated = false;

        }

        private void SavePersistedSettings()
        {
            if (!this.formUpdated)
                return;

            // save last working working dir
            Properties.Settings.Default.SetupAzureAccountName = this.textBoxAzureAccountName.Text;
            this.AzureAccountName = this.textBoxAzureAccountName.Text;

            Properties.Settings.Default.SetupAzureAccountKey = this.textBoxAzureAccountKey.Text;
            this.AzureAccountKey = this.textBoxAzureAccountKey.Text;


            Properties.Settings.Default.Save();
        }

        private void buttonAzureAccountAdd_Click(object sender, EventArgs e)
        {
            this.formUpdated = true;
        }

    }
}
