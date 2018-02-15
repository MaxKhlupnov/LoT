// The purpose of this class is to discover the presence of a webcam

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using HomeOS.Hub.Common;
using HomeOS.Hub.Common.WebCam.WebCamWrapper.Camera;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Platform.DeviceDiscover
{
    class WebcamScout : DeviceScout
    {
        const int m_portNumber = 10000;
        const int m_offset_MAC = 0x17;
        const int m_length_MAC = 12;
        const int m_offset_Camera_IP = 0x39;
        const int m_offset_router_IP = 0x41;
        const int m_length_IP = 4;

        public WebcamScout(VLogger logger) : base(logger) {}

        private static Device GetDevice(Camera camera)
        {
            string cameraName = camera.ToString();

            var device = new Device("Webcam - " + cameraName, cameraName, cameraName, DateTime.Now, "DriverWebCam", false);
            return device;
        }

        public override List<Device> GetDevices()
        {

            var ans = new List<Device>();

            try
            {
                foreach (Camera camera in CameraService.AvailableCameras)
                {
                    ans.Add(GetDevice(camera));
                }
            }
            catch
            {
                ;
            }
            return ans;
        }
    }
}