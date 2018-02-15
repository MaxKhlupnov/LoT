using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class FqStreamID
    {
        public string HomeId { get; set; }
        public string AppId { get; set; }
        public string StreamId { get; set; }

        public FqStreamID(string hid, string aid, string sid)
        {
            HomeId = hid;
            AppId = aid;
            StreamId = sid;
        }

        public override string ToString()
        {
            return HomeId + "/" + AppId + "/" + StreamId;
        }
        
        public string DirName()
        {
            return HomeId + "/" + AppId;
        }
    }
}
