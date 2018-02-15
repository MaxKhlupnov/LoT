using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class RemoteInfo
    {
        public string accountName { get; set; }
        public string accountKey { get; set; }

        public RemoteInfo(string accName, string accKey)
        {
            accountName = accName;
            accountKey = accKey;
        }
    }
}
