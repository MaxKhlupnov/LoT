namespace HomeOS.Hub.Tools.UpdateManager
{
    partial class AzureAccountForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AzureAccountForm));
            this.labelAzureAccountName = new System.Windows.Forms.Label();
            this.textBoxAzureAccountName = new System.Windows.Forms.MaskedTextBox();
            this.labelAzureAccountKey = new System.Windows.Forms.Label();
            this.textBoxAzureAccountKey = new System.Windows.Forms.MaskedTextBox();
            this.buttonAzureAccountAdd = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelAzureAccountName
            // 
            this.labelAzureAccountName.AutoSize = true;
            this.labelAzureAccountName.Location = new System.Drawing.Point(26, 31);
            this.labelAzureAccountName.Name = "labelAzureAccountName";
            this.labelAzureAccountName.Size = new System.Drawing.Size(78, 13);
            this.labelAzureAccountName.TabIndex = 0;
            this.labelAzureAccountName.Text = "Account Name";
            // 
            // textBoxAzureAccountName
            // 
            this.textBoxAzureAccountName.Location = new System.Drawing.Point(110, 28);
            this.textBoxAzureAccountName.Name = "textBoxAzureAccountName";
            this.textBoxAzureAccountName.Size = new System.Drawing.Size(197, 20);
            this.textBoxAzureAccountName.TabIndex = 1;
            // 
            // labelAzureAccountKey
            // 
            this.labelAzureAccountKey.AutoSize = true;
            this.labelAzureAccountKey.Location = new System.Drawing.Point(36, 74);
            this.labelAzureAccountKey.Name = "labelAzureAccountKey";
            this.labelAzureAccountKey.Size = new System.Drawing.Size(68, 13);
            this.labelAzureAccountKey.TabIndex = 2;
            this.labelAzureAccountKey.Text = "Account Key";
            // 
            // textBoxAzureAccountKey
            // 
            this.textBoxAzureAccountKey.Location = new System.Drawing.Point(110, 71);
            this.textBoxAzureAccountKey.Name = "textBoxAzureAccountKey";
            this.textBoxAzureAccountKey.Size = new System.Drawing.Size(197, 20);
            this.textBoxAzureAccountKey.TabIndex = 3;
            // 
            // buttonAzureAccountAdd
            // 
            this.buttonAzureAccountAdd.Location = new System.Drawing.Point(232, 142);
            this.buttonAzureAccountAdd.Name = "buttonAzureAccountAdd";
            this.buttonAzureAccountAdd.Size = new System.Drawing.Size(75, 23);
            this.buttonAzureAccountAdd.TabIndex = 4;
            this.buttonAzureAccountAdd.Text = "Update";
            this.buttonAzureAccountAdd.UseVisualStyleBackColor = true;
            this.buttonAzureAccountAdd.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonAzureAccountAdd.Click += new System.EventHandler(this.buttonAzureAccountAdd_Click);
            // 
            // AzureAccountForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(338, 177);
            this.Controls.Add(this.buttonAzureAccountAdd);
            this.Controls.Add(this.textBoxAzureAccountKey);
            this.Controls.Add(this.labelAzureAccountKey);
            this.Controls.Add(this.textBoxAzureAccountName);
            this.Controls.Add(this.labelAzureAccountName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AzureAccountForm";
            this.Text = "Azure Storage Account";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelAzureAccountName;
        private System.Windows.Forms.MaskedTextBox textBoxAzureAccountName;
        private System.Windows.Forms.Label labelAzureAccountKey;
        private System.Windows.Forms.MaskedTextBox textBoxAzureAccountKey;
        private System.Windows.Forms.Button buttonAzureAccountAdd;
    }
}