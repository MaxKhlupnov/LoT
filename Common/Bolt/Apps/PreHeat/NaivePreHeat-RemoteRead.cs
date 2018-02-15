using HomeOS.Hub.Common.Bolt.DataStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.PreHeat
{
    /// <summary>
    /// This class implements the Preheat in HDS in a naive way. storing only occupany values in a byte stream.
    /// </summary>
    public class NaivePreHeat_remoteread : IPreHeat
    {
        protected string dataFilePath;
        protected StrKey occupancyKey;
        protected Random random;
        protected int constK;
        protected double threshold = 0.5;
        protected StreamWriter results;

        protected string outputFilePath;

        protected int slotIndexBase = 1262304000;
        int chunkSize = 0;
        string mdserver;
        LocationInfo li;
        string fqsidprefix = "naivepreheatcameraready4ray"; 
        public NaivePreHeat_remoteread(string dataFilePath, int K, string outputFilePath, int chunkSize, string mdserver, LocationInfo li, int endSlotIndex )
        {
            this.dataFilePath = dataFilePath;
            occupancyKey = new StrKey("occupancy");
            random = new Random();
            this.constK = K;
            this.outputFilePath = outputFilePath;
            this.chunkSize = chunkSize;
            this.mdserver = mdserver;
            this.li = li;

            int slotIndex = 0;
            StreamFactory streamFactory = StreamFactory.Instance;
            FqStreamID fq_sid = new FqStreamID(fqsidprefix + chunkSize, "A", "TestBS");
            CallerInfo ci = new CallerInfo(null, "A", "A", 1);
            streamFactory.deleteStream(fq_sid, ci);
            IStream occupancyGroundTruthStream = streamFactory.openValueDataStream<StrKey, ByteValue>(fq_sid, ci, li, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Write, mdserver, chunkSize, 1, new Logger());
            while (true)
            {
                occupancyGroundTruthStream.Append(occupancyKey, new ByteValue(BitConverter.GetBytes(random.Next(2))), slotIndexBase + slotIndex);
                slotIndex++;
                if (slotIndex == endSlotIndex)
                    break;
            }
            occupancyGroundTruthStream.Close();
            

        }


        public List<RetVal> PredictOccupancy(long startSlotIndex, long endSlotIndex)
        {
            long average = 0;

            List<RetVal> retVal = new List<RetVal>();
            
            StreamFactory streamFactory = StreamFactory.Instance;
            FqStreamID fq_sid = new FqStreamID(fqsidprefix + chunkSize, "A", "TestBS");
            CallerInfo ci = new CallerInfo(null, "A", "A", 1);
            Directory.Delete(fq_sid.HomeId, true);

            int slotIndex = Convert.ToInt32(startSlotIndex); long startTime = 0, retrievelTime = 0, computeTime = 0;
            IStream occupancyGroundTruthStream = streamFactory.openValueDataStream<StrKey, ByteValue>(fq_sid, ci, li, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Read, mdserver, chunkSize, 1, new Logger());
            while (true)
            {

                List<int> currentPOV= new List<int>();
                List<List<int>> previousDaysPOV= new List<List<int>>();
              
                try
                {
                    startTime = DateTime.Now.Ticks;
                    
                    currentPOV = ConstructCurrentPOV(occupancyGroundTruthStream, slotIndex);
                    previousDaysPOV = ConstructPreviousPOV(occupancyGroundTruthStream, slotIndex);
                    retrievelTime = DateTime.Now.Ticks - startTime;

                    startTime = DateTime.Now.Ticks;
                    int predictedOccupancy = Predict(currentPOV, previousDaysPOV);
                    computeTime = DateTime.Now.Ticks - startTime;   
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
               

                Console.WriteLine("Slot number {0} {1} ", slotIndex, retrievelTime);
                using (results = File.AppendText(outputFilePath))
                    results.WriteLine("Slot number {0} {1}", slotIndex, retrievelTime);
                average += retrievelTime;

                slotIndex++;
                if (slotIndex == endSlotIndex)
                    break;
            }
            occupancyGroundTruthStream.Close();
            average = average / (endSlotIndex - startSlotIndex + 1) ;

            retVal.Add(new RetVal(0,Convert.ToInt32(average)));
            return retVal;
        }







        public int Predict(List<int> currentPOV, List<List<int>> previousDaysPOV)
        {
            List<int> hammingDistances = new List<int>();
            int hammingDistance;

            if (previousDaysPOV.Count < this.constK)
                return 1;

            for (int i=0 ; i < previousDaysPOV.Count ; i++)
            {
                List<int> previousPOV = previousDaysPOV.ElementAt(i);
                hammingDistance = ComputeHammingDistance(currentPOV, previousPOV);
                hammingDistances.Add(hammingDistance);
            }

            List<int> minHammingDistanceDays = new List<int>();

            for (int i = 1; i <= this.constK; i++)
            {
                int min = hammingDistances.Min();// if there are multiple days with min hamming distance this just chooses the first in the list.
                minHammingDistanceDays.Add(hammingDistances.IndexOf(min));
                hammingDistances.Remove(min);
            }

            double occupancySum = 0; 
            foreach (int i in minHammingDistanceDays)
            {
                occupancySum += previousDaysPOV.ElementAt(i).Last();
            }

            occupancySum = occupancySum / this.constK;
            if (occupancySum >= threshold)
                return 1;
            else
                return 0;
        }

        public int ComputeHammingDistance(List<int> v1, List<int> v2)
        {
            int ret = 0;

            for (int i = 0; i < v1.Count; i++)
            {
                if (!v1.ElementAt(i).Equals(v2.ElementAt(i)))
                    ret++;
            }
            return ret;
        }

        public List<int> ConstructCurrentPOV(IStream stream, int slotIndex)
        {
            List<int> retVal = new List<int>();

            IEnumerable<IDataItem> dataItems = stream.GetAll(this.occupancyKey, slotIndexBase + 96 * (int)(slotIndex / 96), slotIndexBase + (slotIndex % 96) + 96 * (int)(slotIndex / 96));
            if (dataItems != null)
            {
                foreach (IDataItem data in dataItems)
                {
                    retVal.Add(ConvertLittleEndian(data.GetVal().GetBytes()));
                }
            }
            return retVal;
        }

        public List<List<int>> ConstructPreviousPOV(IStream stream, int slotIndex)
        {
            List<List<int>> retVal = new List<List<int>>();

            int numberOfPreviousDays = slotIndex / 96;
            
            for (int i = 0; i < numberOfPreviousDays; i++)
            {
                IEnumerable<IDataItem> dataItems = stream.GetAll(this.occupancyKey, slotIndexBase + 96 * i, slotIndexBase + (slotIndex % 96) + 96 * i);
                if (dataItems != null)
                {
                    List<int> dayPOV = new List<int>();
                    foreach (IDataItem data in dataItems)
                    {
                        dayPOV.Add(ConvertLittleEndian(data.GetVal().GetBytes()));
                    }
                    retVal.Add(dayPOV);
                }
            }
            return retVal;
        }

        public int ConvertLittleEndian(byte[] array)
        {
            int pos = 0;
            int result = 0;
            foreach (byte by in array)
            {
                result |= (int)(by << pos);
                pos += 8;
            }
            return result;
        }



    }
}
