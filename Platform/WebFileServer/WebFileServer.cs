using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.IO;
using System.ServiceModel.Web;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Platform.WebFileServer
{
    [ServiceContract]
    public interface IWebFileServer
    {
        [OperationContract, WebGet(UriTemplate = "/{*pathname}")]
        Stream GetRelPathName(string pathname);

    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class WebFileServer : IWebFileServer
    {
        VLogger logger;
        string directory;

        public WebFileServer(string directory, string baseUrl, VLogger logger)
        {
            this.logger = logger;
            this.directory = directory;

            ServiceHost host = new ServiceHost(this, new Uri(baseUrl));
            host.AddServiceEndpoint(typeof(IWebFileServer), new WebHttpBinding(), "").Behaviors.Add(new System.ServiceModel.Description.WebHttpBehavior());

            var smb = new System.ServiceModel.Description.ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            host.Description.Behaviors.Add(smb);

            host.Open();
        }            

        public Stream GetFile(string fileName)
        {
            string fullFileName = directory + "\\" + fileName;

            WebOperationContext.Current.OutgoingResponse.Headers["Cache-Control"] = "no-cache";

            if (File.Exists(fullFileName))
            {
                string mimeType = GetMimeType(fullFileName);
                if (mimeType != null)
                    WebOperationContext.Current.OutgoingResponse.ContentType = mimeType;

                return new FileStream(fullFileName, FileMode.Open, FileAccess.Read);
            }
            else
            {
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;

                string errorMessage = String.Format("Did not find the file: {0}\n", fullFileName);

                //we do this because some browsers (IE and chrome) do not display custom error message smaller than 512 bytes
                while (errorMessage.Length < 512)
                    errorMessage += errorMessage;

                return new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
            }
        }

        public Stream GetRelPathName(string pathName)
        {
            string newPathName = pathName.Replace('/', '\\');
            return GetFile(newPathName);
        }

        private string GetMimeType(string fileName) 
        {
            //TODO: we should do mimetypes properly

            if (fileName.EndsWith("html", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith("htm", StringComparison.CurrentCultureIgnoreCase))
            {
                return "text/html";
            }
            else if (fileName.EndsWith("js", StringComparison.CurrentCultureIgnoreCase))
            {
                return "application/javascript";
            }
            else if (fileName.EndsWith("mp4", StringComparison.CurrentCultureIgnoreCase))
            {
                return "video/mp4";
            }
            else if (fileName.EndsWith("jpg", StringComparison.CurrentCultureIgnoreCase))
            {
                return "image/jpeg";
            }
            else if (fileName.EndsWith("css", StringComparison.CurrentCultureIgnoreCase))
            {
                return "text/css";
            }

            return null;
        }

    }
}
