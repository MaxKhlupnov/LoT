using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace HomeOS.Hub.Watchdog
{
    class Utils
    {
        /// <summary>
        /// cached hardware id because the actual call has some overhead
        /// </summary>
        private static string hwId = null;

        //NB: this hardware id computation code is copied from Common\Utils.cs. If you want to change it, change it there too.

        public static string HardwareId
        {
            get
            {
                if (hwId == null)
                    hwId = String.Format("cpu:{0} hdd:{1}", FirstCpuId(), CVolumeSerial());

                return hwId;
            }
        }

        /// <summary>
        /// Returns the HDD volume of c:
        /// </summary>
        /// <returns></returns>
        private static string CVolumeSerial()
        {
            var disk = new ManagementObject(@"win32_logicaldisk.deviceid=""c:""");
            disk.Get();

            string volumeSerial = disk["VolumeSerialNumber"].ToString();
            disk.Dispose();

            return volumeSerial;
        }

        /// <summary>
        /// Returns the id of the first CPU listed by WMI
        /// </summary>
        /// <returns></returns>
        private static string FirstCpuId()
        {
            var mClass = new ManagementClass("win32_processor");

            foreach (var obj in mClass.GetInstances())
            {
                return obj.Properties["processorID"].Value.ToString();
            }
            return "";
        }
    }
}
