using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeOS.Hub.Common;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Drivers.Gadgeteer.MicrosoftResearch.MoistureSensor
{
    [DataContract]
    public class Response
    {
        [DataMember(Name = "DeviceId")]
        public string DeviceId { get; set; }
        [DataMember(Name = "moisture")]
        public int moisture { get; set; }
    }


    [System.AddIn.AddIn("HomeOS.Hub.Drivers.Gadgeteer.MicrosoftResearch.MoistureSensor")]
    public class DriverGadgeteerMicrosoftResearchMoistureSensor : DriverGadgeteerBase
    {

        const byte WetThreshold = 1; //values equal or more will be considered wet

        byte lastValue = 0;

        protected override List<VRole> GetRoleList()
        {
            return new List<VRole>() { RoleSensor.Instance };
        }

        protected override void WorkerThread()
        {
            while (true)
            {
                try
                {
                    string url = string.Format("http://{0}/moisture", deviceIp);

                    HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                    HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(String.Format(
                        "Server error (HTTP {0}: {1}).",
                        response.StatusCode,
                        response.StatusDescription));
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Response));
                    object objResponse = jsonSerializer.ReadObject(response.GetResponseStream());
                    Response jsonResponse = objResponse as Response;

                    response.Close();

                    if (jsonResponse.moisture > 0)
                        logger.Log("Gadgeteer Moisture: {0}", jsonResponse.moisture.ToString());

                    byte newValue = NormalizeMoistureValue(jsonResponse.moisture);

                    //notify the subscribers
                    if (newValue != lastValue)
                    {
                        IList<VParamType> retVals = new List<VParamType>();
                        retVals.Add(new ParamType(newValue));

                        devicePort.Notify(RoleSensor.RoleName, RoleSensor.OpGetName, retVals);
                    }

                    lastValue = newValue;

                }
                catch (Exception e)
                {
                    logger.Log("{0}: couldn't talk to the device. are the arguments correct?\n exception details: {1}", this.ToString(), e.ToString());

                    //lets try getting the IP again
                    deviceIp = GetDeviceIp(deviceId);
                }


                System.Threading.Thread.Sleep(4 * 1000);
            }
        }

        private byte NormalizeMoistureValue(int rawValue)
        {
            if (rawValue >= WetThreshold)
                return 255;
            else
                return 0;
        }


        /// <summary>
        /// The demultiplexing routing for incoming
        /// </summary>
        /// <param name="message"></param>
        protected override List<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> parameters)
        {
            switch (opName.ToLower())
            {
                case RoleSensor.OpGetName:
                    {
                        List<VParamType> retVals = new List<VParamType>();
                        retVals.Add(new ParamType(lastValue));

                        return retVals;
                    }
                default:
                    logger.Log("Unknown operation {0} for role {1}", opName, roleName);
                    return null;
            }
        }

    }
}
