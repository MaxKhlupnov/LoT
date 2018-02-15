using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common.Bolt.DataStore;

namespace HomeOS.Hub.Apps.Doorjamb
{

    /// <summary>
    /// A dummy a module that 
    /// 1. sends ping messages to all active dummy ports
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Apps.Doorjamb")]
    public class Doorjamb : ModuleBase
    {
        //list of accessible doorjamb ports in the system
        List<VPort> accessibleDoorjambPorts;

        private SafeServiceHost simplexServiceHost;

        private WebFileServer appServer;

        List<string> receivedMessageList;
        SafeThread worker = null;

        List<string> irDataList;
        List<string> usDataList;

        string eventTime;

        public override void Start()
        {
            logger.Log("Started: {0} ", ToString());

            DoorjambService simplexService = new DoorjambService(logger, this);

            simplexServiceHost = DoorjambService.CreateServiceHost(logger, this, simplexService, moduleInfo.BaseURL() + "/webapp");

            simplexServiceHost.Open();

            appServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);


            //........... instantiate the list of other ports that we are interested in
            accessibleDoorjambPorts = new List<VPort>();

            //..... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();

            if (allPortsList != null)
                ProcessAllPortsList(allPortsList);

            this.receivedMessageList = new List<string>();
            this.irDataList = new List<string>();
            this.irDataList.Add("");
            this.usDataList = new List<string>();
            this.usDataList.Add("");
            this.eventTime = "";

            worker = new SafeThread(delegate()
            {
                Work();
            }, "AppDoorjamb-worker", logger);
            worker.Start();
        }

        public override void Stop()
        {
            logger.Log("AppDoorjamb clean up");
            if (worker != null)
                worker.Abort();
            //datastream.Close();
        }

        /// <summary>
        /// Sit in a loop and spray the Pings to all active ports
        /// </summary>
        public void Work()
        {
            int counter = 0;
            while (true)
            {
                counter++;

                lock (this)
                {
                    foreach (VPort port in accessibleDoorjambPorts)
                    {
                        //this is just an example of sending requests to driver, doorjamb does not actually need this
                        SendEchoRequest(port, counter);
                    }
                }

                System.Threading.Thread.Sleep(1 * 10 * 1000);
            }
        }


        public void SendEchoRequest(VPort port, int counter)
        {
            try
            {
                DateTime requestTime = DateTime.Now;

                var retVals = Invoke(port, RoleDoorjamb.Instance, RoleDoorjamb.OpEchoName, new ParamType(counter));

                double diffMs = (DateTime.Now - requestTime).TotalMilliseconds;

                if (retVals[0].Maintype() != (int)ParamType.SimpleType.error)
                {

                    int rcvdNum = (int)retVals[0].Value();

                    //logger.Log("echo success to {0} after {1} ms. sent = {2} rcvd = {3}", port.ToString(), diffMs.ToString(), counter.ToString(), rcvdNum.ToString());
                }
                else
                {
                    //logger.Log("echo failure to {0} after {1} ms. sent = {2} error = {3}", port.ToString(), diffMs.ToString(), counter.ToString(), retVals[0].Value().ToString());
                }

            }
            catch (Exception /*e*/)
            {
                //logger.Log("Error while calling echo request: {0}", e.ToString());
            }
        }

        public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        {
            string message;
            lock (this)
            {
                switch (opName.ToLower())
                {
                    case "echosub":
                        string rcvdData = (string)retVals[0].Value();
                        irDataList.Add(rcvdData);
                        //ProcessData(rcvdData);
                        message = String.Format("async echo response from {0}. rcvd = {1}", senderPort.ToString(), rcvdData.ToString());
                        this.receivedMessageList.Add(message);
                        break;
                    default:
                        message = String.Format("Invalid async operation return {0} from {1}", opName.ToLower(), senderPort.ToString());
                        break;
                }
            }
            //logger.Log("{0} {1}", this.ToString(), message);

        }
    
        private void ProcessData(string newData)
        {
            int irData1 = 5;
            int irData2 = 6;
            int irData3 = 7;
            double irThreshold = 30;
            string[] dataList = newData.Split(' ');
            if (dataList[0].Equals("IR"))
            {
                string dataTime = dataList[1]+" "+dataList[2];
                if (double.Parse(dataList[irData1]) < irThreshold | double.Parse(dataList[irData2]) < irThreshold | double.Parse(dataList[irData3]) < irThreshold)
                {
                    if (eventTime.Equals(""))
                    {
                        eventTime = dataTime;
                    }
                }
                else
                {
                    if (!eventTime.Equals(""))
                    {
                        irDataList.Add("Detected something from Doorjamb "+dataList[4]+" at "+eventTime);
                        eventTime = "";
                        lock(irDataList){
                            foreach (string item in irDataList)
                            {
                                //logger.Log("{0} event list {1}", ToString(), item.ToString());
                            }
                        }
                    }
                }
            }
        }

        private void ProcessAllPortsList(IList<VPort> portList)
        {
            foreach (VPort port in portList)
            {
                PortRegistered(port);
            }
        }

        /// <summary>
        ///  Called when a new port is registered with the platform
        /// </summary>
        public override void PortRegistered(VPort port)
        {

            logger.Log("{0} got registeration notification for {1}", ToString(), port.ToString());

            lock (this)
            {
                if (!accessibleDoorjambPorts.Contains(port) &&
                    Role.ContainsRole(port, RoleDoorjamb.RoleName) &&
                    GetCapabilityFromPlatform(port) != null)
                {
                    accessibleDoorjambPorts.Add(port);

                    //logger.Log("{0} added port {1}", this.ToString(), port.ToString());

                    if (Subscribe(port, RoleDoorjamb.Instance, RoleDoorjamb.OpEchoSubName))
                    {
                        //logger.Log("{0} subscribed to port {1}", this.ToString(), port.ToString());
                    }
                    else
                    {
                        //logger.Log("failed to subscribe to port {1}", this.ToString(), port.ToString());
                    }
                }
            }
        }

        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (accessibleDoorjambPorts.Contains(port))
                {
                    accessibleDoorjambPorts.Remove(port);
                    //logger.Log("{0} deregistered port {1}", this.ToString(), port.GetInfo().ModuleFacingName());
                }
            }
        }

        public List<string> GetReceivedMessages()
        {
            List<string> retList = new List<string>(this.receivedMessageList);
            retList.Reverse();
            return retList;
        }

        public List<string> GetAlerts()
        {
            List<string> retList = new List<string>();
            retList.Add("");
            retList.AddRange(irDataList);
            irDataList.Clear();
            irDataList.Add("");
            return retList;
        }
    }
}