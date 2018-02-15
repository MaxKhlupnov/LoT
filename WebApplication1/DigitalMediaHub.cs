using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace WebApplication1
{
    public class DigitalMediaHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }
    }
}