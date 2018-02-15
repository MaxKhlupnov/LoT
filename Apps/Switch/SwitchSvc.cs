using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Apps.Switch
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SwitchSvc : ISwitchSvcContract
    {
        public static SafeServiceHost CreateServiceHost(VLogger logger, ModuleBase moduleBase, ISwitchSvcContract instance,
                                                     string address)
        {
            SafeServiceHost service = new SafeServiceHost(logger, moduleBase, instance, address);

            var contract = ContractDescription.GetContract(typeof(ISwitchSvcContract));

            var webBinding = new WebHttpBinding();
            var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(service.BaseAddresses()[0]));
            webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());

            service.AddServiceEndpoint(webEndPoint);

            service.AddServiceMetadataBehavior(new ServiceMetadataBehavior());

            return service;
        }

        protected VLogger logger;
        SwitchMultiLevelController controller;

        public SwitchSvc(VLogger logger, SwitchMultiLevelController controller)
        {
            this.logger = logger;
            this.controller = controller;
        }



        public List<string> GetAllSwitches()
        {
            try
            {
                List<string> retVal = controller.GetAllSwitches();

                retVal.Insert(0, "");

                return retVal;
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetSwitchList: " + e);
                return new List<string>() { e.Message };
            }
        }

        public List<string> SetLevel(string switchFriendlyName, string level)
        {
            try
            {
                double dblLevel = double.Parse(level);

                controller.SetLevel(switchFriendlyName, dblLevel);

                return new List<string>() { "" };
            }
            catch (Exception e)
            {
                logger.Log("Got exception in SetLevel ({0}, {1}): {2}", switchFriendlyName, level, e.ToString());
                return new List<string>() { e.Message };
            }
        }

        public List<string> SetAllSwitches(string level)
        {
            try
            {
                double dblLevel = double.Parse(level);

                controller.SetAllSwitches(dblLevel);

                return new List<string>() { "" };
            }
            catch (Exception e)
            {
                logger.Log("Got exception in SetAllSwitches ({0}): {1}", level, e.ToString());
                return new List<string>() { e.Message };
            }
        }

        public List<string> SetColor(string switchFriendlyName, string red, string green, string blue)
        {
            try
            {
                byte byteRed = byte.Parse(red);
                byte byteGreen = byte.Parse(green);
                byte byteBlue = byte.Parse(blue);

                System.Drawing.Color color = System.Drawing.Color.FromArgb(byteRed, byteGreen, byteBlue);

                controller.SetColor(switchFriendlyName, color);

                return new List<string>() { "" };
            }
            catch (Exception e)
            {
                logger.Log("Got exception in SetColor ({0}, {1}, {2}, {3}): {4}", switchFriendlyName, red, green, blue, e.ToString());
                return new List<string>() { e.Message };
            }
        }

        public List<string> GetColor(string switchFriendlyName)
        {
            try
            {
                System.Drawing.Color color = controller.GetColor(switchFriendlyName);

                return new List<string>() { "", color.R.ToString(), color.G.ToString(), color.B.ToString() };
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetColor ({0}): {1}", switchFriendlyName, e.ToString());
                return new List<string>() { e.Message };
            }
        }


        public List<string> DiscoSwitches()
        {
            try
            {
                controller.DiscoSwitches();
                return new List<string>() { "" };
            }
            catch (Exception e)
            {
                logger.Log("Got exception in DiscoSwitches: " + e);
                return new List<string>() { e.Message };
            }
        }
    }


    [ServiceContract]
    public interface ISwitchSvcContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetAllSwitches();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> DiscoSwitches();


        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> SetLevel(string switchFriendlyName, string level);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> SetAllSwitches(string level);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> SetColor(string switchFriendlyName, string red, string green, string blue);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetColor(string switchFriendlyName);


    }
}