using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;
using System.Net;
using System.Net.Sockets;

namespace HomeOS.Hub.Platform
{
    /// <summary>
    /// Helps the mobile app (Dashboard) discover the platform
    /// </summary>
    class DiscoveryHelper
    {

        Platform platform;
        VLogger logger;

        UdpClient listener;

        public DiscoveryHelper(Platform platform, VLogger logger)
        {
            this.platform = platform;
            this.logger = logger;

            listener = new UdpClient(new IPEndPoint(IPAddress.Any, Common.Constants.PlatformDiscoveryPort));

            BeginReceive();
        }

        private void BeginReceive()
        {
            try
            {
                listener.BeginReceive(ReceiveCallback, null);
            }
            catch (ObjectDisposedException e)
            { 
              //ignore this exception; it occurs when we are shutting down
            }
            catch (Exception e)
            {
                logger.Log("Unexpected exception when starting to listen: {0}", e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {

            //doesn't matter how we initialize this; it'll get overwritten by the EndReceive call
            IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                byte[] receivedBytes = listener.EndReceive(ar, ref remoteEndpoint);

                string receivedString = Encoding.ASCII.GetString(receivedBytes);

                if (receivedString.Equals(Common.Constants.PlatformDiscoveryQueryStr))
                {
                    byte[] bytesToSend = Encoding.ASCII.GetBytes(Common.Constants.PlatformDiscoveryResponseStr);

                    listener.Send(bytesToSend, bytesToSend.Length, remoteEndpoint);

                    logger.Log("DiscoveryHelper got discovery request from {0}", remoteEndpoint.ToString());
                }
                else
                {
                    logger.Log("DiscoveryHelper got unknown query {0} from {1}", receivedString, remoteEndpoint.ToString());
                }
            }
            catch (ObjectDisposedException e)
            {
                //ignore this exception; it occurs when we are shutting down
            }
            catch (Exception e)
            {
                logger.Log("Exception in discovery helper: {0}", e.ToString());
            }

            BeginReceive();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                listener.Close();
            }
        }
    }
}
