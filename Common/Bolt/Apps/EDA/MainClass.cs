using HomeOS.Hub.Common.Bolt.DataStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.EDA
{
    class MainClass
    {
        public static string mdServer="http://scspc417.cs.uwaterloo.ca:23456/TrustedServer/";
        public static string AzureaccountName = "testdrive";
        public static string AzureaccountKey = "zRTT++dVryOWXJyAM7NM0TuQcu0Y23BgCQfkt7xh2f/Mm+r6c8/XtPTY0xxaF6tPSACJiuACsjotDeNIVyXM8Q==";

        private static void UploadDataAsStreams(int UploadCount)
        {
            string directory = @"..\\..\\data\\meter-data";
            int count = 0;

            Dictionary<DateTime, double> ts_temperature = new Dictionary<DateTime, double>();
            StreamReader wfile = new System.IO.StreamReader(@"..\\..\\data\\weather.txt");
            string wline;
            while ((wline = wfile.ReadLine()) != null)
            {
                string[] words = wline.Split('\t');
                DateTime date = Convert.ToDateTime(words[4]);
                date = date.AddHours(Int32.Parse(words[5]));
                double temperature = Double.Parse(words[0]);
                ts_temperature[date] = temperature;
            }
            wfile.Close();


            foreach (string filePath in Directory.GetFiles(directory))
            {
                Console.WriteLine("file name:" + filePath);
                string line;
                System.IO.StreamReader file =   new System.IO.StreamReader(filePath);

                LocationInfo li = new LocationInfo(AzureaccountName, AzureaccountKey, SynchronizerType.Azure);

                FqStreamID fq_sid = new FqStreamID("crrealhome"+count, "A", "TestBS");
                CallerInfo ci = new CallerInfo(null, "A", "A", 1);

                StreamFactory sf = StreamFactory.Instance;
                sf.deleteStream(fq_sid, ci);
                IStream dfs_byte_val = sf.openValueDataStream<DoubleKey, ByteValue>(fq_sid, ci, li, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Write, mdserveraddress: mdServer, ChunkSizeForUpload: 4 * 1024 * 1024, ThreadPoolSize: 1, log: new Logger());

                
                while ((line = file.ReadLine()) != null)
                {
                    string[] words = line.Split('\t');
                    DateTime date = Convert.ToDateTime(words[0]);
                    date=date.AddHours(int.Parse(words[1])/100);
                    DoubleKey key = new DoubleKey(((int)(ts_temperature[date])));
                    dfs_byte_val.Append(key, new ByteValue(BitConverter.GetBytes(Double.Parse(words[2]))), DateTimeToUnixTimestamp(date));
                   // Console.WriteLine(DateTimeToUnixTimestamp(date) + "," + words[2]);
                }

                dfs_byte_val.Close();
                count++;
                if (count == UploadCount) 
                    break;
            }
        }

        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (long)(dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }


        private static long RemoteRead(int numberOfHomes, DateTime start, DateTime end, string tag)
        {
            Dictionary<int, List<double>> temp_energy_allhomes= new Dictionary<int,List<double>>();
            Dictionary<int, List<double>> temp_energy_home;
            long retVal=0;

                for(int i = 0 ; i <numberOfHomes ; i++)
                {
                    temp_energy_home = new Dictionary<int,List<double>>();

                    

                    LocationInfo li = new LocationInfo(AzureaccountName, AzureaccountKey, SynchronizerType.Azure);
                    FqStreamID fq_sid = new FqStreamID("crrealhome" + i, "A", "TestBS");
                    CallerInfo ci = new CallerInfo(null, "A", "A", 1);
                    StreamFactory sf = StreamFactory.Instance;
                        

                    IStream dfs_byte_val = sf.openValueDataStream<DoubleKey, ByteValue>(fq_sid, ci, li, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Read, mdServer, 4 * 1024 * 1024, 1, new Logger());
                    
                    long start_ticks = DateTime.Now.Ticks;
                    for (int temp = -30; temp <= 40; temp++)
                    {
                        IEnumerable<IDataItem> vals = dfs_byte_val.GetAll(new DoubleKey(temp), DateTimeToUnixTimestamp(start), DateTimeToUnixTimestamp(end));
                        if (vals != null)
                        {
                            foreach (IDataItem val in vals)
                            {
                                if (!temp_energy_home.ContainsKey(temp)) 
                                    temp_energy_home[temp] = new List<double>();

                                if (!temp_energy_allhomes.ContainsKey(temp))
                                    temp_energy_allhomes[temp] = new List<double>();

                                temp_energy_home[temp].Add(BitConverter.ToDouble(val.GetVal().GetBytes(), 0));
                                temp_energy_allhomes[temp].Add(BitConverter.ToDouble(val.GetVal().GetBytes(), 0));
                            }
                        }
                        
                    }
                    dfs_byte_val.Close();
                    long end_ticks = DateTime.Now.Ticks;
                    retVal+=end_ticks - start_ticks;

                    WriteToFile(".\\result-realhome-" + i + "-n-" + numberOfHomes + "-" + tag, temp_energy_home);
                }
                WriteToFile(".\\result-allhomes-n-" + numberOfHomes + "-" + tag, temp_energy_allhomes);
                return retVal;
        }

        private static void WriteToFile(string filePath, Dictionary<int, List<double>> v)
        {
            StreamWriter writer = File.AppendText(filePath);
            foreach(int t in v.Keys)
            {
                List<double> tempList = v[t];
                tempList.Sort();
                writer.WriteLine(t+","+ tempList.ElementAt(tempList.Count/2));   
            }
            writer.Close();
        }


        static void Main(string[] args)
        {

           // MainClass.UploadDataAsStreams(100);
            int[] n_homes = {1,10, 100};
            DateTime[] starts = { new DateTime(2011, 05, 01), new DateTime(2011,05, 01), new DateTime(2011, 05, 01) };
            DateTime[] ends = { new DateTime(2011, 05, 31), new DateTime(2011, 10, 31), new DateTime(2012, 04, 30) };
            string[] tags={"1 Month", "6 Months", "1 Year" };

            for (int i = 0; i < n_homes.Length; i++)
            {
                for (int j = 0; j < starts.Length; j++)
                {
                    List<double> values = new List<double>();
                    for (int k = 1; k <= 10; k++)
                    {
                        long ret = RemoteRead(n_homes[i], starts[j], ends[j], tags[j]);
                        values.Add(ret);
                        Console.WriteLine(n_homes[i] + "," + tags[j] + "," + ret);
                    }

                    using (StreamWriter w = File.AppendText("./timing-n-"+n_homes[i]+".txt"))
                    {
                        w.WriteLine(tags[j]+ ","+values.Mean()+","+values.StandardDeviation());
                    }
                    

                }
            }

            /*
            string dataFile = "..\\..\\data\\home49918-energy-temperature.txt";
            int[] widths = {24*7, 30*24, 24*365};

            foreach (int width in widths)
            {
                RowlandsAnalysis rowland = new RowlandsAnalysis(dataFile, width);
                
                List<Tuple<double, double, double, double>> retval = new List<Tuple<double, double, double, double>>();
                for (int i = 1; i <= rowland.GetDataLength(); i++)
                {
                    rowland.ReadHourIntoStream();
                    retval = rowland.PerformAnalysis();
                }

                rowland.Finish();
                using (StreamWriter w = File.AppendText(".\\output-width-"+width+".txt"))
                {
                    foreach (Tuple<double, double, double, double> t in retval)
                        w.WriteLine(t.Item1.ToString() + "," + t.Item2.ToString() + "," + t.Item3.ToString() + "," + t.Item4.ToString());
                }
            }*/
        }
    }
}



/*

string line;
            System.IO.StreamReader file = new System.IO.StreamReader(@"..\\..\\m49918.txt");

            DateTime prev = DateTime.Now;

            List<Tuple<DateTime, double, double>> ts_energy_temp = new List<Tuple<DateTime, double, double>>();
            Dictionary<DateTime, double> ts_temperature = new Dictionary<DateTime,double>();

            while ((line = file.ReadLine()) != null)
            {
                string[] words = line.Split('\t');
                DateTime date = Convert.ToDateTime(words[0]);
                date = date.AddHours(Int32.Parse(words[1]) / 100);
                double energy = Double.Parse(words[2]);
                ts_energy_temp.Add(new Tuple<DateTime, double, double>(date, energy,double.MinValue ));
            }
            file.Close();

            file = new System.IO.StreamReader(@"..\\..\\weather.txt");
            
            while ((line = file.ReadLine()) != null)
            {
                string[] words = line.Split('\t');
                DateTime date = Convert.ToDateTime(words[4]);
                date = date.AddHours(Int32.Parse(words[5]));
                double temperature = Double.Parse(words[0]);
                ts_temperature[date]= temperature;
            }
            file.Close();


            StreamWriter w = File.AppendText("..\\..\\home49918-energy-temperature.txt");
            foreach (Tuple<DateTime, double, double> t in ts_energy_temp)
            {
                DateTime ts = t.Item1;
                double energy = t.Item2;
                double temp = 0;
                if (ts_temperature.ContainsKey(ts))
                    temp = ts_temperature[ts];
                else
                    throw new Exception("wtf");

                Console.WriteLine(ts + "," + energy + "," + temp);
                w.WriteLine(ts + "," + energy + "," + temp);
            }

*/