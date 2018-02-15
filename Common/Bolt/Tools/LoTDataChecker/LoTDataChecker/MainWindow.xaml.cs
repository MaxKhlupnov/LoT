using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using HomeOS.Hub.Common.Bolt.DataStore;
using System.Timers;

namespace HomeOS.Hub.Common.Bolt.Tools.LotDataChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ExportUI uiE;
        HashSet<IKey> keys;
        bool running;
        Timer data_pull_timer;

        public MainWindow()
        {
            InitializeComponent();
            accountName.Text = ConfigurationManager.AppSettings.Get("AccountName");
            accountKey.Text = ConfigurationManager.AppSettings.Get("AccountSharedKey");
            homeID.Text = ConfigurationManager.AppSettings.Get("HomeId");
            appID.Text = ConfigurationManager.AppSettings.Get("AppId");
            streamID.Text = ConfigurationManager.AppSettings.Get("StreamId");

            uiE = new ExportUI();
          
            data_pull_timer = new System.Timers.Timer(60 * 1000);
            data_pull_timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            data_pull_timer.Enabled = true;
            running = true;
        }
        protected override void OnClosed(EventArgs e)
        {
            running = false;
            data_pull_timer.Enabled = false;
            uiE.CloseDataStream(); //close the data stream connection if the form is closing.
            ConfigurationManager.AppSettings.Set("AccountName", accountName.Text);
            ConfigurationManager.AppSettings.Set("AccountSharedKey", accountKey.Text);
            ConfigurationManager.AppSettings.Set("HomeId", homeID.Text);
            ConfigurationManager.AppSettings.Set("AppId", appID.Text);
            ConfigurationManager.AppSettings.Set("StreamId", streamID.Text);
            base.OnClosed(e);
        }

        //private async void btnExportData_Click(object sender, RoutedEventArgs e)
        //{
        //   // btnExportData.IsEnabled = false;
        //    //Write the settings to the Configuration in case they changed
        //    ConfigurationManager.AppSettings.Set("AccountName", accountName.Text);
        //    ConfigurationManager.AppSettings.Set("AccountSharedKey", accountKey.Text);
        //    ConfigurationManager.AppSettings.Set("HomeId", homeID.Text);
        //    ConfigurationManager.AppSettings.Set("AppId", appID.Text);
        //    ConfigurationManager.AppSettings.Set("StreamId", streamID.Text);

        //    //get the start and end dates - ?? is a null-coalescing operator
        //    DateTime startDate = DateTime.Today;
        //    DateTime stopDate =  DateTime.Today;

        //    //if (startDate >= stopDate)
        //    //{
        //    //    infoText.Content = "End day must be after Start day";
        //    //  //  btnExportData.IsEnabled = true;
        //    //    return;
        //    //}

        //    if (uiE.GetDataStream() == null)
        //    {
        //        btnLoadStream_Click(sender, e);
        //    }

        //    infoText.Content = "Exporting data";

        //    if (keys == null)
        //    {
        //        infoText.Content = "No keys in the stream";
        //       // btnExportData.IsEnabled = true;
        //        return;
        //    }

        //    HashSet<IKey> selectedKeys = getSelectedKeys();
        //    await uiE.GetData(selectedKeys, startDate, stopDate, outputFileName.Text);
        //    infoText.Content = "Finished";

        // //  btnExportData.IsEnabled = true;
        //}


        HashSet<IKey> getSelectedKeys()
        {
            //Determine which keys are checked
            HashSet<IKey> selectedKeys = new HashSet<IKey>();

            //this is the items rather than their checked state - do super wacky stuff to get the Checkbox for the item
            for (int i = 0; i < keyList.Items.Count; i++)
            {
                //From: http://msdn.microsoft.com/en-us/library/bb613579(v=vs.110).aspx
                ListBoxItem myListBoxItem =
                (ListBoxItem)(keyList.ItemContainerGenerator.ContainerFromItem(keyList.Items[i]));
                if (myListBoxItem != null)
                {
                    ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(myListBoxItem);
                    DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                    CheckBox cBox = (CheckBox)myDataTemplate.FindName("cBox", myContentPresenter);

                    if (cBox.IsChecked != null && cBox.IsChecked == true)
                    {
                        selectedKeys.Add(new StrKey(cBox.Content.ToString()));
                    }
                }
            }
            return selectedKeys;
        }


        //http://msdn.microsoft.com/en-us/library/bb613579(v=vs.110).aspx
        private childItem FindVisualChild<childItem>(DependencyObject obj)
             where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private async void btnLoadStream_Click(object sender, RoutedEventArgs e)
        {
           
            keyList.DataContext = "";
            //setup the stream
            infoText.Content = "Loading data stream";
            await uiE.SetupDataStream(true, accountName.Text, accountKey.Text, homeID.Text, appID.Text, streamID.Text);

            //make a list of keys for this stream
            keys = uiE.GetKeys();
            keyList.DataContext = keys;
            infoText.Content = "Finished loading data stream";
           
            btnGetLatest.IsEnabled = true;
        }

        private void btnGetLatest_Click(object sender, RoutedEventArgs e)
        {
          
            getLatestHelper(uiE);
            
          
        }

        private Dictionary<IKey, Tuple<IValue, DateTime>> getLatestHelper(ExportUI exportUIObj)
        {
            HashSet<IKey> selectedKeys = getSelectedKeys();

            Dictionary<IKey, Tuple<IValue, DateTime>> latestVals = exportUIObj.GetLatest(selectedKeys);

            if (latestVals != null)
            {
                foreach (IKey tag in latestVals.Keys)
                {
                tbLatest.Text += tag + "," + latestVals[tag].Item1.ToString() + "," + latestVals[tag].Item2.ToString() + "\n";
            }
            }

          //  tbLatest.Text = latestVals.ToString();
           // btnExportData.IsEnabled = true;
            return latestVals;
        }

        private void checkRules(Dictionary<IKey, Tuple<IValue, DateTime>> latestValues)
        {
            // TODO: do something here
        }
      
        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (running)
            {
                // 1. open the datastream
                ExportUI exportUI = new ExportUI();
                exportUI.SetupDataStream(true, accountName.Text, accountKey.Text, homeID.Text, appID.Text, streamID.Text);
                // 2. get latest
                Dictionary<IKey, Tuple<IValue, DateTime>> latestVals =  getLatestHelper(exportUI);
                // 3. check rules
                checkRules(latestVals);
                // 4. close datastream
                exportUI.CloseDataStream();
            }
        }

    }
}
