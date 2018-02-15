using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.AddIn;
using HomeOS.Hub.Common;

using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Drivers.Foscam
{
    [AddIn("HomeOS.Hub.Drivers.Foscam")]
    public class DriverFoscam : Common.ModuleBase
    {
        enum VideoFetchMode { SelfParse, MjpegDecoder, FromFile };

        VideoFetchMode videoFetchMode = VideoFetchMode.MjpegDecoder;

        string cameraId;
        string cameraUser;
        string cameraPwd;

        IPAddress cameraIp;

        NetworkCredential cameraCredential;

        Port cameraPort;

        byte[] latestImageBytes = new byte[0];

        //this object is used when we mjpeg decoding
        MjpegProcessor.MjpegDecoder _mjpeg;
        
        //if we read from file, which file to use
        string fileToRead;
        SafeThread worker1, worker2;

        //string baseUrl;
        private WebFileServer imageServer;

        public override void Start()
        {
            worker1 = null;
            worker2 = null;
            _mjpeg = null; 
            fileToRead = Constants.AddInRoot + "\\AddIns\\" + moduleInfo.BinaryName() + "\\logo-green.jpg";

            try
            {
                string[] words = moduleInfo.Args();

                cameraId = words[0];
                cameraUser = words[1];
                cameraPwd = words[2];
            }
            catch (Exception e)
            {
                logger.Log("{0}: Improper arguments: {1}. Exiting module", this.ToString(), e.ToString());
                return;
            }


            //get the IP address
            cameraIp = GetCameraIp(cameraId);

            if (cameraIp == null)
                return;

            //check the username and password
            cameraCredential = new NetworkCredential(cameraUser, cameraPwd);

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(CameraUrl);
                webRequest.Credentials = cameraCredential;
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                if (webResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    logger.Log("{0} credentials ({1}/{2}) are not correct", this.ToString(), cameraUser, cameraPwd);
                    return;
                }

                logger.Log("Started: {0} with response code {1}", ToString(), webResponse.StatusCode.ToString());
                webResponse.Close();
            }
            catch (Exception e)
            {
                logger.Log("{0}: couldn't talk to the camera. are the arguments correct?\n exception details: {1}", this.ToString(), e.ToString());

                //don't return. maybe the camera will come online later
                //return;
            }

            //add the camera service port
            VPortInfo pInfo = GetPortInfoFromPlatform("foscam-" + cameraId);

            //List<VRole> roles = new List<VRole>() {RoleCamera.Instance, RolePTCamera.Instance};
            List<VRole> roles = new List<VRole>() { RolePTCamera.Instance };

            cameraPort = InitPort(pInfo);
            BindRoles(cameraPort, roles, OnOperationInvoke);

            RegisterPortWithPlatform(cameraPort);


            switch (videoFetchMode)
            {
                case VideoFetchMode.SelfParse:
                    worker1 = new SafeThread(delegate()
                    {
                        GetVideoSelfParse();
                    }, "GetVideoSelfParse", logger); 
                    worker1.Start();
                    break;
                case VideoFetchMode.FromFile:
                    worker2 = new SafeThread(delegate()
                    {
                        GetVideoFromFile();
                    }, "GetVideoFromFile", logger);
                    worker2.Start();
                    break;
                case VideoFetchMode.MjpegDecoder:
                    GetVideoMjpegDecoder();
                    break;
                default:
                    logger.Log("Unknown video fetching mode");
                    break;
            }

            imageServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
        }

        public IPAddress GetCameraIp(string cameraId)
        {

            //if the Id is an IP Address itself, return that.
            //else get the Ip from platform

            IPAddress ipAddress = null;

            try
            {
                ipAddress = IPAddress.Parse(cameraId);
                return ipAddress;
            }
            catch (Exception)
            {
            }

            string ipAddrStr = GetDeviceIpAddress(cameraId);

            try
            {
                ipAddress = IPAddress.Parse(ipAddrStr);
                return ipAddress;
            }
            catch (Exception)
            {
                logger.Log("{0} couldn't get IP address from {1} or {2}", this.ToString(), cameraId, ipAddrStr);
            }

            return null;
        }

        public string CameraUrl
        {
            get { return "http://" + cameraIp + "/"; }
        }

        public override void Stop()
        {
            if (worker1!=null)
                worker1.Abort();
            if (worker2!=null)
                worker2.Abort();
                if (_mjpeg != null)
                    _mjpeg.StopStream();

                imageServer.Dispose();
        }

        public void Work()
        {
            //perhaps sit here and ping the camera?
        }

        /// <summary>
        /// The demultiplexing routing for incoming
        /// </summary>
        /// <param name="message"></param>
        private List<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> parameters)
        {
            string requestStr = null;
            List<VParamType> ret = new List<VParamType>();

            switch (opName.ToLower())
            {
                case RolePTCamera.OpDownName:
                    requestStr = String.Format("{0}decoder_control.cgi?command=2&onestep=1", CameraUrl);
                    break;
                case RolePTCamera.OpUpName:
                    requestStr = String.Format("{0}decoder_control.cgi?command=0&onestep=1", CameraUrl);
                    break;
                case RolePTCamera.OpLeftName:
                    requestStr = String.Format("{0}decoder_control.cgi?command=6&onestep=1", CameraUrl);
                    break;
                case RolePTCamera.OpRightName:
                    requestStr = String.Format("{0}decoder_control.cgi?command=4&onestep=1", CameraUrl);
                    break;
                case RoleCamera.OpGetImageName:
                    //requestStr = String.Format("{0}bitmap/image.bmp", CameraUrl);
                    requestStr = String.Format("{0}snapshot.cgi", CameraUrl);
                    break;
                default:
                    logger.Log("Unhandled camera operation {0}", opName);
                    break;
            }

            HttpWebResponse response = null;

            try
            {
                logger.Log("Sending to camera: {0}", requestStr);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestStr);
                request.Credentials = cameraCredential;
                response = (HttpWebResponse)request.GetResponse();

            }
            catch (ThreadAbortException e)
            {
                logger.Log("{0} Thread Abort Exception encountered {1} ", ToString(),e.Message);
            }
            catch (Exception exception)
            {

                logger.Log("{0} got exception: {1}", ToString(), exception.ToString());
            }

            if (response != null)
            {
                if (opName.Equals(RolePTCamera.OpGetImageName, StringComparison.CurrentCultureIgnoreCase))
                {
                    logger.Log("Code for image response: {0}", response.StatusCode.ToString());

                    //byte[] imageBytes = null;

                    //if (response.ContentType.Equals("image/bmp"))
                    if (response.ContentType.Equals("image/jpeg"))
                    {
                        System.IO.Stream responseStream = response.GetResponseStream();

                        lock (this)
                        {

                            if (latestImageBytes.Length < response.ContentLength)
                            {
                                latestImageBytes = new byte[response.ContentLength];
                            }

                            int readCumulative = 0, readThisRound = 0;
                            do
                            {
                                readThisRound = responseStream.Read(latestImageBytes, readCumulative, (int)response.ContentLength - readCumulative);

                                readCumulative += readThisRound;
                            }
                            while (readThisRound != 0);

                            if (readCumulative != response.ContentLength)
                                logger.Log("Could not read all the bytes from the camera. Read {0}/{1}", readCumulative.ToString(),
                                    response.ContentLength.ToString());
                        }
                    }

                    ret.Add(new ParamType(ParamType.SimpleType.jpegimage, latestImageBytes));
                }

                response.Close();
            }

            return ret;
        }

        private void NotifyListeners()
        {
            List<VParamType> ret = new List<VParamType>();
            ret.Add(new ParamType(ParamType.SimpleType.jpegimage, latestImageBytes));
            ret.Add(new ParamType(320)); 
            ret.Add(new ParamType(240)); 

            //image notifications are always for RoleCamera
            cameraPort.Notify(RoleCamera.RoleName, RoleCamera.OpGetVideo, ret);
        }

        private void GetVideoMjpegDecoder()
        {
            _mjpeg = new MjpegProcessor.MjpegDecoder();        
            _mjpeg.FrameReady += mjpeg_FrameReady;
            _mjpeg.Error += mjpeg_Error;

            string requestStr = String.Format("{0}videostream.cgi?resolution=32", CameraUrl);

            _mjpeg.ParseStream(new Uri(requestStr), cameraCredential.UserName, cameraCredential.Password);   
        }

        private void mjpeg_Error(object sender, MjpegProcessor.ErrorEventArgs e)
        {
            logger.Log("got error from mjpeg processor: {0}", e.Message);

            //sleep for three seconds and go again
            System.Threading.Thread.Sleep(3000);

            GetVideoMjpegDecoder();
        }


        private void mjpeg_FrameReady(object sender, MjpegProcessor.FrameReadyEventArgs e)
        {

            byte[] newFrame = _mjpeg.CurrentFrame;

            lock (this)
            {
                if (latestImageBytes.Length < newFrame.Length)
                {
                    //logger.Log("allocating {0} bytes for {1} (mjpeg)", newFrame.Length.ToString(), cameraId);
                    latestImageBytes = new byte[newFrame.Length];
                }

                Buffer.BlockCopy(newFrame, 0, latestImageBytes, 0, newFrame.Length);
            }

            NotifyListeners();
        }

        private void GetVideoFromFile()
        {

            while (true)
            {
                byte[] newImage = System.IO.File.ReadAllBytes(fileToRead);

                if (newImage.Length > latestImageBytes.Length)
                {
                    latestImageBytes = new byte[newImage.Length];
                }

                Buffer.BlockCopy(newImage, 0, latestImageBytes, 0, newImage.Length);

                NotifyListeners();

                System.Threading.Thread.Sleep(50);
            }
        }

        private void GetVideoSelfParse()
        {
             const int bufSize = 512 * 1024;	// buffer size
             const int readSize = 1024;		// portion size to read

             byte[] buffer = new byte[bufSize];	// buffer to read stream
            
            //double tempvideo_pos = 0;           // [chun-te]

            while (true)
            {
                HttpWebRequest req = null;
                WebResponse resp = null;
                System.IO.Stream stream = null;
                byte[] delimiter = null;
                byte[] delimiter2 = null;
                byte[] boundary = null;
                int boundaryLen, delimiterLen = 0, delimiter2Len = 0;
                int read, todo = 0, total = 0, pos = 0, align = 1;
                int start = 0, stop = 0;

                try
                {

                    string requestStr = String.Format("{0}videostream.cgi?resolution=32", CameraUrl);
                    req = (HttpWebRequest)WebRequest.Create(requestStr);
                    req.Credentials = cameraCredential;

                    resp = req.GetResponse();

                    // check content type
                    string ct = resp.ContentType;
                    if (ct.IndexOf("multipart/x-mixed-replace") == -1)
                        throw new ApplicationException("Invalid URL");

                    // get boundary
                    ASCIIEncoding encoding = new ASCIIEncoding();
                    boundary = encoding.GetBytes(ct.Substring(ct.IndexOf("boundary=", 0) + 9));
                    boundaryLen = boundary.Length;

                    // get response stream
                    stream = resp.GetResponseStream();


                    // loop
                    //while ((!stopEvent.WaitOne(0, true)) && (!reloadEvent.WaitOne(0, true)))
                    while (true)
                    {
                        // check total read
                        if (total > bufSize - readSize)
                        {
                            total = pos = todo = 0;
                        }

                        // read next portion from stream
                        if ((read = stream.Read(buffer, total, readSize)) == 0)
                            throw new ApplicationException();

                        total += read;
                        todo += read;

                        // increment received bytes counter
                        //bytesReceived += read;

                        // does we know the delimiter ?
                        if (delimiter == null)
                        {
                            // find boundary
                            pos = ByteArrayUtils.Find(buffer, boundary, pos, todo);

                            if (pos == -1)
                            {
                                // was not found
                                todo = boundaryLen - 1;
                                pos = total - todo;
                                continue;
                            }

                            todo = total - pos;

                            if (todo < 2)
                                continue;

                            // check new line delimiter type
                            if (buffer[pos + boundaryLen] == 10)
                            {
                                delimiterLen = 2;
                                delimiter = new byte[2] { 10, 10 };
                                delimiter2Len = 1;
                                delimiter2 = new byte[1] { 10 };
                            }
                            else
                            {
                                delimiterLen = 4;
                                delimiter = new byte[4] { 13, 10, 13, 10 };
                                delimiter2Len = 2;
                                delimiter2 = new byte[2] { 13, 10 };
                            }

                            pos += boundaryLen + delimiter2Len;
                            todo = total - pos;
                        }

                        // search for image
                        if (align == 1)
                        {
                            start = ByteArrayUtils.Find(buffer, delimiter, pos, todo);
                            if (start != -1)
                            {
                                // found delimiter
                                start += delimiterLen;
                                pos = start;
                                todo = total - pos;
                                align = 2;
                            }
                            else
                            {
                                // delimiter not found
                                todo = delimiterLen - 1;
                                pos = total - todo;
                            }
                        }

                        // search for image end
                        while ((align == 2) && (todo >= boundaryLen))
                        {
                            stop = ByteArrayUtils.Find(buffer, boundary, pos, todo);
                            if (stop != -1)
                            {
                                pos = stop;
                                todo = total - pos;

                                lock (this)
                                {
                                    if (latestImageBytes.Length < stop - start)
                                    {
                                        //logger.Log("allocating {0} bytes for {1} (self-parse)", (stop - start).ToString(), cameraId);
                                        latestImageBytes = new byte[stop - start];
                                    }

                                    Buffer.BlockCopy(buffer, start, latestImageBytes, 0, stop - start);
                                }
                                //tempvideo_pos = tempvideo_pos + 1;           // [chun-te]
                                //if ((tempvideo_pos%3 == 0) && (tempvideo_pos >= 100))                  // [chun-te]     (delay 3 times for Release mode)
                                {
                                    //    tempvideo_pos = 100;

                                    NotifyListeners();
                                }

                                // shift array
                                pos = stop + boundaryLen;
                                todo = total - pos;
                                Array.Copy(buffer, pos, buffer, 0, todo);

                                total = todo;
                                pos = 0;
                                align = 1;
                            }
                            else
                            {
                                // delimiter not found
                                todo = boundaryLen - 1;
                                pos = total - todo;
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    //System.Diagnostics.Debug.WriteLine("=============: " + ex.Message);
                    // wait for a while before the next try
                    logger.Log(ex.ToString());
                    Thread.Sleep(250);
                }
                catch (ApplicationException ex)
                {
                    //System.Diagnostics.Debug.WriteLine("=============: " + ex.Message);
                    // wait for a while before the next try
                    logger.Log(ex.ToString());
                    Thread.Sleep(250);
                }
                catch (Exception ex)
                {
                    //System.Diagnostics.Debug.WriteLine("=============: " + ex.Message);
                    logger.Log(ex.ToString());
                }
                finally
                {
                    // abort request
                    if (req != null)
                    {
                        req.Abort();
                        req = null;
                    }
                    // close response stream
                    if (stream != null)
                    {
                        stream.Close();
                        stream = null;
                    }
                    // close response
                    if (resp != null)
                    {
                        resp.Close();
                        resp = null;
                    }
                }

                //// need to stop ?
                //if (stopEvent.WaitOne(0, true))
                //    break;
            }
        }

  
        public override string GetDescription(string hint)
        {
            return "Foscam IP camera";
        }

        public override string ToString()
        {
            return "Foscam-" + cameraId;
        }

        // ... we don't care about other people's ports
        public override void PortRegistered(VPort port) { }
        public override void PortDeregistered(VPort port) { }

    }


    internal class ByteArrayUtils
    {
        // Check if the array contains needle on specified position
        public static bool Compare(byte[] array, byte[] needle, int startIndex)
        {
            int needleLen = needle.Length;
            // compare
            for (int i = 0, p = startIndex; i < needleLen; i++, p++)
            {
                if (array[p] != needle[i])
                {
                    return false;
                }
            }
            return true;
        }

        // Find subarray in array
        public static int Find(byte[] array, byte[] needle, int startIndex, int count)
        {
            int needleLen = needle.Length;
            int index;

            while (count >= needleLen)
            {
                index = Array.IndexOf(array, needle[0], startIndex, count - needleLen + 1);

                if (index == -1)
                    return -1;

                int i, p;
                // check for needle
                for (i = 0, p = index; i < needleLen; i++, p++)
                {
                    if (array[p] != needle[i])
                    {
                        break;
                    }
                }

                if (i == needleLen)
                {
                    // found needle
                    return index;
                }

                count -= (index - startIndex + 1);
                startIndex = index + 1;
            }
            return -1;
        }
    }
}
