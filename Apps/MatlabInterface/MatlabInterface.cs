using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common.Bolt.DataStore;
using System.Diagnostics;

namespace HomeOS.Hub.Apps.MatlabInterface
{

    /// <summary>
    /// A dummy a module that 
    /// 1. sends ping messages to all active dummy ports
    /// </summary>

    [System.AddIn.AddIn("HomeOS.Hub.Apps.MatlabInterface")]
    public class MatlabInterface :  ModuleBase
    {
        //list of accessible dummy ports in the system
        List<VPort> accessibleDummyPorts;

        private SafeServiceHost serviceHost;

        private WebFileServer appServer;

        List<string> receivedMessageList;
        List<string> receivedMessageListMatlab;
        SafeThread worker = null;

        IStream datastream;

        public override void Start()
        {
            logger.Log("Started: {0} ", ToString());

            DummyService dummyService = new DummyService(logger, this);
            serviceHost = new SafeServiceHost(logger,typeof(IMatlabInterfaceContract), dummyService , this, Constants.AjaxSuffix, moduleInfo.BaseURL());
            serviceHost.Open();

            appServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
            
 
            //........... instantiate the list of other ports that we are interested in
            accessibleDummyPorts = new List<VPort>();

            //..... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();

            if (allPortsList != null)
                ProcessAllPortsList(allPortsList);

            this.receivedMessageList = new List<string>();
            this.receivedMessageListMatlab = new List<string>();
            

            // remoteSync flag can be set to true, if the Platform Settings has the Cloud storage
            // information i.e., DataStoreAccountName, DataStoreAccountKey values
            datastream = base.CreateValueDataStream<StrKey, StrValue>("test", false /* remoteSync */);

            worker = new SafeThread(delegate()
            {
                Work();
            }, "AppDummy-worker", logger);
            worker.Start();
        }

        public override void Stop()
        {
            logger.Log("AppDummy clean up");
            if (worker != null)
                worker.Abort();
            
            if (datastream != null)
                datastream.Close();

            if (serviceHost != null)
                serviceHost.Close();

            if (appServer != null)
                appServer.Dispose();
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
                    foreach (VPort port in accessibleDummyPorts)
                    {
                        SendEchoRequest(port, counter);                       
                    }
                }

                WriteToStream();
                System.Threading.Thread.Sleep(1 * 10 * 1000);
            }
        }

        public void WriteToStream()
        {
            StrKey key = new StrKey("DummyKey");
            datastream.Append(key, new StrValue("DummyVal"));
            logger.Log("Writing {0} to stream " , datastream.Get(key).ToString());
        }

        public void SendEchoRequest(VPort port, int counter)
        {
            try
            {
                DateTime requestTime = DateTime.Now;

                var retVals = Invoke(port, RoleDummy.Instance, RoleDummy.OpEchoName, new ParamType(counter));

                double diffMs = (DateTime.Now - requestTime).TotalMilliseconds;

                if (retVals[0].Maintype() != (int)ParamType.SimpleType.error)
                {

                    int rcvdNum = (int) retVals[0].Value();

                    logger.Log("echo success to {0} after {1} ms. sent = {2} rcvd = {3}", port.ToString(), diffMs.ToString(), counter.ToString(), rcvdNum.ToString());
                }
                else
                {
                    logger.Log("echo failure to {0} after {1} ms. sent = {2} error = {3}", port.ToString(), diffMs.ToString(), counter.ToString(), retVals[0].Value().ToString());
                }

            }
            catch (Exception e)
            {
                logger.Log("Error while calling echo request: {0}", e.ToString());
            }
        }

        public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        {
            string message;
            lock (this)
            {
                switch (opName.ToLower())
                {
                    case RoleDummy.OpEchoSubName:
                        int rcvdNum = (int)retVals[0].Value();

                        message = String.Format("async echo response from {0}. rcvd = {1}", senderPort.ToString(), rcvdNum.ToString());
                        this.receivedMessageList.Add(message);
                        break;
                    default:
                        message = String.Format("Invalid async operation return {0} from {1}", opName.ToLower(), senderPort.ToString());
                        break;
                }
            }
            logger.Log("{0} {1}", this.ToString(), message);

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
                if (!accessibleDummyPorts.Contains(port) && 
                    Role.ContainsRole(port, RoleDummy.RoleName) && 
                    GetCapabilityFromPlatform(port) != null)
                {
                    accessibleDummyPorts.Add(port);

                    logger.Log("{0} added port {1}", this.ToString(), port.ToString());

                    if (Subscribe(port, RoleDummy.Instance, RoleDummy.OpEchoSubName))
                        logger.Log("{0} subscribed to port {1}", this.ToString(), port.ToString());
                    else
                        logger.Log("failed to subscribe to port {1}", this.ToString(), port.ToString());
                }
            }
        }

        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (accessibleDummyPorts.Contains(port))
                {
                    accessibleDummyPorts.Remove(port);
                    logger.Log("{0} deregistered port {1}", this.ToString(), port.GetInfo().ModuleFacingName());
                }
            }
        }

        public List<string> GetReceivedMessages()
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.Arguments = "Hello World";
            start.FileName = "";//"<Path to folder containting interfacingMatlab.exe>\\interfacingMatlab.exe";
            start.WindowStyle = ProcessWindowStyle.Hidden;
            start.CreateNoWindow = true;

            try
            {
                using (Process proc = Process.Start(start))
                {
                    proc.WaitForExit();
                    //string concatenatedMatlabString = System.IO.File.ReadAllText(@"C:\Users\Public\TestFolder\WriteText.txt");
                    string concatenatedMatlabString = System.IO.File.ReadAllText("interfacingMatlabOutput.txt");
                    this.receivedMessageListMatlab.Add(concatenatedMatlabString);
                }
            }
            catch (Exception e)
            {
                logger.Log("Error while interfacing Matlab: {0}", e.ToString());
            }
            List<string> retList = new List<string>(this.receivedMessageListMatlab);
            retList.Reverse();
            return retList;
        }
    }
}
