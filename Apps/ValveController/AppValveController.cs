using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Apps.ValveController
{
    [System.AddIn.AddIn("HomeOS.Hub.Apps.ValveController")]
    public class AppValveController : ModuleBase
    {
        AppValveControllerService service;
        SafeServiceHost serviceHost;
        WebFileServer webUiServer;

        Dictionary<VPort, VCapability> registeredValves = new Dictionary<VPort, VCapability>();

        /// <summary>
        /// The most recently fetched temperature from the sensor
        /// </summary>
        public int Temperature = 0;

        public override void Start()
        {
            logger.Log("Started: {0}", ToString());

            service = new AppValveControllerService(this, logger);

            serviceHost = AppValveControllerService.CreateServiceHost(logger, this, service, moduleInfo.BaseURL() + "/webapp");

            serviceHost.Open();

            webUiServer = new WebFileServer(moduleInfo.BinaryDir(), moduleInfo.BaseURL(), logger);

            logger.Log("{0}: service is open for business at {1}", ToString(), moduleInfo.BaseURL());

            //... get the list of current ports from the platform
            IList<VPort> allPortsList = GetAllPortsFromPlatform();

            if (allPortsList != null)
            {
                foreach (VPort port in allPortsList)
                {
                    PortRegistered(port);
                }
            }
        }

        public override void Stop()
        {
            serviceHost.Abort();
        }

        public override void PortRegistered(VPort port)
        {
            lock (this)
            {
                if (Role.ContainsRole(port, RoleValve.RoleName))
                {
                    VCapability capability = GetCapability(port, Constants.UserSystem);

                    if (registeredValves.ContainsKey(port))
                        registeredValves[port] = capability;
                    else
                        registeredValves.Add(port, capability);

                    if (capability != null)
                    {
                        port.Subscribe(RoleSensor.RoleName, RoleSensor.OpGetName,
                               this.ControlPort, capability, this.ControlPortCapability);
                    }
                }
            }
        }

        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (Role.ContainsRole(port, RoleSensor.RoleName))
                {
                    if (registeredValves.ContainsKey(port))
                    {
                        registeredValves.Remove(port);
                        logger.Log("{0} removed valve port {1}", this.ToString(), port.ToString());
                    }
                }
            }
        }

        public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        { }

        public void SendValveAddress()
        {
            foreach (var port in registeredValves.Keys)
            {
                if (registeredValves[port] == null)
                    registeredValves[port] = GetCapability(port, Constants.UserSystem);

                if (registeredValves[port] != null)
                {
                    IList<VParamType> parameters = new List<VParamType>();

                    port.Invoke(RoleValve.RoleName, RoleValve.OpSend, parameters, ControlPort, registeredValves[port], ControlPortCapability);
                }
            }
        }

        public void DoneValveAddresses()
        {
            foreach (var port in registeredValves.Keys)
            {
                if (registeredValves[port] == null)
                    registeredValves[port] = GetCapability(port, Constants.UserSystem);

                if (registeredValves[port] != null)
                {
                    IList<VParamType> parameters = new List<VParamType>();

                    port.Invoke(RoleValve.RoleName, RoleValve.OpDone, parameters, ControlPort, registeredValves[port], ControlPortCapability);
                }
            }
        }

        public void ResetValveAddresses()
        {
            foreach (var port in registeredValves.Keys)
            {
                if (registeredValves[port] == null)
                    registeredValves[port] = GetCapability(port, Constants.UserSystem);

                if (registeredValves[port] != null)
                {
                    IList<VParamType> parameters = new List<VParamType>();

                    port.Invoke(RoleValve.RoleName, RoleValve.OpReset, parameters, ControlPort, registeredValves[port], ControlPortCapability);
                }
            }
        }

        public void SetAllValves(double percentage)
        {
            percentage = Math.Max(0, Math.Min(100, percentage));
            foreach (var port in registeredValves.Keys)
            {
                if (registeredValves[port] == null)
                    registeredValves[port] = GetCapability(port, Constants.UserSystem);

                if (registeredValves[port] != null)
                {
                    IList<VParamType> parameters = new List<VParamType>();
                    parameters.Add(new ParamType((int)(percentage / 100.0 * 255)));
                    port.Invoke(RoleValve.RoleName, RoleValve.OpSetAllValves, parameters, ControlPort, registeredValves[port], ControlPortCapability);
                }
            }
        }

        public void SetOneValve(int valve, double percentage)
        {
            valve = Math.Min(32, Math.Max(0, valve));
            percentage = Math.Max(0, Math.Min(100, percentage));
            foreach (var port in registeredValves.Keys)
            {
                if (registeredValves[port] == null)
                    registeredValves[port] = GetCapability(port, Constants.UserSystem);

                if (registeredValves[port] != null)
                {
                    IList<VParamType> parameters = new List<VParamType>();
                    parameters.Add(new ParamType(valve));
                    parameters.Add(new ParamType((int)(percentage / 100.0 * 255)));
                    port.Invoke(RoleValve.RoleName, RoleValve.OpSetValve, parameters, ControlPort, registeredValves[port], ControlPortCapability);
                }
            }
        }

        public int GetTotalValveNumber()
        {
            foreach (var port in registeredValves.Keys)
            {
                if (registeredValves[port] == null)
                    registeredValves[port] = GetCapability(port, Constants.UserSystem);

                if (registeredValves[port] != null)
                {
                    IList<VParamType> parameters = new List<VParamType>();
                    IList<VParamType> result = port.Invoke(RoleValve.RoleName, RoleValve.OpGetValveNumber, parameters, ControlPort, registeredValves[port], ControlPortCapability);

                    if (result.Count > 0)
                    {
                        try
                        {
                            int firstValue = (int)result[0].Value();
                            return firstValue;
                        }
                        catch (InvalidCastException e)
                        {
                            logger.Log("Invalid cast exception: " + e.Message + " (" + result[0].Value().ToString() + ")");
                        }
                    }
                }
            }
            return 0;
        }

    }
}
