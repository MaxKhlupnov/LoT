using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Drawing;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;

//The WebCam library being used is documented here
//http://www.codeproject.com/KB/miscctrl/webcam_c_sharp.aspx

//the argument passed to this module should be a substring of the web camera name

namespace DriverDoorjamb
{
    [AddIn("HomeOS.Hub.Drivers.Doorjamb")]
    public class DriverDoorjamb : ModuleBase
    {


        Port doorjambPort;
        SafeThread worker = null;

        private int dataPort = -1;
        private string doorjambDeviceName;

        const int synapseControllerQueryPort = 8403;
        const int synapseControllerResponsePort = 8405;

        string eventTime = "";

        public override void Start()
        {

            // ..... initialize the list of roles we are going to export
            List<VRole> listRole = new List<VRole>() {RoleDoorjamb.Instance};

            //.................instantiate the port
            //grab the specific device name
            doorjambDeviceName = moduleInfo.Args()[0];
            VPortInfo portInfo = GetPortInfoFromPlatform(""+doorjambDeviceName);
            doorjambPort = InitPort(portInfo);

            //..... bind the port to roles and delegates
            BindRoles(doorjambPort, listRole, OnOperationInvoke);

            //.................register the port after the binding is complete
            RegisterPortWithPlatform(doorjambPort);

            GetDataPort();
            ListenForData();

            Thread.Sleep(Timeout.Infinite);

        }

        public override void Stop()
        {
            if (worker != null)
                worker.Abort();
        }

        /// <summary>
        /// The demultiplexing routing for incoming operations
        /// </summary>
        /// <param name="message"></param>
        public IList<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> args)
        {
            switch (opName.ToLower())
            {
                case RoleDummy.OpEchoName:
                    int payload = (int)args[0].Value();
                    //logger.Log("{0} Got EchoRequest {1}", this.ToString(), payload.ToString());

                    List<VParamType> retVals = new List<VParamType>();
                    retVals.Add(new ParamType(-1 * payload));

                    return retVals;

                default:
                    //logger.Log("Invalid operation: {0}", opName);
                    return null;
            }
        }

        private void GetDataPort()
        {

            //ask the synapse controller for the dataport to listen to
            using (var client = new UdpClient(synapseControllerResponsePort))
            {

                client.Client.ReceiveTimeout = 1000;

                Byte[] sendBytes = Encoding.ASCII.GetBytes(doorjambDeviceName);
                client.Send(sendBytes, sendBytes.Length, "localhost", synapseControllerQueryPort);
                try
                {
                    //IPEndPoint object will allow us to read datagrams sent from any source.
                    IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                    // Blocks until a message returns on this socket from a remote host.
                    Byte[] receiveBytes = client.Receive(ref RemoteIpEndPoint);
                    //retrun data in form of "device name::port number"
                    string returnData = Encoding.ASCII.GetString(receiveBytes);
                    string[] info = returnData.Split(':');
                    dataPort = int.Parse(info[1]);
                }
                catch
                {
                    ;
                }
            }
            if (dataPort == -1)
            {
                logger.Log("DriverDoorjamb: could not obtain data port number");
            }
        }
        
        private void ListenForData()
        {
            //listen to the dataport for driver data
            //logger.Log("Doorjamb Driver: {0}", "listening");
            using (var client = new UdpClient(dataPort))
            {
                try
                {
                    while (true)
                    {
                        //IPEndPoint object will allow us to read datagrams sent from any source.
                        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                        // Blocks until a message returns on this socket from a remote host.
                        Byte[] receiveBytes = client.Receive(ref RemoteIpEndPoint);
                        string returnData = Encoding.ASCII.GetString(receiveBytes);
                        //send that data to subscribed apps
                        ProcessData(returnData);
                        //Notify(doorjambPort, RoleDoorjamb.Instance, RoleDoorjamb.OpEchoSubName, new ParamType(8888888));//returnData));
                        /**try
                        {
                            Notify(doorjambPort, RoleDoorjamb.Instance, RoleDoorjamb.OpEchoSubName, new ParamType(ParamType.SimpleType.text, (object)returnData));//returnData));
                        }
                        catch(ArgumentException e)
                        {
                            logger.Log("Doorjamb Driver: {0}", e.ToString());
                        }**/
                    }
                }
                catch
                {
                    ;
                }
            }
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
                string dataTime = dataList[1] + " " + dataList[2];
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
                        string eventString = "Someone's under Doorjamb " + dataList[4] + " at " + eventTime;
                        eventTime = "";
                        try
                        {
                            Notify(doorjambPort, RoleDoorjamb.Instance, RoleDoorjamb.OpEchoSubName, new ParamType(ParamType.SimpleType.text, (object)eventString));//returnData));
                        }
                        catch (ArgumentException e)
                        {
                            logger.Log("Doorjamb Driver: {0}", e.ToString());
                        }
                    }
                }
            }
        }

        public override string GetDescription(string hint)
        {
            return "Doorjamb Device";
        }

        // ... we don't care about other people's ports
        public override void PortRegistered(VPort port) { }
        public override void PortDeregistered(VPort port) { }

    }
}
