using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


using HomeOS.Hub.Common.Bolt.DataStore;

namespace HomeOS.Hub.Common.Bolt.Tools.DataExportUI
{
    class ExportUI
    {
       

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DataExportUI());
        }

         public ExportUI()
         {
             ;
         }
         public void ExportData(bool remote, DateTime dtbegin, DateTime dtend, String outputFileName)
        {
             //read the settings
            string accountName = ConfigurationManager.AppSettings.Get("AccountName");
            string accountKey = ConfigurationManager.AppSettings.Get("AccountSharedKey");
            string homeId = ConfigurationManager.AppSettings.Get("HomeId");
            string appId = ConfigurationManager.AppSettings.Get("AppId");
            string streamId = ConfigurationManager.AppSettings.Get("StreamId");

            IStream datastream;
            FileStream fs = new FileStream(outputFileName, FileMode.Append);
            StreamWriter swOut = new StreamWriter(fs);

           StreamFactory sf = StreamFactory.Instance;

            CallerInfo ci = new CallerInfo(null, appId, appId, 0);
            FqStreamID fq_sid = new FqStreamID(homeId, appId, streamId);
            if (remote)
            {
                LocationInfo li = new LocationInfo(accountName, accountKey, SynchronizerType.Azure);
                datastream = sf.openValueDataStream<StrKey, StrValue>
                    (fq_sid, ci, li, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Read,
                    null, 4*1024*1024, 1, null, true);
            }
            else
            {
                datastream = sf.openValueDataStream<StrKey, StrValue>
                    (fq_sid, ci, null, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Read, 
                     null,  4*1024*1024, 1, null);
            }

         
            DateTime dtbeginutc = dtbegin.ToUniversalTime();
            DateTime dtendutc = dtend.ToUniversalTime();

//            StrKey tmpKey = new StrKey("envih1:sensormultilevel:");
    
            HashSet<IKey> keys = datastream.GetKeys(null, null);
            foreach (IKey key in keys)
            {
               // IEnumerable<IDataItem> dataItemEnum = datastream.GetAll(key);
               //                                                dtendutc.Ticks);
                IEnumerable<IDataItem> dataItemEnum = datastream.GetAll(key,
                                                                            dtbeginutc.Ticks,
                                                                            dtendutc.Ticks);
                if (dataItemEnum != null)
                {
                    foreach (IDataItem di in dataItemEnum)
                    {
                        DateTime ts = new DateTime(di.GetTimestamp());
                        swOut.WriteLine(key + ", " + ts.ToLocalTime() + ", " + di.GetVal().ToString());
                    }
                }
           }

            datastream.Close();
            swOut.Close();
        }
    }
}

