using System;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;

namespace HomeOS.Hub.Common
{
    public class UnitTestLogger : TextWriter
    {
        public UnitTestLogger()
        {
        }
        ~UnitTestLogger()
        {
        }

        // Summary:
        public override System.Text.Encoding Encoding
        {
            get
            {
                return System.Text.Encoding.UTF8;
            }
        }           

        public override void Close()
        {
            Debug.Close();
        }

        public override void Flush()
        {
            Debug.Flush();
        }

        public override void Write(string value)
        {
            Debug.Write(value);
        }

        public override void Write(string format, object arg0)
        {
            Debug.WriteLine(format, arg0);
        }

        public override void WriteLine(string format, params object[] arg)
        {
            Debug.WriteLine(format, arg);
        }
    }

    public sealed class Logger : MarshalByRefObject, HomeOS.Hub.Platform.Views.VLogger
    {

        private enum Mode { Regular, Stdout, Unittester };
        
        TextWriter logWriter = null;
        private string fName = null;
        private Mode mode;

        /// <summary>
        /// RotationThreshold of zero implies no rotation
        /// </summary>
        private ulong rotationThreshold = 0;

        private string archivingDirectory = null;

        private ulong linesSinceLastRotate = 0;

        /// <summary>
        /// The synchronizer object
        /// </summary>
        Bolt.DataStore.ISync synchronizer = null;

        public Logger() : this(":stdout")
        {
        }

        public Logger(String fname)
        {
            this.fName = fname;

            switch (fname)
            {
                case ":stdout":
                case "-":
                    logWriter = Console.Out;
                    mode = Mode.Stdout;
                    break;
                //unit testing logger
                case ":unittester":
                    logWriter = new UnitTestLogger();
                    mode = Mode.Unittester;
                    break;
                default:
                    //Create the directory for the file if it doesn't exist already
                    Directory.CreateDirectory((new FileInfo(fName).Directory).ToString());
                    logWriter = new StreamWriter(fname, true);
                    mode = Mode.Regular;
                    break;
            }            
        }

        public Logger(string fname, ulong rotationThreshold, string archivingDirectory) : this(fname)
        {
            this.rotationThreshold = rotationThreshold;
            this.archivingDirectory = archivingDirectory;

            if (IsRotatingLog)
            {
                Directory.CreateDirectory(archivingDirectory);
            }
        }

        /// <summary>
        /// This function will throw an exception if the log is non-rotating and if the container name does meet the following constraints:
        /// 1. Container names must start with a letter or number, and can contain only letters, numbers, and the dash (-) character.
        /// 2. Every dash (-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names.
        /// 3. All letters in a container name must be lowercase.
        /// 4. Container names must be from 3 through 63 characters long
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="accountKey"></param>
        /// <param name="containerName"></param>
        public void InitSyncing(string accountName, string accountKey, string containerName)
        {
            if (!IsRotatingLog)
                throw new Exception("Cannot sync a non-rotating log");

            //the code below could throw an exception if containerName does not meet the restrictions
            var locationInfo = new Bolt.DataStore.LocationInfo(accountName, accountKey, Bolt.DataStore.SynchronizerType.Azure);

            try
            {
                synchronizer = Bolt.DataStore.SyncFactory.Instance.CreateLogSynchronizer(locationInfo, containerName);
                synchronizer.SetLocalSource(archivingDirectory);

                //lets sync for starters, in case there are leftover logs from last time
                SafeThread worker = new SafeThread(delegate() { synchronizer.Sync(); }, "init log syncing", this);
                worker.Start();
            }
            catch (System.FormatException ex1)
            {
                Log("ERROR: Could not start log syncing. The Azure account key may be wrong \n {0}", ex1.ToString());
            }
            catch (System.Runtime.InteropServices.COMException ex2)
            {
                Log("ERROR: Could not start log syncing. It appears that the Sync Framework v2.1 x86 version is not installed. Make sure that no other version is present. \n {0}", ex2.ToString());
            }
            catch (Microsoft.WindowsAzure.StorageClient.StorageServerException ex3)
            {
                Log("ERROR: Could not start log syncing. The Azure account name may be wrong.\n {0}", ex3.ToString());
            }
            catch (Microsoft.WindowsAzure.StorageClient.StorageClientException ex3)
            {
                Log("ERROR: Could not start log syncing. The Azure account key may be wrong.\n {0}", ex3.ToString());
            }               
            catch (Exception ex3)
            {
                Log("Got unknown exception while starting log syncing.\n {0}", ex3.ToString());
            }
        }
        

        public bool IsRotatingLog
        {
            get { return (rotationThreshold > 0 && mode == Mode.Regular); }
        }

        public string ArchivingDirectory
        {
            get { return archivingDirectory; }
        }

        public void Replace(String fname)
        {
            logWriter.Close();
            logWriter = new StreamWriter(fname, false);
        }

        public void Close()
        {
            logWriter.Close();
        }

        public void Log(String format, params string[] args)
        {
            Log(false, format, args);
        }

        private void Log(bool suppressTime, String format, params object[] args)
        {
            try
            {
                lock (this)
                {
                    if (!suppressTime)
                        logWriter.Write("{0:u} ", DateTime.Now);
                    logWriter.WriteLine(format, args);
                    logWriter.Flush();

                    if (IsRotatingLog)
                    {
                        linesSinceLastRotate++;

                        if (linesSinceLastRotate >= rotationThreshold)
                        {
                            RotateLog();

                            linesSinceLastRotate = 0;
                        }

                    }
                }
            }
            catch (Exception e)
            {
                // Console.WriteLine("Failed to log: " + e + e.StackTrace);
                Console.Error.WriteLine("Failed to log: format: " + format + " exception is " + e + " stack " + e.StackTrace);
            }
        }

        private void RotateLog()
        {
            logWriter.Close();

            string archivingFile = archivingDirectory + "\\" + GetTimeStamp() + "-" + Path.GetFileName(fName);

            while (File.Exists(archivingFile) || File.Exists(archivingFile + ".zip"))
                archivingFile += ".1";

            File.Move(fName, archivingFile);

            logWriter = new StreamWriter(fName, true);

            SafeThread worker = new SafeThread(delegate()
            {
                //compress the file
                CompressFile(archivingFile);

                //if we are syncing, start that on a separate thread
                if (synchronizer != null)
                {
                    synchronizer.Sync();
                }
            },
            "post-log-rotation-work", 
            this
            );

            worker.Start();
        }

        public static void CompressFile(string fileToCompress)
        {
            FileInfo fileInfoToCompress = new FileInfo(fileToCompress);

            if ((File.GetAttributes(fileInfoToCompress.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden & fileInfoToCompress.Extension != ".zip")
            {
                string compressedFileName = fileInfoToCompress.FullName + ".zip";

                ZipArchive archive = ZipFile.Open(compressedFileName, ZipArchiveMode.Create);
                archive.CreateEntryFromFile(fileToCompress, fileInfoToCompress.Name, CompressionLevel.Optimal);
                archive.Dispose();

                File.Delete(fileToCompress);
            }
        }
        


        private string GetTimeStamp()
        {
            var now = DateTime.Now;
            return string.Format("{0:D4}-{1:D2}-{2:D2}-{3:D2}-{4:D2}-{5:D2}", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        }
    }
}