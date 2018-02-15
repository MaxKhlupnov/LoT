using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.IO;
using System.ServiceModel.Web;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Platform
{
    [ServiceContract]
    public interface IHomeOSInfo
    {
        [OperationContract, WebGet(UriTemplate = "/index.html")]
        Stream GetDefaultPage();

        [OperationContract, WebGet(UriTemplate = "/")]
        Stream GetEmptyDefaultPage();

        //[OperationContract, WebGet(UriTemplate = "/runningmodules.txt")]
        //Stream GetRunningModules();

        // the two policy files
        [OperationContract, WebGet(UriTemplate = "/clientaccesspolicy.xml")]
        Stream GetSilverlightPolicy();
        [OperationContract, WebGet(UriTemplate = "/crossdomain.xml")]
        Stream GetFlashPolicy();
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class InfoService : IHomeOSInfo, IDisposable
    {
        Platform platform;
        VLogger logger;
        ServiceHost host;

        public InfoService (Platform platform, VLogger logger)
        {
            this.platform = platform;
            this.logger = logger;

            string homeIdPart = string.Empty;

            //if (HomeOS.Shared.Globals.HomeId != null)
            //{
            //    homeIdPart = "/" + HomeOS.Shared.Globals.HomeId;
            //}

            host = new ServiceHost(this, new Uri(HomeOS.Hub.Common.Constants.InfoServiceAddress + homeIdPart));
            host.AddServiceEndpoint(typeof(IHomeOSInfo), new WebHttpBinding(), "").Behaviors.Add(new System.ServiceModel.Description.WebHttpBehavior());
            
            var smb = new System.ServiceModel.Description.ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            host.Description.Behaviors.Add(smb);

            try
            {
                host.Open();
            }
            catch (Exception e)
            {
                logger.Log("Could not open the service host: " + e.Message + @"
Possible issues: 1) are you running the command prompt / Visual Studio in administrator mode?      
                 2) is another instance of Platform running?
                 3) is a local copy of Gatekeeper running?
                 4) is another process occupying the InfoServicePort (51430)?");

                throw e;
            }
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
                host.Close();
            }
        }

        Stream StringToStream(string result, string contentType = "application/xml")
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = contentType;
            return new MemoryStream(Encoding.UTF8.GetBytes(result));
        }

        public Stream GetDefaultPage()
        {

            //string result = "Welcome to HomeOS";
            //return StringToStream(result, "text/html");

            string page = "<head> <meta HTTP-EQUIV=\"REFRESH\" content=\"0; url=GuiWeb/index.html\"></head>";
            return StringToStream(page, "text/html");
        }

        public Stream GetEmptyDefaultPage()
        {
            return GetDefaultPage();
        }

        //public Stream GetRunningModules() {

        //    string result="";

        //    foreach (VModuleInfo moduleInfo in platform.GetRunningModules()) {
        //        result += String.Format("module: {0} {1}\n", moduleInfo.AppName(), moduleInfo.FriendlyName());
        //    }

        //    return StringToStream(result, "text/plain");
        //}


        public Stream GetSilverlightPolicy()
        {
            string result = @"<?xml version=""1.0"" encoding=""utf-8""?>
<access-policy>
 <cross-domain-access>
 <policy>
 <allow-from http-request-headers=""*"">
 <domain uri=""*""/>
 </allow-from>
 <grant-to>
 <resource path=""/"" include-subpaths=""true""/>
 </grant-to>
 </policy>
 </cross-domain-access>
</access-policy>";
            return StringToStream(result);
        }
        public Stream GetFlashPolicy()
        {
            string result = @"<?xml version=""1.0""?>
<!DOCTYPE cross-domain-policy SYSTEM ""http://www.macromedia.com/xml/dtds/cross-domain-policy.dtd"">
<cross-domain-policy>
 <allow-access-from domain=""*"" />
</cross-domain-policy>";
            return StringToStream(result);
        }
    }

}
