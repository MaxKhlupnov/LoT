using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.IO;

namespace HomeOS.Hub.Common.Bolt.Apps.Eval
{
    public class CostsHelper
    {
        static PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        static PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        static NetworkInterface[] interfaces
                = NetworkInterface.GetAllNetworkInterfaces();


        static long bytes_sent = 0;
        static long bytes_recd = 0;

        public string getCurrentCpuUsage()
        {
            return cpuCounter.NextValue()+"";
        }

        public string getAvailableRAM()
        {
            return ramCounter.NextValue()+"";
        }

        public float getNetworkUsage()
        {
            long t_bytes_sent = 0;
            long t_bytes_recd = 0;
            foreach (NetworkInterface ni in interfaces)
            {
                t_bytes_sent += ni.GetIPv4Statistics().BytesSent;
                t_bytes_recd += ni.GetIPv4Statistics().BytesReceived;
            }

            float ret = ((t_bytes_sent - bytes_sent) + (t_bytes_recd - bytes_recd))/1000.0f;
            bytes_sent = t_bytes_sent;
            bytes_recd = t_bytes_recd;
            return ret;
        }

        // From "http://stackoverflow.com/questions/468119/whats-the-best-way-to-calculate-the-size-of-a-directory-in-net"
        protected float CalculateFolderSize(string folder, bool dataRelated = true)
        {
            float folderSize = 0.0f;
            try
            {
                //Checks if the path is valid or not
                if (!Directory.Exists(folder))
                    return folderSize;
                else
                {
                    try
                    {
                        foreach (string file in Directory.GetFiles(folder))
                        {
                            if (File.Exists(file))
                            {
                                FileInfo finfo = new FileInfo(file);
                                if (finfo.Name == "log" || finfo.Name == "exp" || finfo.Name == "results")
                                    continue;

                                if (dataRelated != true)
                                {
                                    if (finfo.Name == "stream.dat" || finfo.Name == "index.dat")
                                        continue;
                                }
                                folderSize += finfo.Length;
                            }
                        }

                        foreach (string dir in Directory.GetDirectories(folder))
                            folderSize += CalculateFolderSize(dir, dataRelated:dataRelated);
                    }
                    catch (NotSupportedException e)
                    {
                        Console.WriteLine("Unable to calculate folder size: {0}", e.Message);
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("Unable to calculate folder size: {0}", e.Message);
            }
            return folderSize;
        }


        public float getStorageUsage(string path, bool dataRelated = true)
        {
            return this.CalculateFolderSize(path, dataRelated);
        }
    }
}
