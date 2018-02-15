using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;
using System.ServiceModel.Web;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Apps.Alerts
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class DoorNotifierSvc : ISimplexDoorNotifierContract
    {
        
        public static SafeServiceHost CreateServiceHost(VLogger logger, ModuleBase moduleBase, ISimplexDoorNotifierContract instance,
                                                     string address)
        {
            //SafeServiceHost service = new SafeServiceHost(logger, moduleBase, instance, address);

            SafeServiceHost service = new SafeServiceHost(logger, moduleBase, instance, address);

            //BasicHttpBinding binding = new BasicHttpBinding();

            var contract = ContractDescription.GetContract(typeof(ISimplexDoorNotifierContract));

            var webBinding = new WebHttpBinding();
            var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(service.BaseAddresses()[0]));
            webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());

            service.AddServiceEndpoint(webEndPoint);

            service.AddServiceMetadataBehavior(new ServiceMetadataBehavior());

            //service.Description.Behaviors.Add(new ServiceMetadataBehavior());
            //service.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");

            return service;
        }

        protected VLogger logger;
        DoorNotifier doorNotifier;

        public DoorNotifierSvc(VLogger logger, DoorNotifier doorNotifier)
        {
            this.logger = logger;
            this.doorNotifier = doorNotifier;
        }

        public string GetSettings()
        {
            //if (!doorNotifier.IsValidUser(username, password)) 
            //    return "Incorrect username, password";
            //else 
            string retVal = "";
            try
            {
                retVal= doorNotifier.GetSettings().ToString();
                return retVal;
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetSetting: " + e);
                return retVal;
            }
        }

        public string SetSettings(string mode, int startHourMin, int endHourMin, int suppressionSeconds)
        {
            //if (!doorNotifier.IsValidUser())
            //    return "Incorrect username, password";
            //else
            //{
            string retVal = "";
            try
            {
                AlertSettings settings = new AlertSettings();
                settings.Mode = (AlertMode)Enum.Parse(typeof(AlertMode), mode, true);
                settings.StartHourMin = startHourMin;
                settings.EndHourMin = endHourMin;
                settings.SuppressSeconds = suppressionSeconds;

                doorNotifier.UpdateSettings(settings);
                retVal= "settings changed to " + settings.ToString();
                return retVal;
            }
            catch (Exception e)
            {
                logger.Log("Got exception in SetSettings: " + e);
                return retVal;
            }
                
           // }
        }

        #region old signatures
        //public List<Alert> GetAlerts(string mode, DateTime timeReference, int numAlerts, 
        //                                           string username, string password)
        //{
        //    if (!doorNotifier.IsValidUser(username, password))
        //        return null;

        //    if (mode.Equals("newer"))
        //    {
        //        return doorNotifier.GetNewerAlerts(timeReference, numAlerts);
        //    }
        //    else if (mode.Equals("older"))
        //    {
        //        return doorNotifier.GetOlderAlerts(timeReference, numAlerts);
        //    }
        //    else // mode.Equals("latest"))
        //    {
        //        return doorNotifier.GetMostRecentAlerts(numAlerts);
        //    }
        //}

        //public string SetAcknowledgment(DateTime timeReference, bool acknowledgment, string username, string password)
        //{
        //    if (!doorNotifier.IsValidUser(username, password))
        //        return "Incorrect username, password";

        //    doorNotifier.SetAcknowledgment(timeReference, acknowledgment);

        //    return "success";
        //}
        #endregion

        public List<string> GetAlerts(string mode, string time, int numAlerts)
        {
            try
            {
                List<Alert> listAlerts;

                if (mode.Equals("newer"))
                {
                    //AJB moving in here because having trouble passing a time string from javascript that will work so only using "latest"
                    DateTime timeReference = DateTime.Parse(time);
                    listAlerts = doorNotifier.GetNewerAlerts(timeReference, numAlerts);
                }
                else if (mode.Equals("older"))
                {
                    DateTime timeReference = DateTime.Parse(time);
                    listAlerts = doorNotifier.GetOlderAlerts(timeReference, numAlerts);
                }
                else // mode.Equals("latest"))
                {
                    listAlerts = doorNotifier.GetMostRecentAlerts(numAlerts);
                }

                List<string> retList = new List<string>();
                retList.Add("");  //By convention if first element is empty it was successful

                foreach (var alert in listAlerts)
                {
                    retList.Add(alert.FriendlyToString());
                }

                return retList;
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetAlerts: " + e);
                return new List<string>();
            }
        }


        public string SetAcknowledgment(string timeReference, bool acknowledgment)
        {
            //if (!doorNotifier.IsValidUser(username, password))
            //    return "Incorrect username, password";
            try
            {
                DateTime time = DateTime.Parse(timeReference);

                doorNotifier.SetAcknowledgment(time, acknowledgment);

                return "success";
            }
            catch (Exception e)
            {
                logger.Log("Got exception in SetAcknowledgment: " + e);
                return String.Empty;
            }
        }

        public List<string> GetMonitoredDevices()
        {
            //if (!doorNotifier.IsValidUser(username, password))
            //    return null;
            List<string> retVal = new List<string>();
            try
            {
                retVal = doorNotifier.GetMonitoredDevices();
                return retVal;
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetMonitoredDevices: " + e);
                return retVal;
            }

        }

        public void UpdateEmail(string email)
        {
            try
            {
                doorNotifier.UpdateEmail(email);
            }
            catch (Exception e)
            {
                logger.Log("Got exception in UpdateEmail: " + e);
            }
        }

        public List<string> GetEmail()
        {
            try
            {
                string email = doorNotifier.GetEmail();

                if (string.IsNullOrWhiteSpace(email))
                    return new List<string>() { "email not set" };
                else
                    return new List<string>() { "", email };
            }
            catch (Exception e)
            {
                logger.Log("Got exception in GetEmail: " + e);
                return new List<string>() { e.Message };
            }
        }

    }


    [ServiceContract]
    public interface ISimplexDoorNotifierContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string GetSettings();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string SetSettings(string mode, int startHourMin, int endHourMin, int suppressionSeconds);


        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetAlerts(string mode, string timeReference, int numAlerts);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        string SetAcknowledgment(string timeReference, bool acknowledgment);


        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetMonitoredDevices();

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        void UpdateEmail(string email);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        List<string> GetEmail();

        //old signatures
        //[OperationContract]
        //List<Alert> GetAlerts(string mode, DateTime timeReference, int numAlerts,
        //                                    string username, string password);

        //[OperationContract]
        //string SetAcknowledgment(DateTime timeReference, bool acknowledgment, string username, string password);
    }
}