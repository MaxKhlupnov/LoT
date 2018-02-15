using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeOS.Hub.Common;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Drawing;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Drivers.Gadgeteer.MicrosoftResearch.WindowCamera
{
    [DataContract]
    public class Response
    {
        [DataMember(Name = "DeviceId")]
        public string DeviceId { get; set; }
    }


    [System.AddIn.AddIn("HomeOS.Hub.Drivers.Gadgeteer.MicrosoftResearch.WindowCamera")]
    public class DriverGadgeteerMicrosoftResearchWindowCamera : DriverGadgeteerBase
    {
        protected override List<VRole> GetRoleList()
        {
            return new List<VRole>() { RoleCamera.Instance };
        }

        byte[] latestImageBytes = new byte[0];
        
        protected override void WorkerThread()
        {
            while (true)
            {
                try
                {
                    string url = string.Format("http://{0}/webcam", deviceIp);

                    HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);

                    HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(String.Format(
                        "Server error (HTTP {0}: {1}).",
                        response.StatusCode,
                        response.StatusDescription));

                    //logger.Log("GOT IMAGE");

                    if (response.ContentType.Equals("image/bmp"))
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

                            //Uncomment this if the camera is inverted
                            //latestImageBytes = RotateImage(latestImageBytes);

                        }
                    }

                    response.Close();


                    //notify the subscribers
                    List<VParamType> ret = new List<VParamType>();
                    ret.Add(new ParamType(ParamType.SimpleType.jpegimage, latestImageBytes));

                    devicePort.Notify(RoleCamera.Instance.Name(), RoleCamera.OpGetVideo, ret);
                }
                catch (Exception e)
                {
                    logger.Log("couldn't talk to the device {0} ip={1}.\nare the arguments correct?\n exception details: {2}", this.ToString(), deviceIp.ToString(), e.ToString());

                    //lets try getting the IP again
                    deviceIp = GetDeviceIp(deviceId);
                }

                System.Threading.Thread.Sleep(1000);
            }
        }

        private byte[] RotateImage(byte[] imageBytes)
        {

            Bitmap bitmap = new Bitmap(new System.IO.MemoryStream(imageBytes));
            bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);

            System.IO.MemoryStream memStream = new System.IO.MemoryStream();
            bitmap.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);

            return memStream.ToArray();
        }


        /// <summary>
        /// The demultiplexing routing for incoming
        /// </summary>
        /// <param name="message"></param>
        protected override List<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> parameters)
        {
            switch (opName.ToLower())
            {
                case RoleCamera.OpGetImageName:
                    {
                        List<VParamType> retVals = new List<VParamType>();

                        retVals.Add(new ParamType(ParamType.SimpleType.jpegimage, latestImageBytes));

                        return retVals;
                    }
                default:
                    logger.Log("Unknown operation {0} for role {1}", opName, roleName);
                    return null;
            }
        }

    }
}
