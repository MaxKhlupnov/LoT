using System;
using System.ComponentModel;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.AddIn;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media.Imaging;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using SmartRecorder;
using System.ServiceModel;

namespace HomeOS.Hub.Apps.SmartCam
{
    enum MediaType
    {
        MediaType_Video_MP4,
        MediaType_Image_JPEG
    };

    class CameraInfo
    {
        public VCapability Capability { get; set; }
        public byte[] LastImageBytes { get; set; }
        public Bitmap BitmapImage { get; set; }
        public VideoWriter VideoWriter { get; set; }
        public ObjectDetector ObjectDetector { get; set; }
        public bool ObjectFound { get; set; }
        public Rectangle LastObjectRect { get; set; }
        public BackgroundWorker BackgroundWorkerObjectDetector { get; set; }
        public DateTime CurrVideoStartTime { get; set; }
        public DateTime CurrVideoEndTime { get; set; }
        public bool RecordVideo { get; set; }
        public bool EnableObjectTrigger { get; set; }
        public bool UploadVideo { get; set; }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    [ServiceKnownType(typeof(SmartCam.CameraControl))]
    [AddIn("HomeOS.Hub.Apps.SmartCam")]
    public class SmartCam : ModuleBase
    {
        const int VIDEO_FPS_NUM = 24;
        const int VIDEO_FPS_DEN = 1;
        const int VIDEO_ENC_FRAMERATE = 240000;
        static TimeSpan DEFAULT_VIDEO_CLIP_LEN = new TimeSpan(0, 0, 10); //30 seconds

        const string VIDEO_SUB_DIR_NAME = "videos";

        public enum CameraControl { Left, Right, Up, Down, ZoomIn, ZoomOut };

        private SafeServiceHost serviceHost;

        private Dictionary<VPort, CameraInfo> registeredCameras = new Dictionary<VPort, CameraInfo>();
        private Dictionary<string, VPort> cameraFriendlyNames = new Dictionary<string, VPort>();


        private WebFileServer appServer;
        private WebFileServer recordingServer;

        string videosDir;
        string videosBaseUrl;

        public override void Start()
        {
            logger.Log("Started: {0}", ToString());

            SmartCamSvc service = new SmartCamSvc(logger, this);
            serviceHost = new SafeServiceHost(logger, typeof(ISmartCamContract), service, this, Constants.AjaxSuffix, moduleInfo.BaseURL());
            serviceHost.Open();

            appServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);

            this.videosDir = moduleInfo.WorkingDir() + "\\" + VIDEO_SUB_DIR_NAME;
            this.videosBaseUrl = moduleInfo.BaseURL() + "/" + VIDEO_SUB_DIR_NAME;

            recordingServer = new WebFileServer(videosDir, videosBaseUrl, logger);

            logger.Log("camera service is open for business at " + moduleInfo.BaseURL());

            //..... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();

            if (allPortsList != null)
            {
                foreach (VPort port in allPortsList)
                {
                    PortRegistered(port);
                }
            }
        }

        public override void Stop()
        {
            lock (this)
            {
                if (serviceHost != null)
                    serviceHost.Close();

                //close all windows
                foreach (VPort cameraPort in registeredCameras.Keys)
                {
                    StopRecording(cameraPort, true /* force */);
                }

                if (appServer != null)
                    appServer.Dispose();

                if (recordingServer != null)
                    recordingServer.Dispose();
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        /// <param name="port"></param>
        public override void PortRegistered(VPort port)
        {
            lock (this)
            {
                if (Role.ContainsRole(port, RoleCamera.RoleName))
                {
                    if (!registeredCameras.ContainsKey(port))
                    {
                        InitCamera(port);
                    }
                    else
                    {
                        //the friendly name of the port might have changed. update that.
                        string oldFriendlyName = null;

                        foreach (var pair in cameraFriendlyNames)
                        {
                            if (pair.Value.Equals(port) &&
                                !pair.Key.Equals(port.GetInfo().GetFriendlyName()))
                            {
                                oldFriendlyName = pair.Key;
                                break;
                            }
                        }

                        if (oldFriendlyName != null)
                        {
                            cameraFriendlyNames.Remove(oldFriendlyName);
                            cameraFriendlyNames.Add(port.GetInfo().GetFriendlyName(), port);
                        }
                    }

                }
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        /// <param name="cameraPort"></param>
        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (Role.ContainsRole(port, RoleCamera.RoleName))
                {
                    if (registeredCameras.ContainsKey(port))
                        ForgetCamera(port);
                }
            }
        }

        public byte[] GetImage(string friendlyName)
        {
            lock (this)
            {
                if (cameraFriendlyNames.ContainsKey(friendlyName))
                    return registeredCameras[cameraFriendlyNames[friendlyName]].LastImageBytes;

                throw new Exception("Unknown camera " + friendlyName);
            }
        }

        public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        {
            if (registeredCameras.ContainsKey(senderPort))
            {
                if (retVals.Count >= 1 && retVals[0].Value() != null)
                {
                    byte[] imageBytes = (byte[])retVals[0].Value();

                    lock (this)
                    {
                        registeredCameras[senderPort].LastImageBytes = imageBytes;

                        if (registeredCameras[senderPort].RecordVideo ||
                            registeredCameras[senderPort].EnableObjectTrigger)
                        {
                            bool addFrame = false;
                            Rectangle rectObject = new Rectangle(0, 0, 0, 0);
                            MemoryStream stream = new MemoryStream(imageBytes);
                            Bitmap image = null;
                            image = (Bitmap)Image.FromStream(stream);
                            if (null != registeredCameras[senderPort].BitmapImage)
                            {
                                registeredCameras[senderPort].BitmapImage.Dispose();
                                registeredCameras[senderPort].BitmapImage = null;
                            }
                            registeredCameras[senderPort].BitmapImage = image;

                            //lets check if the image is what we expect
                            if (image.PixelFormat != PixelFormat.Format24bppRgb)
                            {
                                string message = String.Format("Image format from {0} is not correct. PixelFormat: {1}",
                                                                senderPort.GetInfo().GetFriendlyName(), image.PixelFormat);
                                logger.Log(message);

                                return;
                            }

                            // stop if needed
                            StopRecording(senderPort, false /* force*/);

                            //// if recording is underway don't bother that, it will stop after that clip time lapses
                            //// if recording needs to be done only on motion (object) triggers, check with the result of the object
                            //// detector above
                            //if (registeredCameras[senderPort].RecordVideo)
                            //{
                            //    //if record video is still true, see if we need to add his frame
                            //    if (registeredCameras[senderPort].VideoWriter != null || !registeredCameras[senderPort].EnableObjectTrigger)
                            //    {
                            //        addFrame = true;
                            //    }
                            //    else
                            //    {
                            //        if (registeredCameras[senderPort].ObjectFound)
                            //            addFrame = true;
                            //    }
                            //}

                            if (registeredCameras[senderPort].RecordVideo)
                            {
                                addFrame = true;
                            }
                            else
                            {
                                if (registeredCameras[senderPort].EnableObjectTrigger &&
                                    registeredCameras[senderPort].ObjectFound)
                                    addFrame = true;
                            }

                            if (addFrame)
                            {

                                StartRecording(senderPort, image.Width, image.Height, VIDEO_FPS_NUM, VIDEO_FPS_DEN, VIDEO_ENC_FRAMERATE);

                                long sampleTime = (DateTime.Now - registeredCameras[senderPort].CurrVideoStartTime).Ticks;

                                AddFrameToVideo(image, senderPort, sampleTime);

                                if (registeredCameras[senderPort].ObjectFound)
                                {
                                    registeredCameras[senderPort].ObjectFound = false;
                                    rectObject = registeredCameras[senderPort].LastObjectRect;
                                    WriteObjectImage(senderPort, image, rectObject, true /* center */);
                                }

                            }
                        }
                    }
                }
                else
                {
                    logger.Log("{0} got null image", this.ToString());
                }
            }
        }

        private void backgroundObjectDetector_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            do
            {
                bool foundObject = false;
                Rectangle rectObject = new Rectangle(0, 0, 0, 0);
                VPort cameraPort = (VPort)e.Argument;
                Bitmap image = null;

                lock (this)
                {
                    if (registeredCameras[cameraPort].BitmapImage != null)
                    {
                        try
                        {
                            image = (Bitmap)registeredCameras[cameraPort].BitmapImage.Clone();
                        }
                        catch (Exception)
                        {
                            logger.Log("BitmapImage Clone threw an exception!");
                        }
                        registeredCameras[cameraPort].BitmapImage.Dispose();
                        registeredCameras[cameraPort].BitmapImage = null;
                    }
                }

                if (null != image)
                {
                    foundObject = ExtractObjectFromFrame(image, cameraPort, ref rectObject);
                }

                lock (this)
                {
                    registeredCameras[cameraPort].ObjectFound = foundObject;
                    registeredCameras[cameraPort].LastObjectRect = rectObject;
                }

                if (null != image)
                {
                    image.Dispose();
                }
            }
            while (!worker.CancellationPending);

            e.Cancel = true;
        }


        //returns [cameraName, roleName] pairs
        public List<string> GetCameraList()
        {
            List<string> retList = new List<string>();

            lock (this)
            {
                foreach (var camera in cameraFriendlyNames.Keys)
                {
                    string bestRoleSoFar = RoleCamera.RoleName;

                    foreach (var role in cameraFriendlyNames[camera].GetInfo().GetRoles())
                    {
                        if (Role.ContainsRole(role.Name(), bestRoleSoFar))
                            bestRoleSoFar = role.Name();
                    }

                    retList.Add(camera);
                    retList.Add(bestRoleSoFar);
                }
            }

            return retList;
        }

        public void EnableMotionTrigger(string cameraFriendlyName, bool enable)
        {
            VPort cameraPort = cameraFriendlyNames[cameraFriendlyName];

            registeredCameras[cameraPort].EnableObjectTrigger = enable;


            // setup a background worker for object detection
            if (enable)
            {
                if (null == registeredCameras[cameraPort].BackgroundWorkerObjectDetector)
                {
                    registeredCameras[cameraPort].BackgroundWorkerObjectDetector = new BackgroundWorker();
                    registeredCameras[cameraPort].BackgroundWorkerObjectDetector.WorkerSupportsCancellation = true;
                    registeredCameras[cameraPort].BackgroundWorkerObjectDetector.DoWork +=
                        new DoWorkEventHandler(backgroundObjectDetector_DoWork);
                    registeredCameras[cameraPort].BackgroundWorkerObjectDetector.RunWorkerAsync(cameraPort);
                }
            }


            if (!enable)
            {
                if (registeredCameras[cameraPort].BackgroundWorkerObjectDetector != null)
                {
                    registeredCameras[cameraPort].BackgroundWorkerObjectDetector.CancelAsync();
                    registeredCameras[cameraPort].BackgroundWorkerObjectDetector = null;
                }

                StopRecording(cameraPort, true /* force */);
            }
        }

     

        public bool IsMotionTriggerEnabled(string cameraFriendlyName)
        {
            bool isEnabled = false;
            VPort cameraPort = cameraFriendlyNames[cameraFriendlyName];
            unsafe
            {
                isEnabled = registeredCameras[cameraPort].EnableObjectTrigger;
            }

            return isEnabled;
        }


        public void EnableVideoUpload(string cameraFriendlyName, bool enable)
        {
            VPort cameraPort = cameraFriendlyNames[cameraFriendlyName];

             registeredCameras[cameraPort].UploadVideo = enable;

            //If video upload is enabled then when recording is stopped program will upload video and snapshots

        }


        public bool IsVideoUploadEnabled(string cameraFriendlyName)
        {
            bool isVideoUploadEnabled = false;
            VPort cameraPort = cameraFriendlyNames[cameraFriendlyName];
            unsafe
            {
                isVideoUploadEnabled = registeredCameras[cameraPort].UploadVideo;
            }

            return isVideoUploadEnabled;
        }

        public string[] GetRecordedCamerasList()
        {

            //string directory = String.Format("{0}\\videos", moduleInfo.WorkingDir());
            //string[] cameraDirsArray = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);

            string[] cameraDirsArray = Directory.GetDirectories(this.videosDir, "*", SearchOption.TopDirectoryOnly);

            int count = 0;
            foreach (string cameraDir in cameraDirsArray)
            {

                int len = cameraDir.Length;
                int lastBackSlash = cameraDir.LastIndexOf('\\');
                if (lastBackSlash != len - 1)
                {
                    cameraDirsArray[count++] = cameraDir.Substring(lastBackSlash + 1, len - lastBackSlash - 1);
                }
            }

            return cameraDirsArray;
        }

        public int GetRecordedClipsCount(string cameraFriendlyName)
        {
            string directory = String.Format("{0}\\{1}", this.videosDir, cameraFriendlyName);

            string[] fileArray = Directory.GetFiles(directory, "*.mp4", SearchOption.AllDirectories);
            return fileArray.GetLength(0);
        }

        private bool IsFileLocked(string filePath)
        {
            bool locked = false;
            try
            {
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None)) { }
            }
            catch (Exception)
            {
                locked = true;
            }

            return locked;
        }


        public string[] GetRecordedClips(string cameraFriendlyName, int countMax)
        {
            string directory = String.Format("{0}\\{1}", this.videosDir, cameraFriendlyName);
            string[] fileArray;

            try
            {
                fileArray = Directory.GetFiles(directory, "*.mp4", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                fileArray = new string[0];
            }

            List<string> fileExcludeList = new List<string>();

            // remove any clip that is still being written to
            for (int i = 0; i < fileArray.Length; ++i)
            {
                if (IsFileLocked(fileArray[i]))
                {
                    fileExcludeList.Add(fileArray[i]);
                }
            }

            for (int j = 0; j < fileExcludeList.Count; ++j)
            {
                fileArray = Array.FindAll<string>(fileArray, (f) => f != fileExcludeList[j]);
            }

            // sort the file array by their time stamp, earliest first
            Array.Sort<string>(fileArray, (f1, f2) => File.GetLastWriteTime(f1).CompareTo(File.GetLastWriteTime(f2)) * -1);

            ConvertLocalPathArrayToUrlArray(fileArray, countMax);

            if (fileArray.Length > countMax)
            {
                Array.Resize<string>(ref fileArray, countMax);
            }

            return fileArray;
        }

        // morph the fileArray into the externally accessible Url array
        private void ConvertLocalPathArrayToUrlArray(string[] fileArray, int countMax)
        {
            int count = 0;
            foreach (string filePath in fileArray)
            {
                int len = filePath.Length;
                string substr = /*"\\" +*/ VIDEO_SUB_DIR_NAME;
                int postVideosPosition = filePath.LastIndexOf(substr) + substr.Length + 1;
                fileArray[count++] = /*this.videosBaseUrl+*/ substr +  "/" + filePath.Substring(postVideosPosition, len - postVideosPosition);
                fileArray[count - 1] = fileArray[count - 1].Replace("\\", "/");

                if (count == countMax)
                    break;
            }
        }

        private string[] GetTriggerImagesFromClipUrl(string clipUrl)
        {
            // extract the sub string from the url that contains the relative location of the clip on disk
            // typical clip url is "http://<adddress:port>/<home id>/SmartCamApp/videos/<camera name>/<YYYY-MM-DD>/<hh-mm-ss>.mp4"

            //int idxRelClipPath = clipUrl.IndexOf(String.Format("/{0}/{1}/", moduleInfo.FriendlyName(), VIDEO_SUB_DIR_NAME));
            //int subStringLen = String.Format("{0}/{1}/", moduleInfo.FriendlyName(), VIDEO_SUB_DIR_NAME).Length;
            //string relClipPath = clipUrl.Substring(idxRelClipPath + subStringLen + 1);

            string relClipPath = clipUrl.Substring(this.videosBaseUrl.Length);

            relClipPath = relClipPath.Replace('/', '\\');
            string clipPath = String.Format("{0}\\{1}", this.videosDir, relClipPath);

            FileInfo clipFileInfo = new FileInfo(clipPath);
            string clipDirPath = clipFileInfo.DirectoryName;

            // get all jpgs in the same directory as the specified clip
            string[] triggerImagesArray = Directory.GetFiles(clipDirPath, "*.jpg", SearchOption.TopDirectoryOnly);

            // filter the jpeg files so that the ones whose write time is less than MAX_VIDEO_CLIP_LEN_IN_MINUTES minutes of the last
            // write time of the video clip 
            triggerImagesArray = Array.FindAll<string>(triggerImagesArray, (f) =>
                    (File.GetLastWriteTime(f).CompareTo(clipFileInfo.LastWriteTime) <= 0) &&
                    (File.GetLastWriteTime(f).CompareTo(clipFileInfo.LastWriteTime - DEFAULT_VIDEO_CLIP_LEN) >= 0));
            //(File.GetLastWriteTime(f).CompareTo(clipFileInfo.LastWriteTime - new TimeSpan(0, MAX_VIDEO_CLIP_LEN_IN_MINUTES, 0)) >= 0));

            // sort the file array by their time stamp, most recent first
            Array.Sort<string>(triggerImagesArray, (f1, f2) => File.GetLastWriteTime(f1).CompareTo(File.GetLastWriteTime(f2)) * -1);

            ConvertLocalPathArrayToUrlArray(triggerImagesArray, -1);

            return triggerImagesArray;
        }

        public int GetClipTriggerImagesCount(string clipUrl)
        {
            return GetTriggerImagesFromClipUrl(clipUrl).Length;
        }

        public string[] GetClipTriggerImages(string clipUrl, int countMax)
        {
            string[] triggerImagesArray = GetTriggerImagesFromClipUrl(clipUrl);

            if (triggerImagesArray.Length == 0)
            {
                return new string[0]; // don't return null for arrays
            }
            else if (countMax < triggerImagesArray.Length)
            {
                Array.Resize<string>(ref triggerImagesArray, countMax);
                return triggerImagesArray;
            }
            else  // countMax >= triggerImagesArray.Length
            {
                // don't fail for case when countMax is greater than the total count of clips
                // because it forces an additional async call in script which can be avoided
                return triggerImagesArray;
            }
        }

        public void SendEmail(string toAddress, string subject, string body, string[] attachmentUrls)
        {
            Tuple<bool,string> result = base.SendEmail(toAddress, subject, body, null);

            if (result.Item1)
            {
                logger.Log("Email notification succeeded");
            }
            else
            {
                logger.Log("Email notification failed with error:{0}", result.Item2);
            }
        }        


        public void SendMsgToCamera(string cameraControl, string cameraFriendlyName)
        {
            if (cameraFriendlyNames.ContainsKey(cameraFriendlyName))
            {
                VPort cameraPort = cameraFriendlyNames[cameraFriendlyName];

                if (registeredCameras.ContainsKey(cameraPort))
                {
                    IList<VParamType> retVal = null;

                    if (cameraControl.Equals(RolePTZCamera.OpZommOutName) || cameraControl.Equals(RolePTZCamera.OpZoomInName))
                    {
                        retVal = cameraPort.Invoke(RolePTZCamera.RoleName, cameraControl, new List<VParamType>(),
                               ControlPort, registeredCameras[cameraPort].Capability, ControlPortCapability);
                    }
                    else
                    {
                        retVal = cameraPort.Invoke(RolePTCamera.RoleName, cameraControl, new List<VParamType>(),
                                           ControlPort, registeredCameras[cameraPort].Capability, ControlPortCapability);
                    }

                    if (retVal.Count != 0 && retVal[0].Maintype() == (int)ParamType.SimpleType.error)
                    {
                        logger.Log("Got error while controlling camera {0} with controlType {1}: {2}", cameraFriendlyName, cameraControl, retVal[0].Value().ToString());
                    }

                }
            }
        }

        // Starts a new recording if there isn't one already under way
        private void StartRecording(VPort cameraPort, int videoWidth, int videoHeight, int videoFPSNum, int videoFPSDen, int videoEncBitrate)
        {
            if (registeredCameras[cameraPort].VideoWriter != null)
            {
                return;
            }

            logger.Log("Started new clip for {0}", cameraPort.GetInfo().GetFriendlyName());
            CameraInfo cameraInfo = registeredCameras[cameraPort];

            string fileName = GetMediaFileName(cameraPort.GetInfo().GetFriendlyName(), MediaType.MediaType_Video_MP4);

            if (null == registeredCameras[cameraPort].VideoWriter)
            {
                registeredCameras[cameraPort].VideoWriter = new VideoWriter();
            }

            cameraInfo.CurrVideoStartTime = DateTime.Now;
            cameraInfo.CurrVideoEndTime = cameraInfo.CurrVideoStartTime + DEFAULT_VIDEO_CLIP_LEN;

            int result = cameraInfo.VideoWriter.Init(fileName, videoWidth, videoHeight, videoFPSNum, videoFPSDen, videoEncBitrate);

            if (result != 0)
            {
                string message = String.Format("Failed to start recording for {0} at {1}. Error code = {2}",
                                                cameraPort.GetInfo().GetFriendlyName(), DateTime.Now, result);
                logger.Log(message);
            }
        }

        private void StopRecording(VPort cameraPort, bool force)
        {
            bool stopConditionMet = false;
            CameraInfo cameraInfo = registeredCameras[cameraPort];

            //if ((DateTime.Now - registeredCameras[cameraPort].CurrVideoStartTime).TotalMinutes >=
            //            MAX_VIDEO_CLIP_LEN_IN_MINUTES)

            if (DateTime.Now >= registeredCameras[cameraPort].CurrVideoEndTime)
            {
                stopConditionMet = true;
            }

            if ((force || stopConditionMet) && (cameraInfo.VideoWriter != null))
            {
                string cameraName = cameraPort.GetInfo().GetFriendlyName();
                VideoWriter VideoWriter = cameraInfo.VideoWriter;

                SafeThread helper = new SafeThread(delegate() { StopRecordingHelper(VideoWriter, cameraName); },
                                                    "stoprecordinghelper-" + cameraName, logger);
                helper.Start();

                cameraInfo.RecordVideo = false;
                cameraInfo.VideoWriter = null;
                cameraInfo.CurrVideoStartTime = DateTime.MinValue;
                cameraInfo.CurrVideoEndTime = DateTime.MinValue;

                if (stopConditionMet)
                {
                    logger.Log("Stop recording because the clip time has elapsed for {0}",
                            cameraPort.GetInfo().GetFriendlyName());
                }
                else
                {
                    logger.Log("Stop recording for {0}", cameraPort.GetInfo().GetFriendlyName());
                }
            }
        }

        public void StartOrContinueRecording(string cameraFriendlyName)
        {
            VPort cameraPort = cameraFriendlyNames[cameraFriendlyName];

            var cameraInfo = registeredCameras[cameraPort];

            cameraInfo.RecordVideo = true;

            //if recording is going on, but the end time is less than clip length in the future, extend the end time
            if (cameraInfo.VideoWriter != null &&
                cameraInfo.CurrVideoEndTime < DateTime.Now + DEFAULT_VIDEO_CLIP_LEN)
            {
                cameraInfo.CurrVideoEndTime = DateTime.Now + DEFAULT_VIDEO_CLIP_LEN;
            }
        }

        private void StopRecordingHelper(VideoWriter VideoWriter, string cameraName)
        {
            DateTime startTime = DateTime.Now;
            int hresult = VideoWriter.Done();
            logger.Log("Stopping took {0} ms", (DateTime.Now - startTime).TotalMilliseconds.ToString());

            if (hresult != 0)
            {
                string message = String.Format("Failed to stop recording for {0} at {1}. Error code = {2:x}",
                                                cameraName, DateTime.Now, (uint)hresult);
                logger.Log(message);

            }

            logger.Log("stopped recording for {0}", cameraName);
        }

        public void StopRecording(string cameraFriendlyName)
        {
            VPort cameraPort = cameraFriendlyNames[cameraFriendlyName];
            StopRecording(cameraPort, true);
        }

        //called when the lock is acquired
        private void ForgetCamera(VPort cameraPort)
        {
            cameraFriendlyNames.Remove(cameraPort.GetInfo().GetFriendlyName());

            //stop recording if we have a video make object
            StopRecording(cameraPort, true);

            registeredCameras.Remove(cameraPort);

            logger.Log("{0} removed camera port {1}", this.ToString(), cameraPort.ToString());

        }

        //called when the lock is acquired and cameraPort is non-existent in the dictionary
        private void InitCamera(VPort cameraPort)
        {
            VCapability capability = GetCapability(cameraPort, Constants.UserSystem);

            //return if we didn't get a capability
            if (capability == null)
            {
                logger.Log("{0} didn't get a capability for {1}", this.ToString(), cameraPort.ToString());

                return;
            }

            //otherwise, add this to our list of cameras

            logger.Log("{0} adding camera port {1}", this.ToString(), cameraPort.ToString());

            CameraInfo cameraInfo = new CameraInfo();
            cameraInfo.Capability = capability;
            cameraInfo.LastImageBytes = new byte[0];
            cameraInfo.VideoWriter = null;
            cameraInfo.CurrVideoStartTime = DateTime.MinValue;
            cameraInfo.CurrVideoEndTime = DateTime.MinValue;

            registeredCameras.Add(cameraPort, cameraInfo);

            string cameraFriendlyName = cameraPort.GetInfo().GetFriendlyName();
            cameraFriendlyNames.Add(cameraFriendlyName, cameraPort);

            cameraPort.Subscribe(RoleCamera.RoleName, RoleCamera.OpGetVideo, ControlPort, cameraInfo.Capability, ControlPortCapability);
        }


        private bool ExtractObjectFromFrame(Bitmap image, VPort cameraPort, ref Rectangle rectObject)
        {
            bool foundObject = false;

            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            if (null == registeredCameras[cameraPort].ObjectDetector)
            {
                registeredCameras[cameraPort].ObjectDetector = new ObjectDetector();
            }

            unsafe
            {
                if (!registeredCameras[cameraPort].ObjectDetector.IsInitialized())
                {
                    registeredCameras[cameraPort].ObjectDetector.InitializeFromFrame((byte*)ptr, 3 * image.Width * image.Height, image.Width, image.Height, null);
                }
                else
                {
                    rectObject = registeredCameras[cameraPort].ObjectDetector.GetObjectRect((byte*)ptr, 3 * image.Width * image.Height);
                    if (rectObject.Width != 0 && rectObject.Height != 0)
                        foundObject = true;
                }

                if (foundObject)
                {
                    logger.Log("Object detected by camera {0} with co-ordinates X={1}, Y={2}, Width={3}, Height={4}",
                        cameraPort.GetInfo().GetFriendlyName(), rectObject.X.ToString(), rectObject.Y.ToString(), rectObject.Width.ToString(), rectObject.Height.ToString());
                }
            }

            image.UnlockBits(bmpData);

            return foundObject;

        }

        private void WriteObjectImage(VPort cameraPort, Bitmap image, Rectangle rectSrc, bool center)
        {
            Rectangle rectTarget = rectSrc;
            int srcPixelShiftX = 0;
            int srcPixelShiftY = 0;

            if (rectSrc.Width == 0 && rectSrc.Height == 0)
            {
                logger.Log("Write Object Image Called with Rect with zero height and width!");
                return;
            }

            if (center)
            {
                rectTarget.X = (int)((image.Width - rectSrc.Width) / 2.0);
                rectTarget.Y = (int)((image.Height - rectSrc.Height) / 2.0);
                srcPixelShiftX = rectTarget.X - rectSrc.X;
                srcPixelShiftY = rectTarget.Y - rectSrc.Y;
            }

            // create the destination based upon layer one
            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int stride = bmpData.Stride;
            image.UnlockBits(bmpData);

            WriteableBitmap composite = new WriteableBitmap(image.Width, image.Height, 96, 96, System.Windows.Media.PixelFormats.Rgb24, null);
            Int32Rect sourceRect = new Int32Rect(0, 0, (int)image.Width, (int)image.Height);
            byte[] pixels = new byte[stride * image.Height];

            for (int x = 0; x < image.Width; ++x)
            {
                for (int y = 0; y < image.Height; ++y)
                {
                    if (rectSrc.Contains(x, y))
                    {
                        Color clr = image.GetPixel(x, y);
                        pixels[stride * (y + srcPixelShiftY) + 3 * (x + srcPixelShiftX)] = clr.R;
                        pixels[stride * (y + srcPixelShiftY) + 3 * (x + srcPixelShiftX) + 1] = clr.G;
                        pixels[stride * (y + srcPixelShiftY) + 3 * (x + srcPixelShiftX) + 2] = clr.B;
                    }
                    else if (!rectTarget.Contains(x, y))
                    {
                        pixels[stride * y + 3 * x] = 0x00;
                        pixels[stride * y + 3 * x + 1] = 0x00;
                        pixels[stride * y + 3 * x + 2] = 0x00;
                    }
                }
            }
            composite.WritePixels(sourceRect, pixels, stride, 0);

            // encode the bitmap to the output file
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(composite));
            string filepath = GetMediaFileName(cameraPort.GetInfo().GetFriendlyName(), MediaType.MediaType_Image_JPEG);

            if (null == filepath)
            {
                logger.Log("GetMediaFileName failed to get a file name, are there more than 10 files of the same name?");
                return;
            }

            using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                encoder.Save(stream);
            }
        }

        private void AddFrameToVideo(Bitmap image, VPort cameraPort, long sampleTime)
        {
            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            int result;

            unsafe
            {
                result = registeredCameras[cameraPort].VideoWriter.AddFrame((byte*)ptr, 3 * image.Width * image.Height, image.Width, image.Height, sampleTime);
            }

            image.UnlockBits(bmpData);

            if (result != 0)
            {
                string message = String.Format("Failed to add frame for {0}. ResultCode: {1:x}", cameraPort.GetInfo().GetFriendlyName(), ((uint)result));
                logger.Log(message);

            }
        }

        private string GetMediaFileName(string cameraName, MediaType mediaType)
        {
            DateTime currTime = DateTime.Now;

            string directory = directory = String.Format("{0}\\{1}\\{2}-{3}-{4}", this.videosDir, cameraName, currTime.Year, currTime.Month, currTime.Day);

            //this method does nothing if the directory exists
            Directory.CreateDirectory(directory);

            string fileName = "";

            if (mediaType == MediaType.MediaType_Video_MP4)
            {
                fileName = String.Format("{0}\\{1}-{2}-{3}.mp4", directory, currTime.Hour, currTime.Minute, currTime.Second);
            }
            else if (mediaType == MediaType.MediaType_Image_JPEG)
            {
                fileName = String.Format("{0}\\{1}-{2}-{3}.jpg", directory, currTime.Hour, currTime.Minute, currTime.Second);
            }

            int count = 1;
            while (File.Exists(fileName) && count <= 10)
            {
                logger.Log("duplicate filename {0}", fileName);

                if (mediaType == MediaType.MediaType_Video_MP4)
                {
                    fileName = String.Format("{0}\\{1}-{2}-{3}_{4}.mp4", directory, currTime.Hour, currTime.Minute, currTime.Second, count);
                }
                else if (mediaType == MediaType.MediaType_Image_JPEG)
                {
                    fileName = String.Format("{0}\\{1}-{2}-{3}_{4}.jpg", directory, currTime.Hour, currTime.Minute, currTime.Second, count);
                }
                count++;
            }

            if (File.Exists(fileName))
            {
                logger.Log("could find a valid file name.");
                return null;
            }

            return fileName;

        }

        private void Log(string format, params string[] args)
        {
            logger.Log(format, args);
        }

        private string GetLocalHostIpAddress()
        {
            string ipAddress = null;
            IPAddress[] ips;

            ips = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress ip in ips)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddress = ip.ToString();
                    break;
                }
            }

            return ipAddress;
        }

        public bool IsMotionedTriggerEnabled(string cameraFriendlyName)
        {
            return this.IsMotionTriggerEnabled(cameraFriendlyName);
        }

    }
}