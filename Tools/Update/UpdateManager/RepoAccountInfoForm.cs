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
    public partial class RepoAccountInfoForm : Form
    {
        private bool formUpdated;
        public RepoAccountInfoForm()
        {
            InitializeComponent();
            this.FormClosing += RepoAccountInfoForm_FormClosing;

            LoadPersistedSettings();
        }

        // cached information to avoid loading persisted values
        public string RepoAccountHost { get; private set; }
        public string RepoAccountPort { get; private set; }
        public string RepoAccountLogin { get; private set; }
        public string RepoAccountPassword { get; private set; }

        void RepoAccountInfoForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SavePersistedSettings();
        }


        private void LoadPersistedSettings()
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SetupRepoAccountHost))
            {
                this.textBoxRepoAccountHost.Text = Properties.Settings.Default.SetupRepoAccountHost;
                this.RepoAccountHost = this.textBoxRepoAccountHost.Text;
            }
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SetupRepoAccountPort))
            {
                this.textBoxRepoAccountPort.Text = Properties.Settings.Default.SetupRepoAccountPort;
                this.RepoAccountPort = this.textBoxRepoAccountPort.Text;
            }
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SetupRepoAccountLogin))
            {
                this.textBoxRepoAccountLogin.Text = Properties.Settings.Default.SetupRepoAccountLogin;
                this.RepoAccountLogin = this.textBoxRepoAccountLogin.Text;
            }
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SetupRepoAccountPassword))
            {
                this.maskedTextBoxRepoAccountPassword.Text = MainForm.SimpleEncryptDecrypt(Properties.Settings.Default.SetupRepoAccountPassword, null);
                this.RepoAccountPassword = this.maskedTextBoxRepoAccountPassword.Text;
            }

            this.formUpdated = false;

        }

        private void SavePersistedSettings()
        {
            if (!this.formUpdated)
                return;

            // save last working working dir
            Properties.Settings.Default.SetupRepoAccountHost = this.textBoxRepoAccountHost.Text;
            this.RepoAccountHost = this.textBoxRepoAccountHost.Text;

            Properties.Settings.Default.SetupRepoAccountPort = this.textBoxRepoAccountPort.Text;
            this.RepoAccountPort = this.textBoxRepoAccountPort.Text;

            Properties.Settings.Default.SetupRepoAccountLogin = this.textBoxRepoAccountLogin.Text;
            this.RepoAccountLogin = this.textBoxRepoAccountLogin.Text;

            Properties.Settings.Default.SetupRepoAccountPassword = MainForm.SimpleEncryptDecrypt(this.maskedTextBoxRepoAccountPassword.Text, null);
            this.RepoAccountPassword = this.maskedTextBoxRepoAccountPassword.Text;

            Properties.Settings.Default.Save();
        }

        private void buttonRepoAccountAdd_Click(object sender, EventArgs e)
        {
            this.formUpdated = true;
        }
    }
}
