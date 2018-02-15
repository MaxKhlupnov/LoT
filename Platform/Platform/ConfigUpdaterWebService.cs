using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;
using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;


namespace HomeOS.Hub.Platform
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class ConfigUpdaterWebService : ISimplexConfigUpdaterContract
    {
        public static ServiceHost CreateServiceHost(ISimplexConfigUpdaterContract instance,
                                                     Uri baseAddress)
        {
            ServiceHost service = new ServiceHost(instance, baseAddress);
            var contract = ContractDescription.GetContract(typeof(ISimplexConfigUpdaterContract));

            var webBinding = new WebHttpBinding();

            var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(baseAddress));
            WebHttpBehavior webBehaviour = new WebHttpBehavior();
            webBehaviour.HelpEnabled = true;
            webEndPoint.EndpointBehaviors.Add(webBehaviour);

            service.AddServiceEndpoint(webEndPoint);

          // service.Description.Behaviors.Add(new ServiceMetadataBehavior());
           //var metaBinding = new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly);
           //metaBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.InheritedFromHost; 
           //service.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");

            return service;
        }

        private VLogger logger;
        private ConfigUpdater configUpdater;

        public ConfigUpdaterWebService(VLogger logger, ConfigUpdater updater)
        {
            this.logger = logger;
            this.configUpdater = updater;
        }


        public UpdateStatus Status()
        {
            return this.configUpdater.LastStatus();
        }

        
        public bool SetDueTime(int dueTime)
        {
            return this.configUpdater.SetDueTime(dueTime);
        }

        public bool SyncNow()
        {
            return this.SetDueTime(500);
        }

    }

        

        [ServiceContract]
        public interface ISimplexConfigUpdaterContract
        {
            [OperationContract]
            [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Xml , UriTemplate="/status")]
            UpdateStatus Status();

            [OperationContract]
            [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Xml, UriTemplate = "/syncnow")]
            bool SyncNow();
        }



        [DataContract]
        public class UpdateStatus
        {
            [DataMember]
            public string versionDownloaded { get; set; }
            [DataMember]
            public string versionUploaded { get; set; }
            [DataMember]
            public Nullable<DateTime> lastConfigSync { get; set; }
            [DataMember]
            public Nullable<DateTime> lastConfigDownload { get; set; }
            [DataMember]
            public Nullable<DateTime> lastConfigUpload { get; set; }

            [DataMember]
            public int frequency { get; set; }

            public UpdateStatus()
            { }

            public UpdateStatus(int frequency)
            {
                this.frequency = frequency;
                versionDownloaded = null;
                versionUploaded = null;
                lastConfigSync = null;
                lastConfigUpload = null;
                lastConfigUpload = null; 
            }

        }

       
    
}
