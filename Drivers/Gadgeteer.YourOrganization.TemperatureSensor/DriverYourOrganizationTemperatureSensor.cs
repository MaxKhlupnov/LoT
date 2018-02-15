using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;


namespace HomeOS.Hub.Drivers.Gadgeteer.YourOrganization.TemperatureSensor
{
    [DataContract]
    public class Response
    {
        [DataMember(Name = "DeviceId")]
        public string DeviceId { get; set; }
        [DataMember(Name = "Temperature")]
        public double temperature { get; set; }
    }

    [System.AddIn.AddIn("HomeOS.Hub.Drivers.Gadgeteer.YourOrganization.TemperatureSensor")]
    public class DriverYourOrganizationTemperatureSensor : DriverGadgeteerBase
    {
        int temp = 0;

        protected override void WorkerThread()
        {
            while (true)
            {
                try
                {
                    string url = string.Format("http://{0}/temp", deviceIp);

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

                    if (jsonResponse.temperature > 0)
                        logger.Log("Gadgeteer Temperature: {0}", jsonResponse.temperature.ToString());

                    temp = (int)jsonResponse.temperature;

                        IList<VParamType> retVals = new List<VParamType>();
                        retVals.Add(new ParamType(temp));

                        devicePort.Notify(RoleSensor.RoleName, RoleSensor.OpGetName, retVals);

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

        /// <summary>
        /// The demultiplexing routing for incoming
        /// </summary>
        /// <param name="message"></param>
        protected override List<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> parameters)
        {
            switch (roleName.ToLower())
            {
                case RoleSensor.RoleName:
                    {
                        switch (opName.ToLower())
                        {
                            case RoleSensor.OpGetName:
                                {
                                    List<VParamType> retVals = new List<VParamType>();
                                    retVals.Add(new ParamType(temp));

                                    return retVals;
                                }
                            default:
                                logger.Log("Unknown operation {0} for {1}", opName, roleName);
                                return null;
                        }
                    }
                case RoleActuator.RoleName:
                    {
                        switch (opName.ToLower())
                        {
                            case RoleActuator.OpPutName:
                                {
                                    try
                                    {
                                        string url = string.Format("http://{0}/led?low={1}&high={2}", deviceIp, (int)parameters[0].Value(), (int)parameters[1].Value());

                                        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                                        HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                                    }
                                    catch (Exception e)
                                    {
                                        logger.Log("{0}: couldn't talk to the device. are the arguments correct?\n exception details: {1}", this.ToString(), e.ToString());

                                        //lets try getting the IP again
                                        deviceIp = GetDeviceIp(deviceId);
                                    }
                                    return new List<VParamType>();
                                }
                            default:
                                logger.Log("Unknown operation {0} for {1}", opName, roleName);
                                return null;
                        }
                    }
                default:
                    logger.Log("Unknown role {0}", roleName);
                    return null;
            }
        }

        protected override List<VRole> GetRoleList()
        {
            return new List<VRole>() { RoleSensor.Instance, RoleActuator.Instance };
        }
    } 

}
