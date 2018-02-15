using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Platform.DeviceScout
{
    public class DeviceList
    {
        List<Device> currentDeviceList = new List<Device>();

        public DeviceList()
        {
            //nothing to do here
        }

        public void InsertDevice(Device device)
        {
            lock (currentDeviceList)
            {
                for (int index = 0; index < currentDeviceList.Count; index++)
                {
                    if (currentDeviceList[index].UniqueName.Equals(device.UniqueName))
                    {
                        //overwrite the old information with the new discovery
                        currentDeviceList[index] = device;

                        return;
                    }
                }

                //coming here implies that the device was not found in the current list; add it
                currentDeviceList.Add(device);
            }
        }

        //removes devices that have not been seen for timeoutSecs
        public void RemoveOldDevices(int timeoutSecs)
        {
            lock (currentDeviceList)
            {
                for (int index = 0; index < currentDeviceList.Count; index++)
                {
                    Device device = currentDeviceList[index];

                    if ((DateTime.Now - device.LastSeen).TotalSeconds > timeoutSecs)
                    {
                        currentDeviceList.RemoveAt(index);
                    }

                }
            }
        }

        public Device GetDevice(string deviceId)
        {
            lock (currentDeviceList)
            {
                foreach (Device device in currentDeviceList)
                {
                    if (device.UniqueName.Equals(deviceId))
                        return device;
                }
            }

            return null;
        }

        //makes a shallow copy of the device list
        public List<Device> GetClonedList()
        {
            List<Device> retList = null;

            lock (currentDeviceList)
            {
                retList = new List<Device>(currentDeviceList);
            }

            return retList;
        }



    }
}
