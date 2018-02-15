using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeOS.Hub.Common;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Drawing;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Drivers.Gadgeteer
{
    public abstract class DriverGadgeteerBase : ModuleBase
    {
        protected string deviceId;
        protected IPAddress deviceIp;
        protected Port devicePort;
        protected SafeThread worker = null;
        protected WebFileServer imageServer;

        protected abstract List<VRole> GetRoleList();
        protected abstract void WorkerThread();
        protected abstract List<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> parameters);

        public override void Start()
        {
            try
            {
                string[] words = moduleInfo.Args();

                deviceId = words[0];
            }
            catch (Exception e)
            {
                logger.Log("{0}: Improper arguments: {1}. Exiting module", this.ToString(), e.ToString());
                return;
            }

            //get the IP address
            deviceIp = GetDeviceIp(deviceId);

            if (deviceIp == null)
            {
                logger.Log("{0} did not get a device ip for deviceId: {1}. Returning", base.moduleInfo.BinaryName(), deviceId.ToString());
                return;
            }

            //add the service port
            VPortInfo pInfo = GetPortInfoFromPlatform("gadgeteer-" + deviceId);
            devicePort = InitPort(pInfo);

            // add role and register with platform
            BindRoles(devicePort, GetRoleList(), OnOperationInvoke);
            RegisterPortWithPlatform(devicePort);

            worker = new SafeThread(delegate()
            {
                WorkerThread();
            }, "DriverGadgeteer-WorkerThread", logger);
            worker.Start();

            imageServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);
        }

        public IPAddress GetDeviceIp(string deviceId)
        {
            //if the Id is an IP Address itself, return that.
            //else get the Ip from platform

            IPAddress ipAddress = null;

            try
            {
                ipAddress = IPAddress.Parse(deviceId);
                return ipAddress;
            }
            catch (Exception)
            {
            }

            string ipAddrStr = GetDeviceIpAddress(deviceId);

            try
            {
                ipAddress = IPAddress.Parse(ipAddrStr);
                return ipAddress;
            }
            catch (Exception)
            {
                logger.Log("{0} couldn't get IP address from {1} or {2}", this.ToString(), deviceId, ipAddrStr);
            }

            return null;
        }

        public override void Stop()
        {
            if (worker != null)
                worker.Abort();
        }

        public override string GetDescription(string hint)
        {
            logger.Log("DriverGadgeteer.GetDescription for {0}", hint);
            return moduleInfo.FriendlyName();
        }

        public override string GetImageUrl(string hint)
        {
            //return moduleInfo.BaseURL() + "/icon.png";
            return "icon.png";
        }

        //we have nothing to do with other ports
        public override void PortRegistered(VPort port) { }
        public override void PortDeregistered(VPort port) { }
    }
}
