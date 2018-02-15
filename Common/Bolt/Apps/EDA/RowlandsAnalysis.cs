using HomeOS.Hub.Common.Bolt.DataStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.EDA
{
    class RowlandsAnalysis
    {

        private string dataFilePath;
        List<Tuple<DateTime, double, double>> timestamp_energy_temperature;
        private int count; 
        IStream dataStream;

        private int width = 7*24 ;// hours
        StreamWriter results;

        public RowlandsAnalysis(string dataFilePath, int width)
        {
            this.width = width;
            this.dataFilePath = dataFilePath;
            count = 0 ; 
            timestamp_energy_temperature = new List<Tuple<DateTime, double, double>>();

            File.Delete(".\\result-width-" + width + ".txt");
            

            StreamFactory streamFactory = StreamFactory.Instance;

            FqStreamID fq_sid = new FqStreamID("RowlandsAnalysis"+ width, "A", "TestBS");
            CallerInfo ci = new CallerInfo(null, "A", "A", 1);
            streamFactory.deleteStream(fq_sid, ci);
            dataStream = streamFactory.openValueDataStream<DoubleKey, ByteValue>(fq_sid, ci, null, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Write, mdserveraddress: null, ChunkSizeForUpload: 4 * 1024 * 1024, ThreadPoolSize: 1, log: new Logger());
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(dataFilePath);
            while ((line = file.ReadLine()) != null)
            {
                string[] words = line.Split(',');
                DateTime date = Convert.ToDateTime(words[0]);
                double energy = Double.Parse(words[1]);
                double temperature = Double.Parse(words[2]);
                timestamp_energy_temperature.Add(new Tuple<DateTime, double, double>(date, energy, temperature));
            }
            file.Close();
        }

        public void ReadHourIntoStream()
        {
            long start = DateTime.Now.Ticks;
            Tuple<DateTime, double, double> tuple = timestamp_energy_temperature.ElementAt(count);
            DoubleKey key = new DoubleKey(Math.Round(tuple.Item3));
            dataStream.Append(key, new ByteValue(BitConverter.GetBytes(tuple.Item2)),(long)count);
            count++;
            long end = DateTime.Now.Ticks;
            Console.Write(count + "," + (end - start));
            
            using (results = File.AppendText(".\\result-width-" + width + ".txt"))
                results.Write(count + "," + (end - start));
        }

        public int GetDataLength()
        {
            return timestamp_energy_temperature.Count;
        }



        public List<Tuple<double, double, double, double>> PerformAnalysis()
        {

            long computeTime = 0, retrievalTime = 0;

            long start = DateTime.Now.Ticks;
            HashSet<IKey> keySet = dataStream.GetKeys(new DoubleKey(double.MinValue), new DoubleKey(double.MaxValue));
            long end = DateTime.Now.Ticks;
            retrievalTime += end - start;

            List<IKey> keys = keySet.ToList();
            keys.Sort();

            List<Tuple<double, double, double, double>> retVal = new List<Tuple<double, double, double, double>>();

            foreach (IKey key in keys)
            {
                start = DateTime.Now.Ticks;
                IEnumerable<IDataItem> datalist = dataStream.GetAll(key, count - this.width, count - 1);
                List<double> energy = new List<double>();
                foreach (IDataItem data in datalist)
                    energy.Add(BitConverter.ToDouble(data.GetVal().GetBytes(), 0));
                end = DateTime.Now.Ticks;
                retrievalTime += end - start;


                start = DateTime.Now.Ticks;
                energy.Sort();
                double tenthPercentile=0;
                double ninetyPercentile=0;
                double median=0;

                if (energy.Count != 0)
                {
                    tenthPercentile = energy.ElementAt(energy.Count / 10);
                    ninetyPercentile = energy.ElementAt((int)(0.9 * energy.Count));
                    median = energy.ElementAt(energy.Count / 2);
                }
                retVal.Add(new Tuple<double,double,double,double>(BitConverter.ToDouble(key.GetBytes(), 0), tenthPercentile, median, ninetyPercentile));
                end = DateTime.Now.Ticks;
                computeTime += end - start;
            }

            Console.WriteLine("," + retrievalTime + "," + computeTime);
            using (results = File.AppendText(".\\result-width-"+width+".txt"))
                results.WriteLine("," + retrievalTime + "," + computeTime);
            return retVal;
        }

        public void Finish()
        {
            this.dataStream.Close();
        }
        
    }
}
