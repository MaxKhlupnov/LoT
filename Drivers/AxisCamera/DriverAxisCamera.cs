using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.AddIn;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Drivers.AxisCamera
{
    [AddIn("HomeOS.Hub.Drivers.AxisCamera")]
    public class DriverAxisCamera : Common.ModuleBase
    {
        string cameraId;
        string cameraUser;
        string cameraPwd;

        IPAddress cameraIp;

        NetworkCredential cameraCredential;

        Port cameraPort;

        //string baseUrl;
        private WebFileServer imageServer;

        byte[] latestImageBytes = new byte[0];
        SafeThread worker = null;
        public override void Start()
        {
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
            VPortInfo pInfo = GetPortInfoFromPlatform("axiscamera-" + cameraId);

            List<VRole> roles = CameraRoles(cameraId);
                        
            cameraPort = InitPort(pInfo);
            BindRoles(cameraPort, roles, OnOperationInvoke);

            RegisterPortWithPlatform(cameraPort);

            worker = new SafeThread(delegate()
            {
                GetVideo();
            }, "", logger);
            worker.Start();

            imageServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
        }

        public List<VRole> CameraRoles(string cameraId)
        {
            //lets turn into lower case for easy comparisons
            cameraId = cameraId.ToLower();

            //the id does not have axis in it, we don't understand
            //lets pretent it is PTZ, so we show the controls
            if (!cameraId.Contains("axis"))
                //return new List<VRole>() { RoleCamera.Instance, RolePTCamera.Instance, RolePTZCamera.Instance };
                return new List<VRole>() { RolePTZCamera.Instance };

            //the list of PTZ cameras is at http://www.axis.com/products/video/camera/

            if (cameraId.Contains("axis m50") ||
                cameraId.Contains("axis p55") ||
                cameraId.Contains("axis q60") ||
                cameraId.Contains("axis q87") ||
                cameraId.Contains("axis 212") ||
                cameraId.Contains("axis 213") ||
                cameraId.Contains("axis 214"))
                //return new List<VRole>() { RoleCamera.Instance, RolePTCamera.Instance, RolePTZCamera.Instance };
                return new List<VRole>() { RolePTZCamera.Instance };

            return new List<VRole>() { RoleCamera.Instance };
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
        public void AddUser(string user, string password)
        {
            string requestStr = String.Format("{0}axis-cgi/admin/pwdgrp.cgi?action=add&user={1}&pwd={2}&grp=axuser&sgrp=axadmin:axoper:axview&comment=Joe", CameraUrl, user, password);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestStr);
            request.Credentials = cameraCredential;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            logger.Log("Add user. command {0} \n    response code {1}", requestStr, response.StatusCode.ToString());

            response.Close();
        }

        public string CameraUrl
        {
            get { return "http://" + cameraIp + "/"; }
        }

        public override void Stop()
        {

            if (worker != null)
                worker.Abort();

            if (imageServer != null)
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
                    requestStr = String.Format("{0}axis-cgi/com/ptz.cgi?move=down", CameraUrl);
                    break;
                case RolePTCamera.OpUpName:
                    requestStr = String.Format("{0}axis-cgi/com/ptz.cgi?move=up", CameraUrl);
                    break;
                case RolePTCamera.OpLeftName:
                    requestStr = String.Format("{0}axis-cgi/com/ptz.cgi?move=left", CameraUrl);
                    break;
                case RolePTCamera.OpRightName:
                    requestStr = String.Format("{0}axis-cgi/com/ptz.cgi?move=right", CameraUrl);
                    break;
                case RolePTZCamera.OpZoomInName:
                    requestStr = String.Format("{0}axis-cgi/com/ptz.cgi?rzoom=1000", CameraUrl);
                    break;
                case RolePTZCamera.OpZommOutName:
                    requestStr = String.Format("{0}axis-cgi/com/ptz.cgi?rzoom=-1000", CameraUrl);
                    break;
                case RoleCamera.OpGetImageName:
                    //requestStr = String.Format("{0}bitmap/image.bmp", CameraUrl);
                    requestStr = String.Format("{0}axis-cgi/jpg/image.cgi", CameraUrl);
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
            catch (Exception exception)
            {
                logger.Log("{0} got exception: {1}", ToString(), exception.ToString());
            }

            if (response != null)
            {
                if (opName.Equals(RolePTZCamera.OpGetImageName, StringComparison.CurrentCultureIgnoreCase))
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

        private const int	bufSize = 512 * 1024;	// buffer size
		private const int	readSize = 1024;		// portion size to read

        private void GetVideo()
        {
            byte[] buffer = new byte[bufSize];	// buffer to read stream

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

                    string requestStr = String.Format("{0}axis-cgi/mjpg/video.cgi?des_fps=10", CameraUrl);
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

                                // increment frames counter
                                //framesReceived ++;

                                //// image at stop
                                //if (NewFrame != null)
                                //{
                                //    Bitmap	bmp = (Bitmap) Bitmap.FromStream(new MemoryStream(buffer, start, stop - start));

                                //    // notify client
                                //    NewFrame(this, new CameraEventArgs(bmp));

                                //    // release the image
                                //    bmp.Dispose();
                                //    bmp = null;
                                //}

                                {
                                    //byte[] imageBytes = new byte[stop - start];

                                    lock (this)
                                    {
                                        if (latestImageBytes.Length < stop - start)
                                        {
                                            latestImageBytes = new byte[stop - start];
                                        }

                                        Buffer.BlockCopy(buffer, start, latestImageBytes, 0, stop - start);
                                    }

                                    List<VParamType> ret = new List<VParamType>();
                                    ret.Add(new ParamType(ParamType.SimpleType.jpegimage, latestImageBytes));

                                    //notifications go on rolecamera
                                    cameraPort.Notify(RoleCamera.RoleName, RoleCamera.OpGetVideo, ret);

                                    //logger.Log("image size: {0}", (stop - start).ToString());
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


        public override string ToString()
        {
            return "DriverAxisCamera-" + cameraId;
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
			int	needleLen = needle.Length;
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
			int	needleLen = needle.Length;
			int	index;

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
