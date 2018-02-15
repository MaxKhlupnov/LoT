using HomeOS.Hub.Common.Bolt.DataStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.DNW
{
    class MainClass
    {
        static void Main(string[] args)
        {
            int len_hours = 1000 ; //24 *60* 30;
            int[] windows = {60} ;// {  600, 24*60, 7*24*60};
            int[] chunkSizes = { 4*1024*1024, 1024 * 1024, 100*1024, 10 * 1024, 1024 };
            LocationInfo li = new LocationInfo("testdrive", "zRTT++dVryOWXJyAM7NM0TuQcu0Y23BgCQfkt7xh2f/Mm+r6c8/XtPTY0xxaF6tPSACJiuACsjotDeNIVyXM8Q==", SynchronizerType.Azure);
            string mdServer="http://scspc417.cs.uwaterloo.ca:23456/TrustedServer/";
            int numberOfStreams = 1; // 10 homes
            int numberOfExperimentRepetitions = 1;
           

           foreach (int window in windows)
           {
               foreach (int chunkSize in chunkSizes)
               {
                   
                   DNW dnwt = new DNW(numberOfStreams, window, li, mdServer, chunkSize);
                   for (int i = 1; i <= len_hours; i++)
                   {
                       dnwt.ReadObject();
                   }
                   dnwt.Finish();
                   

                   List<long> timeTakenForRemoteRead = new List<long>();
                   long dataDownloaded=0;
                   for (int repeat = 1; repeat <= numberOfExperimentRepetitions; repeat++)
                   {
                       //lets clean up everything. as if we're the reader located in a different home.
                       for (int i = 1; i <= numberOfStreams; i++)
                       {
                           Directory.Delete(dnwt.fqprefix + "-" + window + "-" + i, true);
                       }

                       timeTakenForRemoteRead.Add(dnwt.RemoteMatch(null));
                       for (int i = 1; i <= numberOfStreams; i++)
                       {
                           dataDownloaded += GetDirectorySize(dnwt.fqprefix + "-" + window + "-" + i+"/");
                       }
                       
                   }

                   //of the data downloaded, the amount of used for answering the query is 29689 per home
                   //i.e. size of the stream on disk when len_hours=window size=60
                   //and for window size of 600 it is 293207 per home

                   long dataused=0;
                   if (window == 60)
                       dataused = 29689;
                   if (window == 600)
                       dataused = 293207;

                   using (StreamWriter writer = File.AppendText("results.txt"))
                       writer.Write(window + "," + numberOfStreams + "," + chunkSize + "," + ListExtensions.Mean(timeTakenForRemoteRead) + "," + ListExtensions.StandardDeviation(timeTakenForRemoteRead) + "," + dataDownloaded + "," +dataused+"\n");
                   Console.WriteLine(window + "," + numberOfStreams + "," + chunkSize + "," + ListExtensions.Mean(timeTakenForRemoteRead) + "," + ListExtensions.StandardDeviation(timeTakenForRemoteRead) +","+dataDownloaded+","+dataused+ "\n");

               }
           }

        }


        static long GetDirectorySize(string p)
        {
            
            string[] a = Directory.GetFiles(p, "*.*");
            long b = 0;
            foreach (string name in a)
            {
                FileInfo info = new FileInfo(name);
                b += info.Length;
            }

            string[] dirs = Directory.GetDirectories(p);

            foreach (string dirname in dirs)
                b = b + GetDirectorySize(dirname);

            return b;
        }

    }
}
