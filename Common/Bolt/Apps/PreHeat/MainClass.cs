using HomeOS.Hub.Common.Bolt.Apps.Preheat;
using HomeOS.Hub.Common.Bolt.Apps.PreHeat;
using HomeOS.Hub.Common.Bolt.DataStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.PreHeat
{
    class MainClass
    {
        static void Main(string[] args)
        {
            int len =960 + 95;//11 days
            string outputFile;
            string mdServer = "http://scspc417.cs.uwaterloo.ca:23456/TrustedServer/";
            outputFile = ".\\optimal-remoteread";
            LocationInfo li = new LocationInfo("testdrive", "zRTT++dVryOWXJyAM7NM0TuQcu0Y23BgCQfkt7xh2f/Mm+r6c8/XtPTY0xxaF6tPSACJiuACsjotDeNIVyXM8Q==", SynchronizerType.Azure);

            int[] chunkSizes = {960, 480, 320,240,160,120,60,30,20,10};
            
            
            foreach (int chunk in chunkSizes)
            {
                NaivePreHeat_remoteread preheat = new NaivePreHeat_remoteread(null, 5, outputFile, chunk, mdServer , li, len);
                List<long> r= new List<long>();

                for (int i = 1; i <= 10; i++)
                {
                    r.Add(preheat.PredictOccupancy(960, len).ElementAt(0).getVal());// 11th day
                }
                long mean = ListExtensions.Mean(r);
                double std = ListExtensions.StandardDeviation(r);
                
                StreamWriter results;
                using (results = File.AppendText("avg-ret-time-chunksize.txt"))
                    results.WriteLine("{0} {1} {2}", chunk, mean, std);
            }

           

            /*
            outputFile = ".\\smart";
            SmartPreHeat spreheat = new SmartPreHeat(null, 5, outputFile);
            spreheat.PredictOccupancy(0, len);

            CreateDayMax(outputFile, 3);
            CreateDayMax(outputFile, 4);
            CreateDayMax(outputFile, 5);


            outputFile = ".\\optimal";
            OptimalPreHeat preheat = new OptimalPreHeat(null, 5, outputFile);
            preheat.PredictOccupancy(0, len);

            CreateDayMax(outputFile, 3);
            CreateDayMax(outputFile, 4);
            CreateDayMax(outputFile, 5);


            outputFile = ".\\naive";
            NaivePreHeat npreheat = new NaivePreHeat(null, 5, outputFile);
            npreheat.PredictOccupancy(0, len);

            CreateDayMax(outputFile, 3);
            CreateDayMax(outputFile, 4);
            CreateDayMax(outputFile, 5);

            */
            
            

        }

        static void CreateDayMax(string filePath, int index)
        {
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(filePath);
            int maxRet = -1;

            while ((line = file.ReadLine()) != null)
            {
                string[] words = line.Split(' ');

                int slot = Int32.Parse(words[2]);
                slot--;

                if (Int32.Parse(words[index]) > maxRet)
                    maxRet = Int32.Parse(words[index]);

                if (slot % 96 == 0)
                {
                    Console.WriteLine("{0},{1}", slot / 96, (float)(maxRet / 10000));

                    using (StreamWriter w = File.AppendText(filePath+ "-daymax-"+index ))
                    {
                        w.WriteLine("{0},{1}", slot / 96, (float)(maxRet / 10000));
                    }
                    maxRet = -1;
                }

            }

            file.Close();

        }

    }
}
