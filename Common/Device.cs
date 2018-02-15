using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net.NetworkInformation;

namespace HomeOS.Hub.Common
{
    public class DeviceDetails
    {
        //if you add more elements to this class, make sure that the elements are written and read by Config

        public bool Configured { get; set; }
        public string DriverFriendlyName { get; set; }
        public List<string> DriverParams { get; set; }
    }

    public class Device
    {
        //these information bits are provided by the discovery driver
        string friendlyName;
        string uniqueName;

        NetworkInterface localInterface;
        string deviceIpAddress;

        DateTime lastSeen;

        //string m_scoutedBy;
        string driverBinaryName;

        //does the device need credentials
        bool needsCredentials;

        //these bits are carried over across invokations
        public DeviceDetails Details {get; set;}

        public Device() { }

        public Device(string friendlyName, string uniqueName, string deviceIpAddress, DateTime dateTime, string driverBinaryName, bool needCredentials = true) : 
            this(friendlyName, uniqueName, null, deviceIpAddress, dateTime, driverBinaryName, needCredentials)
               
        { }

        public Device(string friendlyName, string uniqueName, NetworkInterface localInterface, string deviceIpAddress, DateTime dateTime, string driverBinaryName, bool needCredentials=true)
        {
            this.friendlyName = friendlyName;
            this.uniqueName = uniqueName;
            this.localInterface = localInterface;
            this.deviceIpAddress = deviceIpAddress;
            this.lastSeen = dateTime;

            this.driverBinaryName = driverBinaryName;

            this.needsCredentials = needCredentials;

            Details = new DeviceDetails();
            Details.Configured = false;
            Details.DriverFriendlyName = "";
            Details.DriverParams = new List<string>();
        }

        public bool NeedsCredentials
        {
            get { return needsCredentials; }
            set { needsCredentials = value; }
        }

        public string DriverBinaryName
        {
            get { return driverBinaryName; }
            set { driverBinaryName = value; }
        }

        public string FriendlyName
        {
            get { return friendlyName; }
            set { friendlyName = value; }
        }
        public string UniqueName
        {
            get { return uniqueName; }
            set { uniqueName = value; }
        }
        public NetworkInterface LocalInterface
        {
            get { return localInterface; }
            set { localInterface = value; }
        }
        public string DeviceIpAddress
        {
            get { return deviceIpAddress; }
            set { deviceIpAddress = value; }
        }
        public DateTime LastSeen
        {
            get { return lastSeen; }
            set { lastSeen = value; }
        }

        public override string ToString()
        {
            return FriendlyName + " | " + UniqueName + " | " + deviceIpAddress + " | " + LastSeen.ToString();
        }

    }
}
