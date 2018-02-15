using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.AddIn;
using System.Drawing;
using System.IO;
//namespace JockerSoft.Media
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;

//The JockerSoft.Media library being used is documented here
//http://www.codeproject.com/Articles/13237/Extract-Frames-from-Video-Files

namespace HomeOS.Hub.Drivers.VideoLoading
{

    [AddIn("HomeOS.Hub.Drivers.VideoLoading")]
    public class DriverVideoLoading : Common.ModuleBase
    {
        Port cameraPort;

        byte[] latestImageBytes = new byte[0];

        string video_dir;
        //string video_filename;

        SafeThread worker = null; 
        public override void Start()
        { 
            string[] words = moduleInfo.Args();
            video_dir = words[0];
            //video_filename = words[1];

            //add the camera service port
            VPortInfo pInfo = GetPortInfoFromPlatform("video loading");

            List<VRole> roles = new List<VRole>() {RoleCamera.Instance};

            cameraPort = InitPort(pInfo);
            BindRoles(cameraPort, roles, OnOperationInvoke);

            RegisterPortWithPlatform(cameraPort);
            worker = new SafeThread(delegate()
            {
                GetVideo();
            }, "DriverVideoLoading-PollDevice", logger);
            worker.Start();
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

        public override void Stop()
        {
            if (worker != null)
                worker.Abort();
       //     throw new Exception("Stopping not implemented for " + this);
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
            throw new NotImplementedException();
        }

        private const int bufSize = 512 * 1024;	// buffer size
        private const int readSize = 1024;		// portion size to read

        private void GetVideo()
        {
            //byte[] buffer = new byte[bufSize];	// buffer to read stream
            string video_file = video_dir;
            double video_pos = 0;
            double tempvideo_pos = 0;
            double video_length = JockerSoft.Media.FrameGrabber.GetStreamLengthFromVideo(video_file);
            double video_framerate = JockerSoft.Media.FrameGrabber.GetFrameRateFromVideo(video_file);
            double delta_pos = 1 / (video_length * video_framerate);

            int delayN = 18;
            int start_delayN = 110;
            int basepos = (((int)start_delayN / delayN) + 1) * delayN;
            while (true)
            {
                tempvideo_pos = tempvideo_pos + 1;

                if (video_pos > 1)          // if the video is loaded to the end, just exit the while loop
                {
                    //video_pos = 0;          // restart from the beginning
                    //System.Console.WriteLine(" Video Relaoded............................");
                    break;
                }

                try
                {
                    // test
                    Bitmap testframe = JockerSoft.Media.FrameGrabber.GetFrameFromVideo(video_file, video_pos);
                    MemoryStream teststream = new MemoryStream();
                    testframe.Save(teststream, System.Drawing.Imaging.ImageFormat.Jpeg);            // Jpeg? or BMP or others?
                    byte[] testbytes = teststream.ToArray();
                    int hei = testframe.Height;
                    int wid = testframe.Width;


                    testframe.Dispose();        // is it necessary?
                    teststream.Dispose();        // is it necessary?
                    testframe = null;
                    teststream = null;
                    ////// test


                    if (tempvideo_pos % delayN == 0 && tempvideo_pos >= start_delayN)            // the delay needs to be adjusted based on the video
                    {
                        //if (tempvideo_pos >= 100)           // since the App may not be ready, we just wait after certain number of frames
                        {
                            //System.Console.WriteLine("video_pos: {0}", video_pos);      // [debug]
                            video_pos = video_pos + delta_pos;
                        }
                        List<VParamType> ret = new List<VParamType>();
                        ret.Add(new ParamType(ParamType.SimpleType.jpegimage, testbytes));                        
                        ret.Add(new ParamType(wid));
                        ret.Add(new ParamType(hei));
                       
                        cameraPort.Notify(RoleCamera.RoleName, RoleCamera.OpGetVideo, ret);

                        tempvideo_pos = basepos;
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
                }
            }
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
