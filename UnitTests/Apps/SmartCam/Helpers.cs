using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace HomeOS.Hub.UnitTests.Apps.SmartCam
{
    public class Helpers
    {
        public static string GetLocalHostIpAddress()
        {
            string ipAddress = null;
            IPAddress[] ips;

            ips = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress ip in ips)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddress = ip.ToString();
                    break;
                }
            }

            return ipAddress;
        }

        public static void FixFileTime(string filepath, DateTime dateTime)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(filepath);
            bool isReadOnly = (File.GetAttributes(filepath) & FileAttributes.ReadOnly) != 0;

            if (lastWriteTime.CompareTo(dateTime) != 0)
            {
                if (isReadOnly)
                {
                    File.SetAttributes(filepath, File.GetAttributes(filepath) & ~FileAttributes.ReadOnly);
                }

                File.SetLastWriteTime(filepath, dateTime);

                if (isReadOnly)
                {
                    File.SetAttributes(filepath, File.GetAttributes(filepath) | FileAttributes.ReadOnly);
                }
            }
        }

    }
}
