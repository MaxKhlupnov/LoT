using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class CallerInfo
    {
        public string workingDir { get; set; }
        public string friendlyName { get; set; }
        public string appName { get; set; }
        public int secret { get; set; }

        public CallerInfo(string WorkingDir, string FriendlyName, string AppName, int Secret)
        {
            workingDir = WorkingDir;
            friendlyName = FriendlyName;
            appName = AppName;
            secret = Secret;
        }
    }
}
