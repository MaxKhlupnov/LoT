namespace HomeOS.Hub.Tools.UpdateManager
{
    partial class RepoAccountInfoForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RepoAccountInfoForm));
            this.textBoxRepoAccountHost = new System.Windows.Forms.TextBox();
            this.textBoxRepoAccountLogin = new System.Windows.Forms.TextBox();
            this.maskedTextBoxRepoAccountPassword = new System.Windows.Forms.MaskedTextBox();
            this.buttonRepoAccountAdd = new System.Windows.Forms.Button();
            this.textBoxRepoAccountPort = new System.Windows.Forms.TextBox();
            this.labelRepoAccountPort = new System.Windows.Forms.Label();
            this.labelRepAccountHost = new System.Windows.Forms.Label();
            this.labelRepAccountLogin = new System.Windows.Forms.Label();
            this.labelRepAccountPassword = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBoxRepoAccountHost
            // 
            this.textBoxRepoAccountHost.Location = new System.Drawing.Point(71, 30);
            this.textBoxRepoAccountHost.Name = "textBoxRepoAccountHost";
            this.textBoxRepoAccountHost.Size = new System.Drawing.Size(171, 20);
            this.textBoxRepoAccountHost.TabIndex = 0;
            // 
            // textBoxRepoAccountLogin
            // 
            this.textBoxRepoAccountLogin.Location = new System.Drawing.Point(71, 85);
            this.textBoxRepoAccountLogin.Name = "textBoxRepoAccountLogin";
            this.textBoxRepoAccountLogin.Size = new System.Drawing.Size(100, 20);
            this.textBoxRepoAccountLogin.TabIndex = 1;
            // 
            // maskedTextBoxRepoAccountPassword
            // 
            this.maskedTextBoxRepoAccountPassword.Location = new System.Drawing.Point(71, 141);
            this.maskedTextBoxRepoAccountPassword.Name = "maskedTextBoxRepoAccountPassword";
            this.maskedTextBoxRepoAccountPassword.PasswordChar = '*';
            this.maskedTextBoxRepoAccountPassword.Size = new System.Drawing.Size(100, 20);
            this.maskedTextBoxRepoAccountPassword.TabIndex = 2;
            // 
            // buttonRepoAccountAdd
            // 
            this.buttonRepoAccountAdd.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonRepoAccountAdd.Location = new System.Drawing.Point(272, 138);
            this.buttonRepoAccountAdd.Name = "buttonRepoAccountAdd";
            this.buttonRepoAccountAdd.Size = new System.Drawing.Size(75, 23);
            this.buttonRepoAccountAdd.TabIndex = 3;
            this.buttonRepoAccountAdd.Text = "Update";
            this.buttonRepoAccountAdd.UseVisualStyleBackColor = true;
            this.buttonRepoAccountAdd.Click += new System.EventHandler(this.buttonRepoAccountAdd_Click);
            // 
            // textBoxRepoAccountPort
            // 
            this.textBoxRepoAccountPort.Location = new System.Drawing.Point(290, 30);
            this.textBoxRepoAccountPort.Name = "textBoxRepoAccountPort";
            this.textBoxRepoAccountPort.Size = new System.Drawing.Size(53, 20);
            this.textBoxRepoAccountPort.TabIndex = 4;
            // 
            // labelRepoAccountPort
            // 
            this.labelRepoAccountPort.AutoSize = true;
            this.labelRepoAccountPort.Location = new System.Drawing.Point(258, 33);
            this.labelRepoAccountPort.Name = "labelRepoAccountPort";
            this.labelRepoAccountPort.Size = new System.Drawing.Size(26, 13);
            this.labelRepoAccountPort.TabIndex = 5;
            this.labelRepoAccountPort.Text = "Port";
            // 
            // labelRepAccountHost
            // 
            this.labelRepAccountHost.AutoSize = true;
            this.labelRepAccountHost.Location = new System.Drawing.Point(36, 37);
            this.labelRepAccountHost.Name = "labelRepAccountHost";
            this.labelRepAccountHost.Size = new System.Drawing.Size(29, 13);
            this.labelRepAccountHost.TabIndex = 6;
            this.labelRepAccountHost.Text = "Host";
            // 
            // labelRepAccountLogin
            // 
            this.labelRepAccountLogin.AutoSize = true;
            this.labelRepAccountLogin.Location = new System.Drawing.Point(32, 88);
            this.labelRepAccountLogin.Name = "labelRepAccountLogin";
            this.labelRepAccountLogin.Size = new System.Drawing.Size(33, 13);
            this.labelRepAccountLogin.TabIndex = 7;
            this.labelRepAccountLogin.Text = "Login";
            // 
            // labelRepAccountPassword
            // 
            this.labelRepAccountPassword.AutoSize = true;
            this.labelRepAccountPassword.Location = new System.Drawing.Point(12, 144);
            this.labelRepAccountPassword.Name = "labelRepAccountPassword";
            this.labelRepAccountPassword.Size = new System.Drawing.Size(53, 13);
            this.labelRepAccountPassword.TabIndex = 8;
            this.labelRepAccountPassword.Text = "Password";
            // 
            // RepoAccountInfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(385, 233);
            this.Controls.Add(this.labelRepAccountPassword);
            this.Controls.Add(this.labelRepAccountLogin);
            this.Controls.Add(this.labelRepAccountHost);
            this.Controls.Add(this.labelRepoAccountPort);
            this.Controls.Add(this.textBoxRepoAccountPort);
            this.Controls.Add(this.buttonRepoAccountAdd);
            this.Controls.Add(this.maskedTextBoxRepoAccountPassword);
            this.Controls.Add(this.textBoxRepoAccountLogin);
            this.Controls.Add(this.textBoxRepoAccountHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RepoAccountInfoForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Repository (FTP) Account";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxRepoAccountHost;
        private System.Windows.Forms.TextBox textBoxRepoAccountLogin;
        private System.Windows.Forms.MaskedTextBox maskedTextBoxRepoAccountPassword;
        private System.Windows.Forms.Button buttonRepoAccountAdd;
        private System.Windows.Forms.TextBox textBoxRepoAccountPort;
        private System.Windows.Forms.Label labelRepoAccountPort;
        private System.Windows.Forms.Label labelRepAccountHost;
        private System.Windows.Forms.Label labelRepAccountLogin;
        private System.Windows.Forms.Label labelRepAccountPassword;
    }
}