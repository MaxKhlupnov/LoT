using System.Collections.Generic;

using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;


namespace HomeOS.Hub.Apps.Thermometer
{
    [System.AddIn.AddIn("HomeOS.Hub.Apps.Thermometer")]
    public class AppThermometer : ModuleBase
    {
        AppThermometerService service;
        SafeServiceHost serviceHost;
        WebFileServer webUiServer;

        Dictionary<VPort, VCapability> registeredSensors = new Dictionary<VPort, VCapability>();
        Dictionary<VPort, VCapability> registeredActuators = new Dictionary<VPort, VCapability>();

        /// <summary>
        /// The most recently fetched temperature from the sensor
        /// </summary>
        public int Temperature = 0;

        public override void Start()
        {
            logger.Log("Started: {0}", ToString());

            service = new AppThermometerService(this, logger);

            serviceHost = AppThermometerService.CreateServiceHost(logger, this, service, moduleInfo.BaseURL() + "/webapp");

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
                if (Role.ContainsRole(port, RoleSensor.RoleName))
                {
                    VCapability capability = GetCapability(port, Constants.UserSystem);

                    if (registeredSensors.ContainsKey(port))
                        registeredSensors[port] = capability;
                    else
                        registeredSensors.Add(port, capability);

                    if (capability != null)
                    {
                        port.Subscribe(RoleSensor.RoleName, RoleSensor.OpGetName,
                               this.ControlPort, capability, this.ControlPortCapability);
                    }
                }
                if (Role.ContainsRole(port, RoleActuator.RoleName))
                {
                    VCapability capability = GetCapability(port, Constants.UserSystem);

                    if (registeredActuators.ContainsKey(port))
                        registeredActuators[port] = capability;
                    else
                        registeredActuators.Add(port, capability);

                    if (capability != null)
                    {
                        port.Subscribe(RoleActuator.RoleName, RoleActuator.OpPutName, this.ControlPort, capability, this.ControlPortCapability);
                    }
                }
            }
        }

        public void setLEDs(double low, double high)
        {
            foreach (var port in registeredActuators.Keys)
            {
                if (registeredActuators[port] == null)
                    registeredActuators[port] = GetCapability(port, Constants.UserSystem);

                if (registeredActuators[port] != null)
                {
                    logger.Log(string.Format("Set LEDs {0},{1}", low, high));
                    IList<VParamType> parameters = new List<VParamType>();
                    parameters.Add(new ParamType((int)low));
                    parameters.Add(new ParamType((int)high));

                    port.Invoke(RoleActuator.RoleName, RoleActuator.OpPutName, parameters, ControlPort, registeredActuators[port], ControlPortCapability);
                }
            }
        }

        public override void PortDeregistered(VPort port)
        {
            lock (this)
            {
                if (Role.ContainsRole(port, RoleSensor.RoleName))
                {
                    if (registeredSensors.ContainsKey(port))
                    {
                        registeredSensors.Remove(port);
                        logger.Log("{0} removed sensor port {1}", this.ToString(), port.ToString());
                    }
                }
                if (Role.ContainsRole(port, RoleActuator.RoleName))
                {
                    if (registeredActuators.ContainsKey(port))
                    {
                        registeredActuators.Remove(port);
                        logger.Log("{0} removed actuator port {1}", this.ToString(), port.ToString());
                    }
                }
            }
        }

        public override void OnNotification(string roleName, string opName, IList<VParamType> retVals, VPort senderPort)
        {
            logger.Log("Notitification from {0} for {0}", roleName, opName);
            if (retVals.Count >= 1)
            {
                this.Temperature = (int)retVals[0].Value();
            }
            else
            {
                logger.Log("{0}: got unexpected retvals [{1}] from {2}", ToString(), retVals.Count.ToString(), senderPort.ToString());
            }
        }
    }

}
