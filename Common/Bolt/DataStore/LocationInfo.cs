using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class LocationInfo
    {
        public string accountName { get; set; }
        public string accountKey { get; set; }
        public SynchronizerType st { get; set;}

        public LocationInfo(string accName, string accKey, SynchronizerType St)
        {
            accountName = accName;
            accountKey = accKey;
            st = St;
        }
    }
}
