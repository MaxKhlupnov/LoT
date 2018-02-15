using HomeOS.Hub.Common.Bolt.DataStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.PreHeat
{
    class SmartPreHeat : NaivePreHeat, IPreHeat
    {
        public SmartPreHeat(string dataFilePath, int K, string outputFilePath)
            : base(dataFilePath, K, outputFilePath)
        {

        }

        new public List<RetVal> PredictOccupancy(long startSlotIndex, long endSlotIndex)
        {
            List<RetVal> retVal = new List<RetVal>();
            System.IO.StreamReader datafile = null;

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

            FqStreamID fq_sid = new FqStreamID("smartpreheat", "A", "TestBS");
            CallerInfo ci = new CallerInfo(null, "A", "A", 1);
            streamFactory.deleteStream(fq_sid, ci);
            IStream occupancyGroundTruthStream = streamFactory.openValueDataStream<StrKey, ByteValue>(fq_sid, ci, null, StreamFactory.StreamSecurityType.Plain, CompressionType.None, StreamFactory.StreamOp.Write, null, 4 * 1024 * 1024, 1, new Logger());

            int slotIndex = 0; long startTime, retrievelTime, computeTime, insertTime;

            while (true)
            {
                startTime = DateTime.Now.Ticks;
                List<int> currentPOV = SmartConstructCurrentPOV(occupancyGroundTruthStream, slotIndex);
                List<List<int>> previousDaysPOV = SmartConstructPreviousPOV(occupancyGroundTruthStream, slotIndex);
                retrievelTime = DateTime.Now.Ticks - startTime;

                startTime = DateTime.Now.Ticks;
                int predictedOccupancy = Predict(currentPOV, previousDaysPOV);
                computeTime = DateTime.Now.Ticks - startTime;

                startTime = DateTime.Now.Ticks;
                int groundTruth; 
                if (datafile == null) // if no datafile to read the ground truth from just append randomly
                    groundTruth = random.Next(2);
                else
                {
                    string line = datafile.ReadLine();
                    groundTruth = int.Parse(line);
                }

                currentPOV.Add(groundTruth);
                List<int> temp = new List<int>();
                foreach (List<int> previousPOV in previousDaysPOV)
                {
                    temp = temp.Concat(previousPOV).ToList();
                }
                temp = temp.Concat(currentPOV).ToList();
                occupancyGroundTruthStream.Append(occupancyKey, new ByteValue(temp.SelectMany(BitConverter.GetBytes).ToArray()), slotIndexBase + slotIndex);

                insertTime = DateTime.Now.Ticks - startTime;
                Console.WriteLine("Slot number {0} {1} {2} {3}", slotIndex, retrievelTime, computeTime, insertTime);

                using (results = File.AppendText(outputFilePath))
                    results.WriteLine("Slot number {0} {1} {2} {3}", slotIndex, retrievelTime, computeTime, insertTime);

                slotIndex++;
                //retVal.Add(new RetVal(endTime - startTime, predictedOccupancy));

                if (slotIndex == endSlotIndex)
                    break;
            }
            occupancyGroundTruthStream.Close();
            return retVal;
        }


        public List<int> SmartConstructCurrentPOV(IStream stream, int slotIndex)
        {
            List<int> retVal = new List<int>();
            IValue val  = stream.Get(occupancyKey);
            if (val != null)
            {
                byte[] bytes = val.GetBytes();
                var listofPOVs = Enumerable.Range(0, bytes.Length / 4).Select(i => BitConverter.ToInt32(bytes, i * 4)).ToList();
                int width = slotIndex % 96;
                int ndays = slotIndex / 96; 
                retVal=listofPOVs.Skip(ndays*width).Take(width).ToList();
            }
            return retVal;
        }

        public List<List<int>> SmartConstructPreviousPOV(IStream stream, int slotIndex)
        {
            List<List<int>> retVal = new List<List<int>>();
            IEnumerable<IDataItem> dataItems = stream.GetAll(occupancyKey, slotIndexBase + slotIndex - 96, slotIndexBase + slotIndex - 96);
            slotIndex = slotIndex % 96; 
            if (dataItems != null && dataItems.Count() > 0)
            {
                byte[] bytes = dataItems.ElementAt(0).GetVal().GetBytes();
                var listofPOVs = Enumerable.Range(0, bytes.Length / 4).Select(i => BitConverter.ToInt32(bytes, i * 4)).ToList();

                for (int i = 0; i < listofPOVs.Count() / (slotIndex+1) ; i++)
                {
                    retVal.Add(listofPOVs.Skip(i * (slotIndex + 1)).Take(slotIndex + 1).ToList());
                }
            }
            return retVal;
        }
    }
}
