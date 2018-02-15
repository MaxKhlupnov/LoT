using HomeOS.Hub.Common;
using HomeOS.Hub.Common.SafeTokenHandler;
using HomeOS.Hub.Platform;
using HomeOS.Hub.Platform.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using WindowsLive;

namespace HomeOS.Hub.Platform.Authentication
{
    public interface AuthSvcViewOfPlatform
    {
        string GetConfSetting(string paramName);
        bool IsValidUser(string username, string password);
        List<string> GetPrivilegeLevels(string moduleFriendlyName, string domainOfAccess);
        string GetLiveIdUserName(string LiveIdUniqueUserToken);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class AuthenticationService : IAuthenticationService
    {

        private VLogger logger;
        private AuthSvcViewOfPlatform platform;


        private static readonly string cookieName = "homeostoken";
        private static readonly string cookieStackHead = cookieName + ":tok";
        private static readonly int cookieStackHeadExpiry = 7 * 24 * 3600;

        public readonly static string authEndpoint = "auth";
        public static readonly string GuiWebAddLiveIdUserPage = "AddLiveIdUser.html";

        private const string setCookieJS = "<script type='text/javascript'> function setCookie(c_name,value,expirySeconds) { var exdate=new Date(); exdate.setTime(exdate.getTime() + 1000*expirySeconds);  var c_value=escape(value) + ((expirySeconds==null) ? '' : '; expires='+exdate.toGMTString()); c_value += '; path=/'; document.cookie=c_name + '=' + c_value;  } </script>";
        private const string getCookieJS = "<script type='text/javascript'> function getCookie(c_name) { var c_value = document.cookie; var c_start = c_value.indexOf(' ' + c_name + '='); if (c_start == -1) { c_start = c_value.indexOf(c_name + '='); } if (c_start == -1) { c_value = null; } else { c_start = c_value.indexOf('=', c_start) + 1; var c_end = c_value.indexOf(';', c_start); if (c_end == -1) { c_end = c_value.length; } c_value = unescape(c_value.substring(c_start, c_end)); } return c_value; } </script>";
        private const string delCookieJS = "<script type='text/javascript'>function delCookie(name){document.cookie = name + '=; path=/; expires=Thu, 01 Jan 1970 00:00:01 GMT;';}</script>";
        private const string redirectJS = "<script type='text/javascript'>function redirect(url) { document.location.href=url;} </script>";

        private static string pushTokenJS = getCookieJS + setCookieJS + "<script type='text/javascript'> function pushToken(token, expirySeconds) { var i=null;  var currTokName = getCookie('" + cookieStackHead + "'); if(currTokName==null || !currTokName) { i=-1; } else { i=currTokName.split(':')[1]; } if(i==null) {i=-1;} i=parseInt(i)+1; setCookie('" + cookieName + ":'+i , token, expirySeconds ); setCookie('" + cookieStackHead + "', '" + cookieName + ":'+i , " + cookieStackHeadExpiry + "  ); return getCookie('" + cookieStackHead + "');   } </script> ";
        private static string popTokenJS = getCookieJS + setCookieJS + delCookieJS + "<script type='text/javascript'> function popToken() { var i=null;  var currTokName = getCookie('" + cookieStackHead + "'); if(currTokName==null || !currTokName) { delCookie('" + cookieStackHead + "'); return null; } else { i=currTokName.split(':')[1]; } i=parseInt(i)-1; if(i>=0) { setCookie('" + cookieStackHead + "','" + cookieName + ":'+i); } else { delCookie('" + cookieStackHead + "'); }  var retVal = getCookie(currTokName);   delCookie(currTokName);  return retVal;   } </script> ";


        public AuthenticationService(VLogger logger, AuthSvcViewOfPlatform platform)
        {
            this.logger = logger;
            this.platform = platform;


        }

        public static ServiceHost CreateServiceHost(VLogger logger, VPlatform platform, IAuthenticationService instance)
        {
            string homeIdPart = string.Empty, homeId = platform.GetConfSetting("HomeId");
            if (!string.IsNullOrEmpty(homeId))
                homeIdPart = "/" + homeId;

            ServiceHost serviceHost = new ServiceHost(instance, new Uri(Constants.InfoServiceAddress + homeIdPart + "/" + authEndpoint));
            var contract = ContractDescription.GetContract(typeof(IAuthenticationService));
            var webBinding = new WebHttpBinding();
            var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(new Uri(Constants.InfoServiceAddress + homeIdPart + "/" + authEndpoint)));
            webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());
            serviceHost.AddServiceEndpoint(webEndPoint);
            return serviceHost;
        }


        #region redirect endpoints. Responsible for setting the token into cookie
        public Stream RedirectOp_stream(Stream input)
        {
            var streamReader = new StreamReader(input);
            string streamString = streamReader.ReadToEnd();
            streamReader.Close();
            System.Collections.Specialized.NameValueCollection nvc = HttpUtility.ParseQueryString(streamString);

            string action = string.IsNullOrEmpty(nvc["action"]) ? "" : nvc["action"];
            string stoken = string.IsNullOrEmpty(nvc["stoken"]) ? "" : nvc["stoken"];
            string appctx = string.IsNullOrEmpty(nvc["appctx"]) ? "" : nvc["appctx"];
            string scheme = string.IsNullOrEmpty(nvc["scheme"]) ? "" : nvc["scheme"];
            stoken = "Liveid " + stoken;
            return RedirectOp(stoken, appctx, action, scheme);

        }

        public Stream RedirectOp(string stoken, string appctx, string action, string scheme)
        {
            string html = "";

            if (Constants.PrivilegeLevels.Keys.Contains(scheme))
            {
                string tokenString = "level=" + Constants.PrivilegeLevels[scheme] + "&name=" + scheme + "&user=" + GetUserName(scheme, stoken) + "&token=" + stoken  ;

                switch (action)
                {
                    case "login":
                        html += "<html>" + pushTokenJS + redirectJS + "<script>" + "pushToken('" + tokenString + "', " + Constants.PrivilegeLevelTokenExpiry[scheme] + " );" + "redirect('" + appctx + "');" + " </script>" + "</html>";
                        break;
                    case "logout":
                        html += "<html>" + popTokenJS + "<script>" + "document.write(popToken()); </script>" + "</html>";
                        break;
                    case "clearcookie":
                        // not implemented
                        break;
                    default:
                        break;
                }

            }
            else
            {
                html = "<html><h1>Authentication Scheme Type: " + scheme + " Not Supported. </h1></html>";
            }

            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            byte[] htmlBytes = Encoding.UTF8.GetBytes(html);
            return new MemoryStream(htmlBytes);
        }

        public Stream RedirectOp_get(string stoken, string appctx, string action, string scheme)
        {
            
            #region treat the add liveid user as a special app and handle it separately
            string html = "";
            if (appctx.ToLower().Contains(GuiWebAddLiveIdUserPage.ToLower()) && scheme.Equals(Constants.LiveId,StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    Uri appctxUri = new Uri(appctx);
                    var dict = appctxUri.Query.Split(',').Select(s => s.Split('=')).ToDictionary(a => a[0].Trim(), a => a[1].Trim());

                    string function=dict["?function"];
                    string userName = dict["userName"];
                    string groupName = dict["groupName"];
                    string liveId = dict["liveId"];
                    string liveIdUniqueUserToken = dict["liveIdUniqueUserToken"];
                    
                    html += HandleAddUserGuiWebPage(stoken, dict);

                    if (WebOperationContext.Current != null)
                        WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
                    byte[] htmlBytes = Encoding.UTF8.GetBytes(html);
                    return new MemoryStream(htmlBytes);
                }
                catch (Exception e)
                {
                    // if exception in handling this request as a GuiAddUserWebPage then treat it simply as other requests
                    logger.Log("exception in handling {0} in authservice: {1} ", GuiWebAddLiveIdUserPage, e.Message );
                }

                
            }
            #endregion

            // for all others set token in cookie store and redirect back to the app url i.e. appctx
            return RedirectOp(stoken, appctx, action, scheme);
        }


        private string HandleAddUserGuiWebPage(string stoken, Dictionary<string,string> dict)
        {
            string html="";
            try
            {
                WindowsLiveLogin wll = new WindowsLiveLogin(Constants.LiveIdappId, Constants.LiveIdappsecret, Constants.LiveIdsecurityAlgorithm, true, Constants.LiveIdpolicyURL, Constants.LiveIdreturnURL);
                WindowsLiveLogin.User windowsliveiduser = wll.ProcessToken(stoken);

                if (windowsliveiduser == null)
                    throw new Exception("unable to decrypt liveid token");
                else if (DateTime.UtcNow.Subtract(windowsliveiduser.Timestamp).TotalMilliseconds <= Constants.PrivilegeLevelTokenExpiry[Constants.LiveId] * 1000)
                {
                    dict["liveIdUniqueUserToken"] = windowsliveiduser.Id;
                    string redirectTo = "../" + Constants.GuiServiceSuffixWeb + "/" + GuiWebAddLiveIdUserPage;

                    foreach (string param in dict.Keys)
                    {
                        redirectTo += param + "=" + dict[param] + ",";
                    }
                    redirectTo = redirectTo.TrimEnd(',');

                    html += "<html> " + redirectJS + "<script type='text/javascript'>redirect(\"" + redirectTo + "\");</script>";
                }
                else
                    throw new Exception("Token provided is expired.");
            }
            catch (Exception e)
            {
                logger.Log("Unable to add user. Exception : " + e);
                string redirectTo = "../" + Constants.GuiServiceSuffixWeb + "/" + GuiWebAddLiveIdUserPage + "?function=message,message= User add failed! " + e.Message;
                html += "<html> " + redirectJS + "<script type='text/javascript'>redirect(\"" + redirectTo + "\");</script>";
            }
            return html;

        }


        private string GetUserName(string scheme,string  stoken)
        {
            // if this is a liveID authenticated user. he must have a name associated with this token
            if (scheme.Equals(Constants.LiveId, StringComparison.CurrentCultureIgnoreCase))
            {
                WindowsLiveLogin wll = new WindowsLiveLogin(Constants.LiveIdappId, Constants.LiveIdappsecret, Constants.LiveIdsecurityAlgorithm, true, Constants.LiveIdpolicyURL, Constants.LiveIdreturnURL);
                WindowsLiveLogin.User windowsliveiduser = wll.ProcessToken(stoken);
                string name = platform.GetLiveIdUserName(windowsliveiduser.Id);
                if (string.IsNullOrEmpty(name))
                    return "unknown";
                else
                    return name;
            }

            
            return scheme;
        }
        
        #endregion


        #region generating local token endpoint and tokens
        public Stream DisplayTokenEndpoint(string user, string appctx)
        {
            string html = "<html><h1>Enter Password for " + user + "</h1>";
            html = html + "<form method='post' action='token' >Password: <input type='password' name='password'/> <input type='hidden' name='user' value='" + user + "' /> <input type='hidden' name='appctx' value='" + appctx + "'> <input type='submit' value='Submit'/></form>";


            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            byte[] htmlBytes = Encoding.UTF8.GetBytes(html);
            if (!platform.IsValidUser(user, "")) // if the user has a password "" then no need to generate the form just generate token and redirect
                return new MemoryStream(htmlBytes);
            else
                return SetToken(user, "", appctx);

        }

        public Stream GenerateToken(Stream input)
        {

            var streamReader = new StreamReader(input);
            string streamString = streamReader.ReadToEnd();
            streamReader.Close();
            System.Collections.Specialized.NameValueCollection nvc = HttpUtility.ParseQueryString(streamString);


            string user = string.IsNullOrEmpty(nvc["user"]) ? "" : nvc["user"];
            string password = string.IsNullOrEmpty(nvc["password"]) ? "" : nvc["password"];
            string appctx = string.IsNullOrEmpty(nvc["appctx"]) ? "" : nvc["appctx"];
            return SetToken(user, password, appctx);

        }

        private Stream SetToken(string user, string password, string appctx)
        {
            string html = "";
            if (platform.IsValidUser(user, password))
            {
                SafeTokenHandler tokenHandler = new HomeOS.Hub.Common.SafeTokenHandler.SafeTokenHandler(Constants.TokenEncryptionSecret);
                string token = HttpUtility.UrlEncode(tokenHandler.GenerateToken(user));
                html = "<html>" + redirectJS + "<script type='text/javascript'> redirect('redirect?stoken=" + token + "&appctx=" + appctx + "&action=login&scheme=" + user + " '); </script></html>";


            }
            else
            {
                html = "<html><h1>Not Authenticated</h1><h2><a href='" + appctx + "'>Click here</a> to go back</h2></html>";
            }
            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            byte[] htmlBytes = Encoding.UTF8.GetBytes(html);
            return new MemoryStream(htmlBytes);

        }
        #endregion

        /// <summary>
        /// Get the PrivilegeLevels() needed for accessing a url i.e. appFriendlyName and domain(local/gatekeeper) 
        /// as a ordered list
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public List<PrivilegeLevel> GetPrivilegeLevels(string url)
        {
            List<PrivilegeLevel> retVal = new List<PrivilegeLevel>();
            try
            {
                Uri invokedUrl = new Uri(url);
                string domain = invokedUrl.Host + ":" + invokedUrl.Port;
                string moduleFriendlyName = invokedUrl.LocalPath.Split('/').ElementAt(2);

                List<string> privilegeLevels = platform.GetPrivilegeLevels(moduleFriendlyName, domain);


                foreach (string level in Constants.PrivilegeLevels.Keys)
                {
                    if (privilegeLevels.Contains(level))
                    {
                        PrivilegeLevel privilegeLevel = new PrivilegeLevel();
                        privilegeLevel.Name = level;
                        privilegeLevel.Level = Constants.PrivilegeLevels[level];
                        privilegeLevel.TokenEndpoint = Constants.PrivilegeLevelTokenEndpoints[level].OriginalString.Replace("DOMAIN", domain);
                        privilegeLevel.TokenEndpoint = privilegeLevel.TokenEndpoint.Replace("<HOMEID>", platform.GetConfSetting("HomeId"));
                        privilegeLevel.TokenEndpoint = privilegeLevel.TokenEndpoint.Replace("<APPCTX>", url);
                        retVal.Add(privilegeLevel);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log("Exception in Authentication Service at GetPrivilegeLevel for URL " + url + ". Exception: " + e);
            }

            return retVal;
        }



        public string GetUrlStatus(string url) // using string to support possible tri-state logic in future
        {

            try
            {
                Uri uri = new Uri(url);

                if (System.ServiceModel.Web.WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri.Host.Equals(uri.Host))
                    return "true";


                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
                request.AllowAutoRedirect = false; // find out if this site is up and don't follow a redirector
                request.Method = "GET";
                request.Timeout = 2000;

                WebResponse response = request.GetResponse();
                response.Dispose();


                // do something with response.Headers to find out information about the request
            }
            catch (Exception)
            {
                //set flag if there was a timeout or some other issues
                return "false";
            }

            return "true";

        }

    }

    [ServiceContract]
    public interface IAuthenticationService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "/redirect")]
        Stream RedirectOp_stream(Stream input);
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, UriTemplate = "/redirect_post")]
        Stream RedirectOp(string stoken, string appctx, string action, string scheme);// scheme = LiveId or HomeOS etc
        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, UriTemplate = "/redirect?stoken={stoken}&appctx={appctx}&action={action}&scheme={scheme}")]
        Stream RedirectOp_get(string stoken, string appctx, string action, string scheme);// scheme = LiveId or HomeOS etc


        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/policy", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json)]
        List<PrivilegeLevel> GetPrivilegeLevels(string url);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json, UriTemplate = "/status?url={url}")]
        string GetUrlStatus(string url);


        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, UriTemplate = "/token?user={user}&appctx={appctx}")]
        Stream DisplayTokenEndpoint(string user, string appctx);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, UriTemplate = "/token")]
        Stream GenerateToken(Stream input);



    }

    [DataContract]
    public class PrivilegeLevel
    {
        [DataMember]
        public int Level { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string TokenEndpoint { get; set; }
    }


}


/*
 
 * 
        /*
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.serviceHost.Close();
                this.webrootService.Dispose();
            }
         public void StartWebFileServer()
        {
            webrootService = new SafeWebFileServer(null, null, Globals.WebRoot, "webroot", logger, platform);
        }
        }

 
            string cookieVal = scheme + " " + stoken;

            if (action == "login")
            {
                
            }

            if (action == "logout")
            {
                html += "<html>" + delCookieJS + "<script>" + "delCookie('" + cookiename + "');" + "</script>" ;
                //WebOperationContext.Current.OutgoingResponse.Headers.Add("Set-Cookie", cookiename + "=" + "''; Expires=Thu, 01 Jan 1970 00:00:01 GMT");
                html += "<h1>Logged Out</h1></html>";
            }

            if (action == "clearcookie")
            {
                html += "<html>" + delCookieJS + "<script>" + "delCookie('" + cookiename + "');" + "</script>" + "</html>";
            }

*/