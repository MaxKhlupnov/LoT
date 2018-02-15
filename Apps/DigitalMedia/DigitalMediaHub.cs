using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

[assembly: OwinStartup(typeof(HomeOS.Hub.Apps.DigitalMedia.Startup))]
namespace HomeOS.Hub.Apps.DigitalMedia
{

    /// <summary>
    /// Used by OWIN's startup process. 
    /// </summary>
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
           
            app.MapSignalR();

        }
    }
    public class DigitalMediaHub : Microsoft.AspNet.SignalR.Hub
    {

        public void OnConnectEvent()
        {
            Clients.All.onConnectEvent();
        }

        public void OnDigitalEvent(int slot, int join, bool value)
        {
            Clients.All.onDigitalEvent(slot,join,value);
        }

        public void OnDisconnectEvent(string DisconnectReasonMessage)
        {
            Clients.All.onDisconnectEvent(DisconnectReasonMessage);
        }

        public void OnErrorEvent(string ErrorMessage)
        {
            Clients.All.onErrorEvent(ErrorMessage);
        }
    }
}