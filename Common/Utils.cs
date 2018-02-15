using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net.Mail;
using HomeOS.Hub.Platform.Views;
using System.Management;
using System.IO;
using System.Security.Cryptography;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.Linq;
using System.AddIn.Hosting;
using System.Collections.ObjectModel;

namespace HomeOS.Hub.Common
{
    /// <summary>
    /// This class contains a bunch of static utilities that both platform and modules can access
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// cached hardware id because the actual call has some overhead
        /// </summary>
        private static string hwId = null;

        /// NB: If you change how hardware id is computed, make sure that you change it in Watchdog as well

        /// <summary>
        /// Returns a unique hardware id for a computer, by combining the cpu and hdd ids.        
        /// </summary>
        /// <returns></returns>
        public static string HardwareId
        {
            get
            {
                if (hwId == null)
                    hwId = String.Format("cpu:{0} hdd:{1}", FirstCpuId(), CVolumeSerial());

                return hwId;
            }
        }

        public static string GetAddInConfigFilepath(string moduleName)
        {
            return Constants.AddInRoot + "\\AddIns\\" + moduleName + "\\" + moduleName + ".dll.config";
        }

        public static string GetScoutConfigFilepath(string scoutName)
        {
            return Constants.ScoutRoot + "\\" + scoutName + "\\" + scoutName + ".dll.config";
        }

        public static Collection<AddInToken> GetAddInTokens(string addInRoot, string moduleName)
        {
            // rebuild the cache files of the pipeline segments and add-ins.
            string[] warnings = AddInStore.Rebuild(addInRoot);

            foreach (string warning in warnings)
                Console.WriteLine(warning);

            // Search for add-ins of type VModule
            Collection<AddInToken> tokens = AddInStore.FindAddIns(typeof(VModule), addInRoot);

            return tokens;
        }

        public static string GetHomeOSUpdateVersion(string configFile)
        {
            return GetHomeOSUpdateVersion(configFile, null);
        }

        public static string GetHomeOSUpdateVersion(string configFile, VLogger logger)
        {
            string homeosUpdateVersion = Constants.UnknownHomeOSUpdateVersionValue;
            try
            {
                XElement xmlTree = XElement.Load(configFile);
                IEnumerable<XElement> das =
                    from el in xmlTree.DescendantsAndSelf()
                    where el.Name == "add" && el.Parent.Name == "appSettings" && el.Attribute("key").Value == Constants.ConfigAppSettingKeyHomeOSUpdateVersion
                    select el;
                if (das.Count() > 0)
                {
                    homeosUpdateVersion = das.First().Attribute("value").Value;
                }
            }
            catch (Exception e)
            {
                if (e is DirectoryNotFoundException || e is FileNotFoundException)
                {
                    if (logger != null)
                        logger.Log("Warning: File not found: " + configFile);
                }
                else
                {
                    if (logger != null)
                        logger.Log(String.Format("GetHomeOSUpdateVersion call failed: Cannot parse {0}. {1}", configFile, "The vervsion is not returned"));
                }
            }
            return homeosUpdateVersion;
        }

        //public static string GetHomeOSUpdateVersion(string configFileUri, VLogger logger)
        //{
        //    string homeosUpdateVersion = UnknownHomeOSUpdateVersionValue;
        //    try
        //    {
        //        System.Net.WebRequest req = System.Net.WebRequest.Create(configFileUri);
        //        System.Net.WebResponse resp = req.GetResponse();

        //        XElement xmlTree = XElement.Load(resp.GetResponseStream());
        //        IEnumerable<XElement> das =
        //            from el in xmlTree.DescendantsAndSelf()
        //            where el.Name == "add" && el.Parent.Name == "appSettings" && el.Attribute("key").Value == ConfigAppSettingKeyHomeOSUpdateVersion
        //            select el;
        //        if (das.Count() > 0)
        //        {
        //            homeosUpdateVersion = das.First().Attribute("value").Value;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        if (null != logger)
        //        {
        //            logger.Log(String.Format("Failed to parse {0}, exception: {1}", configFileUri, e.ToString()));
        //        }
        //    }
        //    return homeosUpdateVersion;
        //}

        /// <summary>
        /// Returns a valid mac address for the fastest interface
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static string MacAddress()
        {
            const int MIN_MAC_ADDR_LENGTH = 12;
            string macAddress = string.Empty;
            long maxSpeed = -1;

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                string tempMac = nic.GetPhysicalAddress().ToString();
                if (nic.Speed > maxSpeed &&
                    !string.IsNullOrEmpty(tempMac) &&
                    tempMac.Length >= MIN_MAC_ADDR_LENGTH)
                {
                    maxSpeed = nic.Speed;
                    macAddress = tempMac;
                }
            }

            return macAddress;
        }

        /// <summary>
        /// Returns the HDD volume of c:
        /// </summary>
        /// <returns></returns>
        private static string CVolumeSerial()
        {
            var disk = new ManagementObject(@"win32_logicaldisk.deviceid=""c:""");
            disk.Get();

            string volumeSerial = disk["VolumeSerialNumber"].ToString();
            disk.Dispose();

            return volumeSerial;
        }

        /// <summary>
        /// Returns the id of the first CPU listed by WMI
        /// </summary>
        /// <returns></returns>
        private static string FirstCpuId()
        {
            var mClass = new ManagementClass("win32_processor");

            foreach (var obj in mClass.GetInstances())
            {
                return obj.Properties["processorID"].Value.ToString();
            }
            return "";
        }


        public static void RunCommandTillEnd(string filename, string arguments, List<string> stdout, List<string> stderr, Logger logger)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();

            process.StartInfo.FileName = filename;
            process.StartInfo.Arguments = arguments;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += (sender, eventArgs) => CommandOutputReceived(sender, eventArgs, stdout);
            process.ErrorDataReceived += (sender, eventArgs) => CommandOutputReceived(sender, eventArgs, stderr);

            logger.Log("Abbout to start: {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception e)
            {
                logger.Log("Got an exception while starting the process");
                logger.Log(e.ToString());

                return;
            }

            process.WaitForExit();
        }

        private static void CommandOutputReceived(object sender, System.Diagnostics.DataReceivedEventArgs e, List<string> output)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                output.Add(e.Data);
            }
        }

        /// <summary>
        /// Determines whether or not the given prospective HomeId is valid.
        /// </summary>
        /// <param name="input">The prospective HomeId.</param>
        /// <returns>
        /// True if the prospective HomeId is valid, false otherwise.
        /// </returns>
        public static bool IsValidHomeId(string homeId)
        {
            return IsValidHomeId(Encoding.ASCII.GetBytes(homeId));
        }

        /// <summary>
        /// Determines whether or not the given prospective HomeId is valid.
        /// </summary>
        /// <param name="input">The prospective HomeId.</param>
        /// <returns>
        /// True if the prospective HomeId is valid, false otherwise.
        /// </returns>
        private static bool IsValidHomeId(byte[] input)
        {
            ArraySegment<byte> test = new ArraySegment<byte>(input);
            return IsValidHomeId(test);
        }

        /// <summary>
        /// Determines whether or not a prospective HomeId is valid.
        /// </summary>
        /// <remarks>
        /// We only allow ASCII alpha-numeric characters in HomeIds.
        /// </remarks>
        /// <param name="input">
        /// A byte array segment containing the prospective HomeId.
        /// </param>
        /// <returns>
        /// True if the prospecive HomeId is valid, false otherwise.
        /// </returns>
        private static bool IsValidHomeId(ArraySegment<byte> input)
        {
            for (int index = input.Offset;
                index < input.Offset + input.Count;
                index++)
            {
                byte test = input.Array[index];
                if ((test < 48) ||
                    (test > 122) ||
                    ((test > 57) && (test < 65)) ||
                    ((test > 90) && (test < 97)))
                {
                    return false;
                }
            }

            return true;
        }

        #region file and directory operations 
        public static void CleanDirectory(VLogger logger, string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                    return;

                DirectoryInfo dir = new DirectoryInfo(directory);
                foreach (FileInfo fi in dir.GetFiles())
                {
                    fi.Delete();
                }

                foreach (DirectoryInfo di in dir.GetDirectories())
                {
                    Utils.CleanDirectory(logger,di.FullName);
                    di.Delete();
                }
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger,"E", e.Message + ". CleanDirectory, directory:" + directory);
            }
        }

        public static void DeleteDirectory(VLogger logger, string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Utils.CleanDirectory(logger,directory);
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger,"E", e.Message + ". DeleteDirectory, directory :" + directory);
            }

        }

        public static string CreateDirectory(VLogger logger, String completePath)
        {
            try
            {
                if (Directory.Exists(completePath))
                {
                    Utils.CleanDirectory(logger, completePath);
                    Directory.Delete(completePath, true);
                }

                Directory.CreateDirectory(completePath);
                DirectoryInfo info = new DirectoryInfo(completePath);
                System.Security.AccessControl.DirectorySecurity security = info.GetAccessControl();
                security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(Environment.UserName, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.InheritanceFlags.ContainerInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
                security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(Environment.UserName, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.InheritanceFlags.ContainerInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
                info.SetAccessControl(security);
                return completePath;
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger,"E", e.Message + ". CreateDirectory, completePath:" + completePath);
            }

            return null;
        }

        public static void DeleteFile(VLogger logger, string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger,"E", e.Message + ". DeleteFile, filePath:" + filePath);
            }
        }

        public static List<string> ListFiles(VLogger logger, string directory)
        {
            List<string> retVal = new List<string>();
            try
            {
                foreach (string f in Directory.GetFiles(directory))
                    retVal.Add(Path.GetFileName(f));
            }
            catch (Exception e)
            {
                if (null != logger)
                {
                    Utils.structuredLog(logger, "E", e.Message + ". ListFiles, directory: " + directory);
                }
            }
            return retVal;
        }

        public static void CopyDirectory(VLogger logger, string sourceDir, string destDir)
        {
            try
            {
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                string[] files = Directory.GetFiles(sourceDir);
                foreach (string file in files)
                {
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(destDir, name);
                    File.Copy(file, dest, true);
                }
                string[] folders = Directory.GetDirectories(sourceDir);
                foreach (string folder in folders)
                {
                    string name = Path.GetFileName(folder);
                    string dest = Path.Combine(destDir, name);
                    Utils.CopyDirectory(logger, folder, dest);
                }
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger,"E", e.Message + ". CopyDirectory, sourceDir: " + sourceDir + ", destDir:" + destDir);
            }
        }

        public static bool UnpackZip(VLogger logger, String zipPath, string extractPath)
        {
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
                return true;
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger,"E", e.Message + ". UnpackZip, zipPath: " + zipPath + ", extractPath:" + extractPath);
                return false;
            }

        }

        public static bool PackZip(VLogger logger, string startPath, String zipPath)
        {
            try
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
                System.IO.Compression.ZipFile.CreateFromDirectory(startPath, zipPath);
                return true;
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger,"E", e.Message + ". PackZip, startPath: " + startPath + ", zipPath:" + zipPath);
                return false;
            }

        }

        public static string GetMD5HashOfFile(VLogger logger, string filePath)
        {
            try
            {
                FileStream file = new FileStream(filePath, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger,"E", e.Message + ". GetMD5HashOfFile(), file" + filePath);
                return "";
            }
        }

        public static string ReadFile(VLogger logger, string filePath)
        {
            try
            {

                System.IO.StreamReader myFile = new System.IO.StreamReader(filePath);
                string myString = myFile.ReadToEnd();
                myFile.Close();
                return myString;
            }
            catch (Exception e)
            {
                if (null != logger)
                {
                    Utils.structuredLog(logger, "E", e.Message + ". GetMD5HashOfFile(), file" + filePath);
                }
                return "";
            }
        }

        public static string WriteToFile(VLogger logger, string filePath, string text)
        {
            try
            {
                System.IO.StreamReader myFile = new System.IO.StreamReader(filePath);
                string myString = myFile.ReadToEnd();
                myFile.Close();
                return myString;
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger, "E", e.Message + ". WriteToFile "+filePath+" "+ text);
                return "";
            }

        }

        public static bool CopyFile(VLogger logger, string filePath1, string filePath2)
        {
            if (File.Exists(filePath2))
                Utils.DeleteFile(logger, filePath2);
            try
            {
                File.Copy(filePath1, filePath2);
                return true;
            }
            catch (Exception e)
            {
                Utils.structuredLog(logger, "E", e.Message + ". CopyFile(), file" + filePath1 + " to "+filePath2);
                return false; ;
            }
        }
        #endregion


        public static void structuredLog(VLogger logger, string type, params string[] messages)
        {
            if (type == "ER") type = "ERROR";
            else if (type == "I") type = "INFO";
            else if (type == "E") type = "EXCEPTION";
            else if (type == "W") type = "WARNING";

            StringBuilder s = new StringBuilder();
            s.Append("[ConfigUpdater]" + "[" + type + "]");
            foreach (string message in messages)
                s.Append("[" + message + "]");
            logger.Log(s.ToString());
        }

        private static Notification BuildNotification(string dest, string subject, string body, List<Attachment> attachmentList)
        {
            Notification notification = null;

            if (string.IsNullOrWhiteSpace(dest))
            {
                return notification;
            }

            notification = new Notification();

            notification.toAddress = dest;
            notification.subject = subject;
            notification.body = body;
            notification.attachmentList = attachmentList;

            return notification;
        }

        /// <summary>
        /// Send email by trying to send from Hub first, if that fails, send using cloud relay.
        /// </summary>
        /// <param name="dst">to:</param>
        /// <param name="subject">subject</param>
        /// <param name="body">body of the message</param>
        /// <returns>A tuple with true/false success and string exception message (if any)</returns>
        public static Tuple<bool, string> SendEmail(string dst, string subject, string body, List<Attachment> attachmentList, VPlatform platform, VLogger logger)
        {
            logger.Log("Utils.SendEmail called with  " + dst + " " + subject + " " + body);

            Tuple<bool, string> result = SendHubEmail(dst, subject, body, attachmentList, platform, logger);
            if (!result.Item1)
            {
                logger.Log("SendHubEmail failed with error={0}", result.Item2);
                result = SendCloudEmail(dst, subject, body, attachmentList, platform, logger);
            }
            return result;
        }
            
        /// <summary>
        /// Send email from Hub.
        /// </summary>
        /// <param name="dst">to:</param>
        /// <param name="subject">subject</param>
        /// <param name="body">body of the message</param>
        /// <returns>A tuple with true/false success and string exception message (if any)</returns>
        public static Tuple<bool, string> SendHubEmail(string dst, string subject, string body, List<Attachment> attachmentList, VPlatform platform, VLogger logger)
        {
            string error = "";
            logger.Log("Utils.SendHubEmail called with " + dst + " " + subject + " " + body);

            string smtpServer = platform.GetPrivateConfSetting("SmtpServer");
            string smtpUser = platform.GetPrivateConfSetting("SmtpUser");
            string smtpPassword = platform.GetPrivateConfSetting("SmtpPassword");
            string bodyLocal;

            Emailer emailer = new Emailer(smtpServer, smtpUser, smtpPassword, logger);

            if (string.IsNullOrWhiteSpace(body))
            {
                bodyLocal = platform.GetPrivateConfSetting("NotificationEmail");
            }
            else
            {
                bodyLocal = body;
            }

            Notification notification = BuildNotification(dst, subject, body, attachmentList);
            if (null == notification)
            {
                error = "Destination for the email not set";
                logger.Log(error);
                return new Tuple<bool, string>(false, error);
            }

            return emailer.Send(notification);
        }

        public static Tuple<bool, string> SendCloudEmail(string dst, string subject, string body, List<Attachment> attachmentList, VPlatform platform, VLogger logger)
        {
            string error = "";
            logger.Log("Utils.SendCloudEmail called with " + dst + " " + subject + " " + body);

            string smtpServer = platform.GetPrivateConfSetting("SmtpServer");
            string smtpUser = platform.GetPrivateConfSetting("SmtpUser");
            string smtpPassword = platform.GetPrivateConfSetting("SmtpPassword");
            Uri serviceHostUri = new Uri("https://" + platform.GetConfSetting("EmailServiceHost") + ":" + Shared.Constants.EmailServiceSecurePort + "/" +
                                   Shared.Constants.EmailServiceWcfEndPointUrlSuffix);

            string bodyLocal;

            CloudEmailer emailer = new CloudEmailer(serviceHostUri, smtpServer, smtpUser, smtpPassword, logger);

            if (string.IsNullOrWhiteSpace(body))
            {
                bodyLocal = platform.GetPrivateConfSetting("NotificationEmail");
            }
            else
            {
                bodyLocal = body;
            }

            Notification notification = BuildNotification(dst, subject, body, attachmentList);
            if (null == notification)
            {
                error = "Destination for the email not set";
                logger.Log(error);
                return new Tuple<bool, string>(false, error);
            }

            return emailer.Send(notification);
        }

        public static byte[] CreateTestJpegImage(
                int maxXCells,
                int maxYCells,
                int cellXPosition,
                int cellYPosition,
                int boxSize)
        {
            using (var bmp = new System.Drawing.Bitmap(maxXCells * boxSize + 1, maxYCells * boxSize + 1))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Yellow);
                    Pen pen = new Pen(Color.Black);
                    pen.Width = 1;

                    //Draw red rectangle to go behind cross
                    Rectangle rect = new Rectangle(boxSize * (cellXPosition - 1), boxSize * (cellYPosition - 1), boxSize, boxSize);
                    g.FillRectangle(new SolidBrush(Color.Red), rect);

                    //Draw cross
                    g.DrawLine(pen, boxSize * (cellXPosition - 1), boxSize * (cellYPosition - 1), boxSize * cellXPosition, boxSize * cellYPosition);
                    g.DrawLine(pen, boxSize * (cellXPosition - 1), boxSize * cellYPosition, boxSize * cellXPosition, boxSize * (cellYPosition - 1));

                    //Draw horizontal lines
                    for (int i = 0; i <= maxXCells; i++)
                    {
                        g.DrawLine(pen, (i * boxSize), 0, i * boxSize, boxSize * maxYCells);
                    }

                    //Draw vertical lines            
                    for (int i = 0; i <= maxYCells; i++)
                    {
                        g.DrawLine(pen, 0, (i * boxSize), boxSize * maxXCells, i * boxSize);
                    }
                }

                var memStream = new MemoryStream();
                bmp.Save(memStream, ImageFormat.Jpeg);
                return memStream.ToArray();
            }
        }

        #region argument printing and handling

        public static void configLog(string type, params string[] messages)
        {
            if (type == "ER") type = "ERROR";
            else if (type == "I") type = "INFO";
            else if (type == "E") type = "EXCEPTION";
            else if (type == "W") type = "WARNING";

            StringBuilder s = new StringBuilder();
            s.Append("[" + type + "]");
            foreach (string message in messages)
                s.Append("[" + message + "]");
            Console.WriteLine(s.ToString());
        }

        /// <summary>
        /// die after printing given message
        /// </summary>
        /// <param name="message"></param>
        public static void die(string message)
        {
            Console.WriteLine(message);
            System.Environment.Exit(0);
        }

        public static string missingArgumentMessage(string argumentName)
        {
            return "Argument: " + argumentName + " is missing. Use --Help for help.";
        }

        public static void printArgumentsDictionary(ArgumentsDictionary dict)
        {
            foreach (string key in dict.Keys)
            {
                Console.WriteLine(key + " : " + dict[key]);
            }

        }
        #endregion 
    }
}
