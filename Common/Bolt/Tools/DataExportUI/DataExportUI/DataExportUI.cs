using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Configuration;

namespace HomeOS.Hub.Common.Bolt.Tools.DataExportUI
{
    public partial class DataExportUI : Form
    {
        ExportUI uiE;

        public DataExportUI()
        {
            InitializeComponent();
            uiE = new ExportUI();
        }

        private void DataExportUI_Load(object sender, EventArgs e)
        {
            accountName.Text = ConfigurationManager.AppSettings.Get("AccountName");
            accountKey.Text = ConfigurationManager.AppSettings.Get("AccountSharedKey");
            homeID.Text = ConfigurationManager.AppSettings.Get("HomeId");
            appID.Text = ConfigurationManager.AppSettings.Get("AppId");
            streamID.Text = ConfigurationManager.AppSettings.Get("StreamId");
        }

        private void button1_Click(object sender, EventArgs e)
        {

            //Write the settings to the Configuration in case they changed
            ConfigurationManager.AppSettings.Set("AccountName", accountName.Text);
            ConfigurationManager.AppSettings.Set("AccountSharedKey", accountKey.Text);
            ConfigurationManager.AppSettings.Set("HomeId", homeID.Text);
            ConfigurationManager.AppSettings.Set("AppId", appID.Text);
            ConfigurationManager.AppSettings.Set("StreamId", streamID.Text);
            
            //get the start and end dates
            DateTime beginDate = (startDate.Value).Date;
            DateTime stopDate = (endDate.Value).Date;

            InfoText.Text = "Exporting data";
            uiE.ExportData(true, beginDate, stopDate, outputFileName.Text);
            InfoText.Text = "Finished";

          
        }


      
    }
}
