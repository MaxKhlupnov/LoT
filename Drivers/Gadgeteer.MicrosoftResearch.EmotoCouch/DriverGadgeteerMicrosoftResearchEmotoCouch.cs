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

//using System.Threading;


namespace HomeOS.Hub.Drivers.Gadgeteer.MicrosoftResearch.EmotoCouch
{
    [DataContract]
    public class Response
    {
        [DataMember(Name = "DeviceId")]
        public string DeviceId { get; set; }
        //[DataMember(Name = "moisture")]  //AJB - this looks like copy paste bug
        //public int moisture { get; set; }
    }


    [System.AddIn.AddIn("HomeOS.Hub.Drivers.Gadgeteer.MicrosoftResearch.EmotoCouch")]
    public class DriverGadgeteerMicrosoftResearchEmotoCouch : DriverGadgeteerBase
    {
        protected override void WorkerThread()
        {

            logger.Log("Started: {0}", this.ToString());

            
            try
            {
                string[] words = moduleInfo.Args();

                deviceId = words[0];
            }
            catch (Exception e)
            {
                logger.Log("{0}: Improper arguments: {1}. Exiting module", this.ToString(), e.ToString());
                return;
            }

            //get the IP address
            deviceIp = GetDeviceIp(deviceId);

            if (deviceIp == null)
            {
                logger.Log("{0} did not get a device ip for deviceId: {1}. Returning", base.moduleInfo.BinaryName(), deviceId.ToString());
                return;
            }

            //add the service port
            ///AJB - how do we know it's a couch?
            VPortInfo pInfo = GetPortInfoFromPlatform("gadgeteer-" + deviceId);

            RoleCouch roleCouch = RoleCouch.Instance;

            List<VRole> roles = new List<VRole>();
            roles.Add(roleCouch);

            devicePort = InitPort(pInfo);
            BindRoles(devicePort, roles, OnOperationInvoke);

            RegisterPortWithPlatform(devicePort);
            worker = new SafeThread(delegate()
            {
                PollDevice();
            }, "DriverGadgeteerMSREmotoCouch-PollDevice", logger);
            worker.Start();
        }

        private void PollDevice()
        {
            while (true)
            {
                try
                {
                    //AJB - TODO: what is the right format for gadget couch or other role?
                    //string url = string.Format("http://{0}/couch?red=255", deviceIp);

                    //HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);

                    //HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                    //if (response.StatusCode != HttpStatusCode.OK)
                    //    throw new Exception(String.Format(
                    //    "Server error (HTTP {0}: {1}).",
                    //    response.StatusCode,
                    //    response.StatusDescription));

                    ////logger.Log("GOT IMAGE");

                    //text/html
                    //if (response.ContentType.Equals("text/html"))
                    //{
                    //    System.IO.Stream responseStream = response.GetResponseStream();

                    //    lock (this)
                    //    {

                    //        if (latestImageBytes.Length < response.ContentLength)
                    //        {
                    //            latestImageBytes = new byte[response.ContentLength];
                    //        }

                    //        int readCumulative = 0, readThisRound = 0;
                    //        do
                    //        {
                    //            readThisRound = responseStream.Read(latestImageBytes, readCumulative, (int)response.ContentLength - readCumulative);

                    //            readCumulative += readThisRound;
                    //        }
                    //        while (readThisRound != 0);

                    //        if (readCumulative != response.ContentLength)
                    //            logger.Log("Could not read all the bytes from the camera. Read {0}/{1}", readCumulative.ToString(),
                    //                response.ContentLength.ToString());

                    //        //Comment this out once gadgeteer cameras are fixed
                    //        latestImageBytes = RotateImage(latestImageBytes);

                    //    }
                    //}

                    //response.Close();


                    ////notify the subscribers
                    //List<VParamType> ret = new List<VParamType>();
                    //ret.Add(new ParamType(ParamType.SimpleType.image, System.Net.Mime.MediaTypeNames.Image.Jpeg, latestImageBytes, "image"));

                    //devicePort.Notify(RoleCamera.RoleName, RoleCamera.OpGetVideo, ret);
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

        /// <summary>
        /// The demultiplexing routing for incoming
        /// </summary>
        /// <param name="message"></param>
        protected override List<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> args)
        {
            switch (opName.ToLower())
            {
                // AJB leaving as example of what to do when couch operations are decided
                case RoleCouch.OpGoHappyName:
                    {
                        int payload = (int)args[0].Value();
                        logger.Log("{0} Got emotion request {1}", this.ToString(), payload.ToString());

                        //....

                        var retVals = new List<VParamType>();
                        return retVals;
                    }
                default:
                    logger.Log("Unknown operation {0} for role {1}", opName, roleName);
                    return null;
            }
        }

        protected override List<VRole> GetRoleList()
        {
            return new List<VRole>() { RoleCouch.Instance };
        }
    }
}
