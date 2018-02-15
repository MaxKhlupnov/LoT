using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;

namespace HomeOS.Hub.Common
{
    public sealed class Notification 
    {
        public string toAddress { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
        public List<Attachment> attachmentList { get; set; }
    }
}
