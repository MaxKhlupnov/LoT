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
    public class NaivePreHeat : IPreHeat
    {
        protected string dataFilePath;
        protected StrKey occupancyKey;
        protected Random random;
        protected int constK;
        protected double threshold = 0.5;
        protected StreamWriter results;

        protected string outputFilePath;

        protected int slotIndexBase = 1262304000;
        public NaivePreHeat(string dataFilePath, int K, string outputFilePath)
        {
            this.dataFilePath = dataFilePath;
            occupancyKey = new StrKey("occupancy");
            random = new Random();
            this.constK = K;
            this.outputFilePath = outputFilePath;
        }


        public List<RetVal> PredictOccupancy(long startSlotIndex, long endSlotIndex)
        {
            List<RetVal> retVal = new List<RetVal>();
            System.IO.StreamReader datafile=null;
            
            if (dataFilePath != null) //assuming datafile has one occupancy value per line read to startSlotIndex
            {
                string line;
                int counter = 0;
                datafile = new System.IO.StreamReader(this.dataFilePath);
                if (startSlotIndex != 0)
                {
                    while ((line = datafile.ReadLine()) != null)
                    {
                        if (counter == startSlotIndex)
                            break;
                        counter++;
                    }
                }
            }

            StreamFactory streamFactory = StreamFactory.Instance;

            FqStreamID fq_sid = new FqStreamID("preheatnaive", "A", "TestBS");
            CallerInfo ci = new CallerInfo(null, "A", "A", 1);
            streamFactory.deleteStream(fq_sid, ci);
            IStream occupancyGroundTruthStream = streamFactory.openValueDataStream<StrKey, ByteValue>(fq_sid, ci, null, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Write, null, 4*1024*1024, 1, new Logger());
            
            int slotIndex = 0; long startTime, retrievelTime, computeTime, appendTime;

            while (true)
            {
                startTime = DateTime.Now.Ticks;
                List<int> currentPOV = ConstructCurrentPOV(occupancyGroundTruthStream, slotIndex);
                List<List<int>> previousDaysPOV = ConstructPreviousPOV(occupancyGroundTruthStream, slotIndex);
                retrievelTime = DateTime.Now.Ticks - startTime;

                startTime = DateTime.Now.Ticks;
                int predictedOccupancy = Predict(currentPOV, previousDaysPOV);

                computeTime = DateTime.Now.Ticks - startTime;

                startTime = DateTime.Now.Ticks;
                if (datafile == null) // if no datafile to read the ground truth from just append randomly
                    occupancyGroundTruthStream.Append(occupancyKey, new ByteValue(BitConverter.GetBytes(random.Next(2))), slotIndexBase+ slotIndex);
                else
                {
                    string line = datafile.ReadLine();
                    if (line == null)
                    {
                        Console.WriteLine("reached the end of datafile");
                        break;
                    }
                    occupancyGroundTruthStream.Append(occupancyKey, new ByteValue(StreamFactory.GetBytes(line)), slotIndexBase + slotIndex);

                }
                slotIndex++;
                appendTime = DateTime.Now.Ticks - startTime;

                Console.WriteLine("Slot number {0} {1} {2} {3}", slotIndex, retrievelTime, computeTime, appendTime);
                using (results = File.AppendText(outputFilePath))
                    results.WriteLine("Slot number {0} {1} {2} {3}", slotIndex, retrievelTime, computeTime, appendTime);
                //retVal.Add(new RetVal(endTime - startTime, predictedOccupancy));

                if (slotIndex == endSlotIndex)
                    break;
            }
            occupancyGroundTruthStream.Close();
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
