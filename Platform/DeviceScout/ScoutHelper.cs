using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System.Timers;
using UPNPLib;

namespace HomeOS.Hub.Platform.DeviceScout
{
    public class ScoutHelper
    {
        public const int DefaultDeviceDiscoveryPeriodSec = 30;
        public const int DefaultNumPeriodsToForgetDevice = 5;

        static Timer upnpScanTimer;

        //variables to ensure that we only have one instance of ScoutHelper running
        private static object instance = null;

        private static object lockObject = new object();

        private static List<UPnPDevice> upnpDevices = new List<UPnPDevice>();

        private static VLogger logger;

        public static void Init(VLogger loggerObject)
        {
            lock (lockObject)
            {
                if (instance != null)
                    loggerObject.Log("Duplicate Init called on ScoutHelper");

                instance = new object();
            }

            logger = loggerObject;

            //create a time that fires ScanNow() periodically
            upnpScanTimer = new Timer(ScoutHelper.DefaultDeviceDiscoveryPeriodSec * 1000);
            //upnpScanTimer = new Timer(1 * 1000);  // for debugging
            upnpScanTimer.Enabled = true;
            upnpScanTimer.Elapsed += new ElapsedEventHandler(UpnpScan);
        }

        private static void UpnpScan(object source, ElapsedEventArgs e)
        {
            //lets stop the timer while we do our work
            upnpScanTimer.Enabled = false;

            var finder = new UPnPDeviceFinder();
            var devices = finder.FindByType("upnp:rootdevice", 0);

            lock (lockObject)
            {
                upnpDevices.Clear();

                foreach (UPnPDevice device in devices)
                {
                    upnpDevices.Add(device);

                    //logger.Log("UPnPDevice: model={0} present={1} type={2} sn={3} upc={4} udn={5}", device.ModelURL, device.PresentationURL, device.Type, device.SerialNumber, device.UPC, device.UniqueDeviceName);
                }
            }

           //lets re-start the timer now
           upnpScanTimer.Enabled = true;
        }

        /// <summary>
        /// Returns the upnp devices that were discovered in the last scan
        /// We return as UPnpDevice[] because .net does not let us pass List<UPnPDevice>
        /// </summary>
        public static UPnPDevice[] UpnpGetDevices()
        {
           UPnPDevice[] retList = null;

            lock (lockObject)
            {
                retList = new UPnPDevice[upnpDevices.Count];

                for(int index = 0; index < upnpDevices.Count; index++)
                {
                    retList[index] = upnpDevices[index];
                }
            }

            return retList;
        }

        public static bool BroadcastRequest(byte[] request, int portNumber, VLogger logger)
        {
            try
            {
                foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (netInterface.OperationalStatus != OperationalStatus.Up)
                        continue;

                    foreach (var netAddress in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        //only send to IPv4 and non-loopback addresses
                        if (netAddress.Address.AddressFamily != AddressFamily.InterNetwork ||
                            IPAddress.IsLoopback(netAddress.Address))
                            continue;

                        IPEndPoint localEp = new IPEndPoint(netAddress.Address, portNumber);
                        using (var client = new UdpClient(localEp))
                        {
                            //logger.Log("Sending bcast packet from {0}", localEp.ToString());
                            client.Client.EnableBroadcast = true;
                            var endPoint = new IPEndPoint(IPAddress.Broadcast, portNumber);
                            client.Connect(endPoint);
                            client.Send(request, request.Length);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log("Exception while sending UDP request. \n {0}", e.ToString());
                return false;
            }

            return true;
        }

        [System.Runtime.InteropServices.DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref int PhyAddrLen);

        public static string GetMacAddressByIP(System.Net.IPAddress ipAddress)
        {
            byte[] macBytes = new byte[6];
            int length = 6;
            SendARP(BitConverter.ToInt32(ipAddress.GetAddressBytes(), 0), 0, macBytes, ref length);
            return BitConverter.ToString(macBytes, 0, 6);
        }

        public static NetworkInterface GetInterface(IPPacketInformation packetInfo)
        {

            int interfaceId = packetInfo.Interface;

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var netInterface in interfaces)
            {
                var ipv4InterfaceProps = netInterface.GetIPProperties().GetIPv4Properties();

                if (ipv4InterfaceProps != null &&
                    ipv4InterfaceProps.Index == interfaceId)
                    return netInterface;
            }

            return null;
        }

        public static bool IsMyAddress(IPAddress iPAddress)
        {
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var netAddress in netInterface.GetIPProperties().UnicastAddresses)
                {
                    if (netAddress.Address.Equals(iPAddress))
                        return true;
                }
            }

            return false;
        }
    }
}
