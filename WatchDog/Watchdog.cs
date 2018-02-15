using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.ServiceProcess;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Linq;

namespace HomeOS.Hub.Watchdog
{
    public partial class WatchdogService : ServiceBase
    {
        public const bool SendHeartBeats = false;
        public const bool CheckForProcessLiveness = true;


        public const bool EnforceChecksumMatch = false;
        public const bool CheckEmbeddedVersionMatch = true;

        private string LastCheckSumValue = "";
        private string InstalledVersion = "";
        private string DesiredVersion = "";


#if DEBUG
        public const int SLEEP_TIME_SECONDS = 1;
        public const double RESTART_TIME_SECONDS = (10.0);
        public const double UPDATE_CHECK_SECONDS = (60.0);
#else
        public const int SLEEP_TIME_SECONDS = 30;
        public const double RESTART_TIME_SECONDS = (15.0);
        public const double UPDATE_CHECK_SECONDS = (60.0);
#endif
        // Stores Array of programs to monitor
        public class ProgramDetails
        {
            //the following fields are read from Watchdog.txt
            public string ProcessName;
            public string ExeDir;       //stored as absolute path, but should be relative watchdog.exe in watchdog.txt
            public string ExeName;     
            public int nSecondsRunDelay;
            public bool checkForUpdates;
            public string UpdateUri;
            public string Args;

            //the following fields are dynamically maintained state
            public bool fRunningAtLastCheck;
            public DateTime dtLastRun;
            public DateTime lastUpdateCheck;
        };


        private List<ProgramDetails> aPrograms = new List<ProgramDetails>();
        private string inputDir;
        private System.Timers.Timer watchDogTimer;
        private MessageQueue errorMessages;

        private string messageUploadUrl = null;
        private Boolean currentlyUploadingMessages = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            WatchdogService service = new WatchdogService();

            if (Environment.UserInteractive)
            {
                service.OnStart(args);
                Application.Run();
            }
            else
            {
                ServiceBase.Run(service);
            }
        }

        public WatchdogService()
        {
            InitializeComponent();
        }
 
        protected override void OnStart(string[] args)
        {
            StartWatching();
        }
 
        protected override void OnStop()
        {
            watchDogTimer.Stop();
        }

        public void StartWatching()
        {
            // Make sure only one copy of Watchdog is running...
            Process p = Process.GetCurrentProcess();

            Process[] processes = Process.GetProcessesByName(p.ProcessName);

            if (processes.Length > 1)
            {
                foreach (Process rp in processes)
                {
                    if (rp.Id != p.Id && rp.SessionId == p.SessionId)
                    {
#if DEBUG
                        Console.WriteLine("Watchdog Monitor is already running " + rp.SessionId.ToString() + " is equal to " + p.SessionId.ToString());
#endif
                        return;
                    }
                }
            }

            // Find input file
            inputDir = p.MainModule.FileName;
            inputDir = inputDir.Remove(inputDir.LastIndexOf('\\'));
            Directory.SetCurrentDirectory(inputDir);
            string inputFile = inputDir + "\\Watchdog.txt";

            errorMessages = new MessageQueue();

            ReadInputFile(inputFile);

#if DEBUG
            Win32Imports.AllocConsole();
#endif
            
            watchDogTimer = new System.Timers.Timer();

            //we do not autoreset because we want the count to restart after each time watchdogmonitor (which can take a lot of time) finishes 
            watchDogTimer.AutoReset = false;

            watchDogTimer.Interval = (SLEEP_TIME_SECONDS * 1000);
            watchDogTimer.Elapsed += new ElapsedEventHandler(WatchdogMonitorTick);
            watchDogTimer.Start();

            //Application.Run();

        }

        /// <summary>
        /// Parses the input file and initializes the necessary variables (watchdog.txt)
        /// </summary>
        /// <param name="inputFile"></param>
        private void ReadInputFile(string inputFile)
        {
            using (StreamReader sr = new StreamReader(inputFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {

                        //ignore lines starting with a hash (comments) and empty lines
                        if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                            continue;

                        if (line.StartsWith("MessageUploadUrl:", StringComparison.CurrentCultureIgnoreCase))
                        {
                            string[] words = line.Split(' ');
                            messageUploadUrl = words[1];
                        }

                        ProgramDetails pd = new ProgramDetails();
                        string[] data = line.Split(';');
                        if (data.Length > 1)
                        {
                            pd.ProcessName = data[0];
                            pd.ExeDir = inputDir + "\\" + data[1];
                            pd.ExeName = data[2];
                            pd.nSecondsRunDelay = Convert.ToInt32(data[3]);
                            pd.checkForUpdates = Convert.ToBoolean(data[4]);
                            pd.UpdateUri = data[5];
                            // add in args if any
                            if (data.Length > 6)
                                pd.Args = data[6];

                            pd.fRunningAtLastCheck = false;
                            pd.dtLastRun = DateTime.Now.AddHours(-1.0);
                            pd.lastUpdateCheck = DateTime.Now.AddHours(-1.0);

                            aPrograms.Add(pd);
                        }
                    }
                    catch (Exception e)
                    {
                        LogMessage(e.ToString(), true);
                    }
                }
            }
        }

        private void LogMessage(string message, bool isError=false)
        {
#if DEBUG
            Console.WriteLine(message);
#endif
            if (isError)
            {
                lock (errorMessages)
                {
                    errorMessages.AddMessage(message);

                    //kick the uploading process
                    if (messageUploadUrl != null)
                        TriggerMessageUpload();

                }
            }
        }

        private void TriggerMessageUpload()
        {
            //are we already uploading messages?
            lock (errorMessages)
            {
                if (currentlyUploadingMessages)
                    return;
            }

            System.Threading.Thread uploadThread = new System.Threading.Thread(UploadMessages);
            uploadThread.Start();
        }

        private void UploadMessages()
        {
            try
            {
                lock (errorMessages)
                {
                    currentlyUploadingMessages = true;
                }

                while (true)
                {
                    Message msgToUpload = null;

                    lock (errorMessages)
                    {
                        if (errorMessages.Count == 0)
                        {
                            currentlyUploadingMessages = false;
                            return;
                        }

                        //this should not return an exception because the queue is not empty
                        msgToUpload = errorMessages.Peek();
                    }

                    if (msgToUpload != null)
                    {
                        UploadMessage(msgToUpload);
                    }

                }
            }
            //catch all because we never want this thread to raise an exception. 
            //we do not log the exception, however, because we might run into a inifnite loop
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }

        }

        private void UploadMessage(Message message) 
        {
            try
            {
                HomeOS.Shared.WatchdogMsgInfo msgInfo = message.ToWatchdogMessageInfo();

                if (null != msgInfo)
                {
                    string jsonString = msgInfo.SerializeToJsonStream();
                    WebClient webClient = new WebClient();
                    webClient.Headers["Content-type"] = "application/json";
                    webClient.Encoding = Encoding.UTF8;
                    webClient.UseDefaultCredentials = true;
                    webClient.UploadData(new Uri(messageUploadUrl), "POST", Encoding.UTF8.GetBytes(jsonString));

                    lock (errorMessages)
                    {
                        //this should be non-zero, but does not hurt to check
                        if (errorMessages.Count != 0)
                        {
                            var currentTop = errorMessages.Peek();

                            // the top message may not be the one we just uploaded if the top was dequeued
                            // during message insertion because the queue got full
                            if (currentTop.Equals(message))
                                 errorMessages.Dequeue();
                        }
                    }
                }
            }
            //catch all because we never want this thread to raise an exception. 
            //we do not log the exception, however, because we might run into a inifnite loop
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }

        private void WatchdogMonitorTick(object sender, EventArgs Args)
        {
            try
            {
                //1. deposit heart beat information
                if (SendHeartBeats)
                {
                    // TODO
                }

                //2. check if there is a new version of the platform
                CheckProcessUpdatedness();

                //3. check if the process is running
                if (CheckForProcessLiveness)
                {
                    CheckProcessLiveness();
                }
            }
            catch (Exception e)
            {
                LogMessage(e.ToString(), true);
            }

            //let's start count again
            watchDogTimer.Start();
        }

        #region code relater to updating platform

        private string GetDesiredPlatformChecksumValueFromUrl(string hashUrl, string localHashFile)
        {
            string checksumValue = "";
            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(hashUrl, localHashFile);
                webClient.Dispose();
            }
            catch (Exception e)
            {
                LogMessage(String.Format("Failed to download the latest platform zip checksum file using url {0}, Exception:{1}", hashUrl, e.ToString()), true);
                return checksumValue;
            }

            checksumValue = File.ReadAllText(localHashFile);

            return checksumValue.Trim(); 
        }


        private bool GetDesiredPlatformZipFromUrl(string zipUrl, string localZipFile)
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(zipUrl, localZipFile);
                webClient.Dispose();
            }
            catch (Exception e)
            {
                LogMessage(String.Format("Failed to download the latest platform zip file using url {0}, Exception:{1}", zipUrl, e.ToString()), true);
                return false;
            }

            if (!File.Exists(localZipFile))
            {
                return false;
            }

            return true;
        }

        private string GetHomeOSUpdateVersion(string configFile)
        {
            const string DefaultHomeOSUpdateVersionValue = "0.0.0.0";
            const string ConfigAppSettingKeyHomeOSUpdateVersion = "HomeOSUpdateVersion";

            string homeosUpdateVersion = DefaultHomeOSUpdateVersionValue;
            try
            {
                XElement xmlTree = XElement.Load(configFile);
                IEnumerable<XElement> das =
                    from el in xmlTree.DescendantsAndSelf()
                    where el.Name == "add" && el.Parent.Name == "appSettings" && el.Attribute("key").Value == ConfigAppSettingKeyHomeOSUpdateVersion
                    select el;
                if (das.Count() > 0)
                {
                    homeosUpdateVersion = das.First().Attribute("value").Value;
                }
            }
            catch (Exception e)
            {
                LogMessage(String.Format("Failed to parse {0}, exception: {1}", configFile, e.ToString()), true);
            }
            return homeosUpdateVersion;
        }


        private  void CheckProcessUpdatedness()
        {
            foreach (ProgramDetails pd in aPrograms)
            {
                // should we check for update for this program
                if (!pd.checkForUpdates)
                    continue;

                //did enough time elapse since the last update
                if (pd.lastUpdateCheck.AddSeconds(UPDATE_CHECK_SECONDS) > DateTime.Now)
                    continue;

                string pdExeName = Path.GetFileNameWithoutExtension(pd.ExeName);
                string zipUrl = pd.UpdateUri  + "/" + pdExeName + ".zip";
                string hashUrl = pd.UpdateUri + "/" + pdExeName + ".md5";

                LogMessage("Checking for updates at URI: " + zipUrl);

                string tmpZipFile = String.Format("{0}\\{1}.zip", inputDir, pdExeName);
                string tmpHashFile = String.Format("{0}\\{1}.md5", inputDir, pdExeName);
                string tmpBinFolder = String.Format("{0}\\{1}.tmp", inputDir, pdExeName);

                // Block 1 : DOWNLOAD AND VALIDATE CHECKSUM
                // 1. Download the hash file for the desired platform version
                // 2. If the hash value is same as the last one we downloaded AND we downloaded the corresponding zip and validated
                // the checksum for a successful download, then skip this.
                // 3. Otherwise, download zip, if successful delete the tmpBinFolder for extracting zip
                // 4. Validate checksum, if matched, remember the hash value for step 2 optimization
                // 
                // Note: Here we have optimized for not having to to download the zip everytime by just checking the hash value,
                // theoretically this can have a collision, i.e, two zip files having different bits but the same hash value,
                // but the odds of this happening are very small. 
                do
                {
                    string desiredPlatformChecksum = GetDesiredPlatformChecksumValueFromUrl(hashUrl, tmpHashFile);
                    if (string.IsNullOrEmpty(desiredPlatformChecksum))
                    {
                        LogMessage(String.Format("Failed to download latest {0} checksum (hash) file from {1} to be copied to {2} locally", pd.ExeName, hashUrl, tmpHashFile), true);
                        goto next;
                    }
                    if (this.LastCheckSumValue == desiredPlatformChecksum)
                    {
                        // we already have the latest
                        break;
                    }
                    if (!GetDesiredPlatformZipFromUrl(zipUrl, tmpZipFile))
                    {
                        LogMessage(String.Format("Failed to download latest {0} zip using {1} for a local write to {2} with expected checksum value {3}",
                                        pd.ExeName, zipUrl, tmpZipFile, desiredPlatformChecksum), true);
                        goto next;
                    }
                    if (Directory.Exists(tmpBinFolder))
                    {
                        DeleteFolder(tmpBinFolder, true);
                        this.DesiredVersion = "";
                    }
                    string computedChecksum = GetMD5HashFromFile(tmpZipFile);
                    if (computedChecksum != desiredPlatformChecksum)
                    {
                        LogMessage(String.Format("Checksum mismatch!, expected MD5 hash value: [{0}], computed value: [{1}], ignoring this attempt", desiredPlatformChecksum, computedChecksum), true);
                        goto next;
                    }

                    this.LastCheckSumValue = computedChecksum;
                } while (false);

                // Block 2: EXTRACT, COMPARE VERSION, INSTALL IF NEWER
                // 5. If tmp folder not present:
                //      a. Extract the validated zip in a tmp folder, if folder not present, otherwise skip
                //      b. Validate contents of the temp folder and grab the name of the top level folder
                //    else
                //         Grab the name of the top level folder
                // 6.   a. Get the desired version from tmp folder's pd.exe.config, if empty
                //      b. Get the current version from pd.ExeDir's pd.exe.config, if empty
                // 7. If the desired version is the same or older then currently installed version, done with this block
                // 8. If newer, stop the running process, copy the contents of the tmp folder to the exe dir
                // 9. Update the current installed version

                do
                {
                    if (!Directory.Exists(tmpBinFolder))
                    {
                        CreateFolder(tmpBinFolder);
                        System.IO.Compression.ZipFile.ExtractToDirectory(tmpZipFile, tmpBinFolder);
                    }
                    if (string.IsNullOrEmpty(this.DesiredVersion))
                    {
                        this.DesiredVersion = GetHomeOSUpdateVersion(tmpBinFolder + "\\" + pd.ExeName + ".config");
                    }
                    if (string.IsNullOrEmpty(this.InstalledVersion))
                    {
                        this.InstalledVersion = GetHomeOSUpdateVersion(pd.ExeDir + "\\" + pd.ExeName + ".config");
                    }

                    LogMessage(String.Format("Current Version = {0}, Desired Version = {1}", this.InstalledVersion, this.DesiredVersion));
                    if (new Version(this.DesiredVersion).CompareTo(new Version(this.InstalledVersion)) <= 0)
                    {
                        break;
                    }
                    LogMessage(String.Format("About to update {0} to version {1}. Current installed version = {2}", pd.ExeName, this.DesiredVersion, this.InstalledVersion), true);

                    // things seem in order lets kill the process and then copy files
                    KillProcess(pd);

                    // copy over the folder
                    CopyFolder(tmpBinFolder, pd.ExeDir);

                    // remember the new version installed
                    this.InstalledVersion = this.DesiredVersion;

                } while (false);

next:
                // Block 3: Update the last check time stamp
                pd.lastUpdateCheck = DateTime.Now;
            }
        }

        public void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);

                int numTries = 3;
                while (numTries > 0)
                {
                    try
                    {
                        numTries--;

                        File.Copy(file, dest, true);

                        break;
                    }
                    catch (Exception e)
                    {
                        if (numTries > 0)
                            System.Threading.Thread.Sleep(5 * 1000);
                        else 
                           LogMessage("Failed to copy " + file + "\n" + e.ToString(), true);
                    }
                }
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                try
                {
                    CopyFolder(folder, dest);
                }
                catch (Exception e)
                {
                    LogMessage(e.ToString(), true);
                }
            }
        }

        private void KillProcess(ProgramDetails pd)
        {
            Process ThisProcess = Process.GetCurrentProcess();
            Process[] processes = System.Diagnostics.Process.GetProcesses();

            // Is it running right now?
            Process process = GetProcessIfRunning(ThisProcess.SessionId, pd, processes);

            if (process != null)
            {
                //todo: need a way to gracefully shut down the process

                //bool result = process.CloseMainWindow();

                //LogInfo(String.Format("Result of CloseMainWindow on {0} is {1}", pd.ProcessName, result));

                process.Kill();

                process.Close();
            }
            else
            {
                LogMessage(String.Format("Tried to kill {0} but it is not running", pd.ProcessName));
            }
        }


        private void CreateFolder(String folder)
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);

            Directory.CreateDirectory(folder);

            //DirectoryInfo info = new DirectoryInfo(completePath);
            //System.Security.AccessControl.DirectorySecurity security = info.GetAccessControl();
            //security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(Environment.UserName, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.InheritanceFlags.ContainerInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
            //security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(Environment.UserName, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.InheritanceFlags.ContainerInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
            //info.SetAccessControl(security);
            //return completePath;

            //return null;
        }

        private void DeleteFolder(string folder, bool recursive = false)
        {
            Exception exception = null;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Directory.Delete(folder, recursive);

                    return;
                }
                catch (Exception e)
                {
                    exception = e;
                }

                //lets wait 5 seconds and then we try to delete again
                System.Threading.Thread.Sleep(5 * 1000);
            }

            throw exception;
        }

        private string GetMD5HashFromFile(string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }


        #endregion

        private void CheckProcessLiveness()
        {
            Process ThisProcess = Process.GetCurrentProcess();
            Process[] processes = System.Diagnostics.Process.GetProcesses();
            foreach (ProgramDetails pd in aPrograms)
            {
                // Should this be running?
                if (pd.dtLastRun.AddSeconds(pd.nSecondsRunDelay) > DateTime.Now)
                    continue;

                LogMessage("Checking liveness for ..." + pd.ProcessName);

                // Is it running right now?
                Process process = GetProcessIfRunning(ThisProcess.SessionId, pd, processes);

                if (process != null)
                {
                    pd.fRunningAtLastCheck = true;
                }
                else
                {
                    DateTime curTime = DateTime.Now;

                    // Hey look, it isn't running...
                    if (pd.fRunningAtLastCheck)
                    {
                        
                        LogMessage(String.Format("{0} appears to have died...", pd.ProcessName), true);

                        pd.fRunningAtLastCheck = false;
                        pd.dtLastRun = curTime;
                    }

                    // It is _still_ not running...
                    else
                    {
                        LogMessage(String.Format("{0} still not running...", pd.ProcessName));

                        pd.fRunningAtLastCheck = false;
                        TimeSpan ts = curTime - pd.dtLastRun;
                        if (ts.TotalSeconds > RESTART_TIME_SECONDS)
                        {
                            LogMessage(String.Format("Re-starting {0}...", pd.ProcessName));

                            try
                            {
                                ProcessStartInfo startInfo = new ProcessStartInfo(pd.ExeDir + '\\' + pd.ExeName, pd.Args);
                                startInfo.WorkingDirectory = pd.ExeDir;
                                
                                //Process.Start(startInfo);

                                startInfo.UseShellExecute = false;
                                startInfo.RedirectStandardError = true;
                                startInfo.RedirectStandardOutput = true;

                                Process processToStart = new Process();
                                processToStart.StartInfo = startInfo;
                                processToStart.ErrorDataReceived += (sender, eventArgs) => CommandOutputReceived(sender, eventArgs, true);
                                processToStart.OutputDataReceived += (sender, eventArgs) => CommandOutputReceived(sender, eventArgs, false);

                                processToStart.Start();
                                processToStart.BeginErrorReadLine();
                                processToStart.BeginOutputReadLine();
                            }
                            catch (Exception e)
                            {
                                LogMessage(e.ToString(), true);
                            }

                            pd.fRunningAtLastCheck = true;
                        }
                    }
                }
            }
        }

        private void CommandOutputReceived(object sender, System.Diagnostics.DataReceivedEventArgs e, bool isError)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                string tag = isError ? "stderr: " : "stdout: "; 
                LogMessage(tag + e.Data, isError);
            }
        }

        private Process GetProcessIfRunning(int SessionId, ProgramDetails pd, Process[] processes)
        {
            for (int i = 0; i < processes.Length; i++)
            {
                if (processes[i].SessionId != SessionId)
                    continue;

                String name = processes[i].ProcessName;
                // Console.WriteLine("Found a process: {0}", name);

                if (name.ToLower().Equals(pd.ProcessName.ToLower()))
                {
                    //LogMessage(String.Format("Hooray!  {0} is running...", pd.ProcessName));

                    return processes[i];
                }
            }
            return null;
        }

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        //#region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.ServiceName = "HomeOS Hub Watchdog";
        }

        //#endregion

    }
}