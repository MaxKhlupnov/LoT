using HomeOS.Hub.Common.Bolt.DataStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.DNW
{
    class DNW
    {
        private int numberOfStreams;
        private int window;
        List<IStream> dataStreams;
        int count = 0;
        public string[] types = { "human", "animal", "car","bike" };
        StreamWriter writer;
        string outputFilePath;

        public DNW(int numberOfStreams, int window, string outputFilePath)
        {
            this.numberOfStreams = numberOfStreams;
            this.window = window;
            this.count = 0;
            this.outputFilePath = outputFilePath;
            StreamFactory streamFactory = StreamFactory.Instance;

            FqStreamID fq_sid = new FqStreamID("dnw-"+window+"-"+numberOfStreams, "A", "TestBS");
            CallerInfo ci = new CallerInfo(null, "A", "A", 1);
            streamFactory.deleteStream(fq_sid, ci);
            dataStreams = new List<IStream>();

            for(int i=1 ; i <= numberOfStreams ; i++)
                dataStreams.Add(streamFactory.openFileStream<StrKey, ByteValue>(fq_sid, ci, null, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Write, null, 4 * 1024 * 1024, 1, new Logger()));

        }

        public void ReadObject()
        {
            long start = DateTime.Now.Ticks;
            for (int i = 0; i < numberOfStreams; i++)
            {
                dataStreams.ElementAt(i).Append(new StrKey("ID"), new ByteValue(BitConverter.GetBytes(count)), count);
                dataStreams.ElementAt(i).Append(new StrKey("Type"), new ByteValue(StreamFactory.GetBytes(types[  count%types.Length])), count);
                dataStreams.ElementAt(i).Append(new StrKey("EnterTimestamp"), new ByteValue(BitConverter.GetBytes(94.00000)), count);
                dataStreams.ElementAt(i).Append(new StrKey("ExitTimestamp"), new ByteValue(BitConverter.GetBytes(231.00000)), count);
                dataStreams.ElementAt(i).Append(new StrKey("EntryArea"), new ByteValue(BitConverter.GetBytes(2)), count);
                dataStreams.ElementAt(i).Append(new StrKey("ExitArea"), new ByteValue(BitConverter.GetBytes(1)), count);
                List<int> l = new List<int>() {114,114,117,121,134,133,141,115,136,129,127,120,138,130,128,126,121,129,123,127,120,121,121,131,132,117,136,126,131,127,133,133,129,128,115,137,128,125,120,129,134,128,129,130,127,133,128,126,133,130,122,127,128,121,126,129,130,122,123,127,134,121,126,135,132,123,134,126,129,124,134,131,130,124 };        
                dataStreams.ElementAt(i).Append(new StrKey("SMPC"), new ByteValue(l.SelectMany(BitConverter.GetBytes).ToArray()), count);
            }
            long time = DateTime.Now.Ticks - start;
            using (writer = File.AppendText(outputFilePath))
                writer.Write(count+","+time);
            Console.Write(count + "," + time);
            count++;
        }

        public int NumberOfMatches(List<int> targetSMPCvector)
        {
            long start = DateTime.Now.Ticks;
            for (int i = 0; i < numberOfStreams; i++)
            {
                IEnumerable<IDataItem> objects = dataStreams.ElementAt(i).GetAll(new StrKey("SMPC"), count - window , count);
                foreach (IDataItem item in objects)
                {
                    List<int> recObject = Enumerable.Range(0, item.GetVal().GetBytes().Length / 4).Select(j => BitConverter.ToInt32(item.GetVal().GetBytes(), j * 4)).ToList();
                    IsMatch(targetSMPCvector, recObject);
                }

            }
            long time = DateTime.Now.Ticks - start;
            using (writer = File.AppendText(outputFilePath))
                writer.WriteLine("," + time);
            Console.WriteLine("," + time);
            return 0;
        }

        private bool IsMatch(List<int> obj1, List<int> obj2)
        {

            return true;
        }

        public void Finish()
        {
            writer.Close();
            foreach (IStream stream in dataStreams)
                stream.Close();
        }

    }
}
