using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Files;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using HomeOS.Hub.Common.Bolt.DataStore;

namespace HomeOS.Hub.Common.Bolt.Apps.Eval 
{

    public class EvalMain
    {
        public static double StandardDeviation(List<double> valueList)
        {
            double M = 0.0;
            double S = 0.0;
            int k = 1;
            foreach (double value in valueList)
            {
                double tmpM = M;
                M += (value - tmpM) / k;
                S += (value - tmpM) * (value - M);
                k++;
            }
            return Math.Sqrt(S / (k - 1));
        }

        // static StreamOperation[] ops = {StreamOperation.RandomKeyGetAll, StreamOperation.RandomKeyRandomValueAppend, StreamOperation.RandomKeyGet};
        static StreamOperation[] ops = {StreamOperation.RandomKeyGet};
        static StreamType[] types = { StreamType.DiskRaw, StreamType.Local, StreamType.CloudRaw, StreamType.Remote, StreamType.RemoteEnc };
        // static StreamType[] types = { StreamType.Remote};
        // static StreamType[] types = { StreamType.DiskRaw };
        // static int[] n_operations = { 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000};
        // static int[] n_operations = { 10083, 10083 * 2, 10083 * 4, 10083 * 8, 10083 * 16, 10083 * 32, 10083 * 64, 10083 * 128 };
        static int[] n_operations = { 1000 };
        static int num_multi_segment_operations = 1000;
        static int n_iterations = 10;
        static StreamFactory.StreamDataType[] ptypes = { StreamFactory.StreamDataType.Files};
        static CompressionType[] ctypes = { CompressionType.None};
        //  static int[] data_size = { 10, 1000, 10000 };
        static int[] data_size = { 1000, 10000, 100000 };
        // static int[] data_size = { 10, 1000, 10000 };
        static int[] ChunkSize = { 4194285 };
        // static int[] ChunkSize = { 4194304 * 8 };
        // static int[] ChunkSize = { 128 * 1024, 256 * 1024, 512 * 1024, 1024 * 1024, 2048 * 1024, 4194285 };
        // static int[] ChunkSize = {128 * 1024, 256 * 1024, 512 * 1024, 1024 * 1024, 2048 * 1024, 4194285, 4194304 * 2, 4194304 * 4, 4194304 * 8, 4194304 * 16}; // ~ 4 MB
        // static int[] num_threads = {1, 2, 3, 4, 5, 6, 7, 8}  ; // ~ 4 MB
        static int[] num_threads = { 1 };
        static SynchronizerType[] synctypes = { SynchronizerType.Azure};
        static String[] streamListType = { null };

        static void SummaryToDat(string dir)
        {
            // Dump out .dat files
            try
            {
                List<string> summary = File.ReadAllLines(dir + "/Summary").ToList();
                
                // For writes
                // string suffix = "_" + ptypes[0] + "_" + types[0] + ".dat";
                string suffix = "_" + ptypes[0] + ".dat";
                StreamWriter sw = new StreamWriter(dir + "writes_bw" + suffix);
                SummaryToDat(summary, new int[] { 22, 33 }, sw, doBW: true);
                sw.Close();

                sw = new StreamWriter(dir + "writes_label" + suffix);
                SummaryToDat(summary, new int[] { 22, 33 }, sw, dotimes: true, dolabel: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "writes_ops" + suffix);
                SummaryToDat(summary, new int[] { 22, 33 }, sw, doops: true);
                sw.Close();

                sw = new StreamWriter(dir + "writes_latency" + suffix);
                SummaryToDat(summary, new int[] { 22, 33 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "writes_index_lookup" + suffix);
                SummaryToDat(summary, new int[] { 26 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "writes_construct_db" + suffix);
                SummaryToDat(summary, new int[] { 27 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "writes_construct_and_secure_db" + suffix);
                SummaryToDat(summary, new int[] { 23 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "writes_update_vds" + suffix);
                SummaryToDat(summary, new int[] { 25 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "writes_serial" + suffix);
                SummaryToDat(summary, new int[] { 28 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "writes_fds_todisk" + suffix);
                SummaryToDat(summary, new int[] { 31 }, sw, dotimes: true);
                sw.Close();
                sw = new StreamWriter(dir + "writes_todisk" + suffix);
                SummaryToDat(summary, new int[] { 29, 35 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "writes_add_dbi" + suffix);
                SummaryToDat(summary, new int[] { 30 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "writes_flushindex" + suffix);
                SummaryToDat(summary, new int[] { 36 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "writes_upload_indi" + suffix);
                SummaryToDat(summary, new int[] { 39 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "writes_upload_fds" + suffix);
                SummaryToDat(summary, new int[] { 38 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "writes_upload_chunked" + suffix);
                SummaryToDat(summary, new int[] { 40 }, sw, dotimes: true);
                sw.Close();

                //sw = new StreamWriter(dir + "writes_chunking" + suffix);
                //SummaryToDat(summary, new int[] { 42, 43, 44, 45, 49, 50, 51 }, sw, dotimes: true);
                //sw.Close();
                
                sw = new StreamWriter(dir + "writes_read_chunk" + suffix);
                SummaryToDat(summary, new int[] { 46 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "writes_compress" + suffix);
                SummaryToDat(summary, new int[] { 47 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "writes_encrypt" + suffix);
                SummaryToDat(summary, new int[] { 48 }, sw, dotimes: true);
                sw.Close();

                // Storage overhead
                sw = new StreamWriter(dir + "writes_datarelated_storage" + suffix);
                SummaryToDat(summary, new int[] {57}, sw, doCosts:true, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "writes_constant_storage" + suffix);
                SummaryToDat(summary, new int[] {58}, sw, doCosts:true, dotimes: true);
                sw.Close();







                // For simple read
                sw = new StreamWriter(dir + "reads_bw" + suffix);
                SummaryToDat(summary, new int[] { 59 }, sw, doBW: true);
                sw.Close();

                sw = new StreamWriter(dir + "reads_label" + suffix);
                SummaryToDat(summary, new int[] { 59 }, sw, dotimes: true, dolabel: true);
                sw.Close();

                sw = new StreamWriter(dir + "reads_latency" + suffix);
                SummaryToDat(summary, new int[] { 59 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_ops" + suffix);
                SummaryToDat(summary, new int[] { 59 }, sw, doops: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_offset" + suffix);
                SummaryToDat(summary, new int[] { 60 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_fromdisk" + suffix);
                SummaryToDat(summary, new int[] { 68, 66 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_deserial" + suffix);
                SummaryToDat(summary, new int[] { 64 }, sw, dotimes: true);
                sw.Close();
                
                // Chunking includes decrypt, decompress
                sw = new StreamWriter(dir + "reads_chunking" + suffix);
                SummaryToDat(summary, new int[] { 61 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_chunking_actual_download" + suffix);
                SummaryToDat(summary, new int[] { 72 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_chunking_update_cache" + suffix);
                SummaryToDat(summary, new int[] { 73 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_chunking_actual_read" + suffix);
                SummaryToDat(summary, new int[] { 77 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_chunking_open_chunk_file" + suffix);
                SummaryToDat(summary, new int[] { 76 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_chunking_readfromcache" + suffix);
                SummaryToDat(summary, new int[] { 74 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_decrypt" + suffix);
                SummaryToDat(summary, new int[] { 63, 67 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "reads_decompress" + suffix);
                SummaryToDat(summary, new int[] { 62 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "reads_download_indi" + suffix);
                SummaryToDat(summary, new int[] { 65 }, sw, dotimes: true);
                sw.Close();
                
                
                sw = new StreamWriter(dir + "reads_retreive_vds" + suffix);
                SummaryToDat(summary, new int[] { 69 }, sw, dotimes: true);
                sw.Close();


                // For read range query
                sw = new StreamWriter(dir + "reads_range_bw" + suffix);
                SummaryToDat(summary, new int[] { 70, 71 }, sw, doBW: true);
                sw.Close();

                sw = new StreamWriter(dir + "reads_range_label" + suffix);
                SummaryToDat(summary, new int[] { 70, 71 }, sw, dotimes: true, dolabel: true);
                sw.Close();

                sw = new StreamWriter(dir + "reads_range_latency" + suffix);
                SummaryToDat(summary, new int[] { 70, 71 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_range_ops" + suffix);
                SummaryToDat(summary, new int[] { 70, 71 }, sw, doops: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_range_offset" + suffix);
                SummaryToDat(summary, new int[] { 60 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_range_fromdisk" + suffix);
                SummaryToDat(summary, new int[] { 66, 68 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "reads_range_chunking" + suffix);
                SummaryToDat(summary, new int[] { 61 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_range_chunking_actual_download" + suffix);
                SummaryToDat(summary, new int[] { 72 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_range_chunking_update_cache" + suffix);
                SummaryToDat(summary, new int[] { 73 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_range_chunking_actual_read" + suffix);
                SummaryToDat(summary, new int[] { 77 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_range_chunking_open_chunk_file" + suffix);
                SummaryToDat(summary, new int[] { 76 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_range_chunking_readfromcache" + suffix);
                SummaryToDat(summary, new int[] { 74 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_range_decompress" + suffix);
                SummaryToDat(summary, new int[] { 62 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "reads_range_decrypt" + suffix);
                SummaryToDat(summary, new int[] { 63, 67 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "reads_range_deserial" + suffix);
                SummaryToDat(summary, new int[] { 64 }, sw, dotimes: true);
                sw.Close();

                sw = new StreamWriter(dir + "reads_range_download_indi" + suffix);
                SummaryToDat(summary, new int[] { 65 }, sw, dotimes: true);
                sw.Close();


                // Chunking related
                sw = new StreamWriter(dir + "writes_chunksize_effect" + suffix);
                SummaryToDat(summary, new int[] { 38, 40 }, sw, doBW: true, doscale:true);
                sw.Close();
                
                // Threads related
                sw = new StreamWriter(dir + "writes_thread_effect" + suffix);
                SummaryToDat(summary, new int[] { 38, 40 }, sw, doBW: true, doscale:true);
                sw.Close();

                /*
                sw = new StreamWriter(dir + "cpu" + suffix);
                SummaryToDat(summary, new int[] {20}, sw, doCosts: true, dotimes:true);
                sw.Close();

                sw = new StreamWriter(dir + "network" + suffix);
                SummaryToDat(summary, new int[] {21}, sw, doCosts: true, dotimes:true);
                sw.Close();

                sw = new StreamWriter(dir + "storage" + suffix);
                SummaryToDat(summary, new int[] {22}, sw, doCosts: true, dotimes:true);
                sw.Close();
                */
                
                // Open related.
                sw = new StreamWriter(dir + "open_listingrelated" + suffix);
                SummaryToDat(summary, new int[] { 10, 15 }, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "open_securityrelated" + suffix);
                SummaryToDat(summary, new int[] { 5,6, 7, 8, 11, 12, 13, 14, 20}, sw, dotimes: true);
                sw.Close();
                
                sw = new StreamWriter(dir + "open_other" + suffix);
                SummaryToDat(summary, new int[] { 2, 3, 17, 18, 19, 21}, sw, dotimes: true);
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Convert the summary file of the experiment to .dat files for gnuplot scripts
        static void SummaryToDat(List<string> summary, int[] offset, StreamWriter sw,
            bool doCosts = false,
            bool doBW = false,
            bool dotimes = false,
            bool dolabel = false,
            bool doops = false,
            bool doscale = false)
        {
            double baseline = 1;
            foreach (StreamFactory.StreamDataType ptype in ptypes)
            {
                double x = 1.0;
                foreach (int size in data_size)
                {
                    foreach (StreamType type in types)
                    {
                        foreach (int chunksize in ChunkSize)
                        {
                            foreach (int ThreadPoolSize in num_threads)
                            {
                                foreach (int num_operations in n_operations)
                                {
                                    List<double> numbers = new List<double>();
                                    for (int i = 0; i < summary.Count; ++i)
                                    {
                                        if (summary[i].Contains("Value size: " + size + " ") == true
                                            && summary[i].Contains("Stream Ptype: " + ptype + " ") == true
                                            && summary[i].Contains("Chunk Size: " + chunksize + " ") == true
                                            && summary[i].Contains("num_operations: " + num_operations + " " ) == true
                                            && summary[i].Contains("ThreadPool Size:" + ThreadPoolSize) == true
                                                && summary[i].Contains("" + type + " ") == true)
                                        {
                                            char[] delimiterChars = { ':' };
                                            double val = 0.0;
                                            foreach (int j in offset)
                                            {
                                                string[] splits = summary[i + j].Split(delimiterChars);
                                                val += Convert.ToDouble(splits[splits.Length - 1]);
                                            }

                                            if (!doCosts && doBW)
                                            {
                                                double number = ((size * Convert.ToDouble(num_operations)) / val) / 1000f;
                                                numbers.Add(number);
                                            }
                                            else if (!doCosts && dotimes)
                                            {
                                                double number = val;
                                                numbers.Add(number);
                                            }
                                            else if (!doCosts && doops)
                                            {
                                                double number = (Convert.ToDouble(num_operations) / val) * 1000f;
                                                numbers.Add(number);
                                            }
                                            else
                                            {
                                                double number = val;
                                                numbers.Add(number);
                                            }
                                        }
                                    }

                                    if (!dotimes)
                                    {
                                        if (!doscale)
                                            sw.Write(x.ToString("0.0"));
                                        else
                                            sw.Write(ThreadPoolSize + "-" + chunksize);
                                    }
                                    if (!dolabel)
                                        sw.Write(" " + (numbers.Average() / baseline).ToString("0.00"));
                                    else
                                        sw.Write(" " + type + "-" + size + "B");

                                    if (!doCosts && !dotimes)
                                    {
                                        sw.Write(" " + (numbers.Average() - StandardDeviation(numbers)).ToString("0.00"));
                                        sw.Write(" " + (numbers.Average() + StandardDeviation(numbers)).ToString("0.00"));
                                        sw.Write(" " + "0.0");
                                    }

                                    x += 0.5;
                                    sw.WriteLine();
                                }
                            }
                        }
                    }
                    x += 1;
                }
            }
        }

        static void OldMain()
        {
            SummaryToDat("Results/24Sep16-21-00/");
            Console.ReadKey();
        }

        static void Main(string[] args)
        {
            string dir = "";
            List<string> summary = new List<string>();
            foreach (StreamType type in types)
            {
                foreach (String address in streamListType)
                {
                    foreach (int size in data_size)
                    {
                        foreach (StreamFactory.StreamDataType ptype in ptypes)
                        {
                            foreach (CompressionType ctype in ctypes)
                            {
                                foreach (SynchronizerType synctype in synctypes)
                                {
                                    foreach (StreamOperation op in ops)
                                    {
                                        foreach (int chunksize in ChunkSize)
                                        {
                                            foreach (int ThreadPoolSize in num_threads)
                                            {
                                                foreach (int num_operations in n_operations)
                                                {
                                                for (int i = 0; i < n_iterations; ++i)
                                                {
                                                    byte[] data = new byte[size];
                                                    Random rnd = new Random(DateTime.Now.Millisecond);
                                                    rnd.NextBytes(data);
                                                    Experiment e = new Experiment();

                                                    DateTime Date = new DateTime(DateTime.UtcNow.Ticks);

                                                    string HomeName = String.Format("ExpHome-{0}", Date.ToString("yyyy-MM-dd"));
                                                    string Caller = String.Format("{0}", Date.ToString("HH-mm-ss"));
                                                    string AppName = Caller;
                                                    string StreamName = String.Format("{0}", rnd.Next());

                                                    Byte[] value = data;
                                                    // int num_operations = n_operations;

                                                    string RandString = Experiment.RandomString(4);
                                                    long stime = 0;
                                                    long etime = 0;

                                                    // Run the experiment
                                                    if (op == StreamOperation.RandomKeyGet ||
                                                        op == StreamOperation.RandomKeyGetMultipleSegments ||
                                                        op == StreamOperation.RandomKeyGetAll)
                                                    {
                                                        // Need to create the stream before reading it

                                                        stime = StreamFactory.NowUtc();
                                                        Console.WriteLine("Stime: " + stime);

                                                        StreamOperation tmp = StreamOperation.RandomKeySameValueAppend;
                                                        if (op == StreamOperation.RandomKeyGetAll)
                                                            tmp = StreamOperation.SameKeySameValueAppend;
                                                        e.Run(Caller, HomeName, AppName, StreamName,
                                                            RandString,
                                                            stime, etime,
                                                            type, tmp,
                                                            ptype,
                                                            ctype, chunksize, ThreadPoolSize,
                                                            value, num_operations,
                                                            synctype, doCosts: true, address: address);
                                                        etime = StreamFactory.NowUtc();
                                                        Console.WriteLine("Etime: " + etime);
                                                        CallerInfo ci = new CallerInfo(null, AppName, AppName, 1);
                                                        string exp_directory = Path.GetFullPath((null != ci.workingDir) ? ci.workingDir : Directory.GetCurrentDirectory());
                                                        exp_directory = exp_directory + "/" + HomeName + "/" + AppName + "/" + StreamName;

                                                        File.Delete(exp_directory + "/log");

                                                        try
                                                        {
                                                            File.Delete(exp_directory + "/results");
                                                            File.Delete(exp_directory + "/exp");
                                                        }
                                                        catch
                                                        {
                                                        }

                                                        if (type == StreamType.Remote || type == StreamType.RemoteEnc)
                                                        {
                                                            Directory.Delete(exp_directory + "/0", true);
                                                            File.Delete(exp_directory + "/index_md.dat");
                                                        }
                                                    }

                                                    TimeSpan span = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                                                    Console.WriteLine("Time: " + span.TotalSeconds);


                                                    if (op == StreamOperation.RandomKeyGetMultipleSegments || op == StreamOperation.RandomKeyGet)
                                                    {
                                                        e.Run(Caller, HomeName, AppName, StreamName, RandString, stime, etime, type, op, ptype, ctype, chunksize, ThreadPoolSize, value, num_multi_segment_operations, synctype, max_key: num_operations, doCosts: true, address: address);
                                                    }
                                                    else
                                                    {
                                                        e.Run(Caller, HomeName, AppName, StreamName, RandString, stime, etime, type, op, ptype, ctype, chunksize, ThreadPoolSize, value, num_operations, synctype, doCosts: true, address: address);
                                                    }

                                                    dir = e.exp_directory + "/../../../Results/";

                                                    Console.WriteLine("Completed: " + e.ToString());

                                                    span = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                                                    Console.WriteLine("Time: " + span.TotalSeconds);

                                                    Console.WriteLine("====================================");

                                                    // Dump raw data from the experiment
                                                    e.Dump(Caller, HomeName, AppName, StreamName,
                                                        type, op, ptype,
                                                        value, num_operations, synctype);


                                                    // Get parsed data of the experiment
                                                    // if (i != 0)
                                                    //{
                                                        // Ignore the first iteration as warmup
                                                        summary.Add(e.exp_id);
                                                        List<string> ret = e.Analyze(type, value, num_operations);
                                                        summary.AddRange(ret);
                                                    //}
                                                    System.Threading.Thread.Sleep(2000);

                                                    e.Destroy();
                                                }
                                            }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            dir = dir + StreamFactory.PrettyNowUtc();
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            dir = dir + "/";
            File.WriteAllLines(dir + "Summary", summary);

            // Dump .dat files
            SummaryToDat(dir);

            // Thats it! 
            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        
    }
}
