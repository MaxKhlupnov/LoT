using HomeOS.Hub.Platform.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Policy;
using System.IdentityModel.Selectors;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using WindowsLive;


namespace HomeOS.Hub.Common
{
    public interface SafeServicePolicyDecider
    {
        int IsValidAccess(string accessedModule, string domainOfAccess, string privilegeLevel, string userIdentifier);
    }


    public sealed class SafeServiceHost
    {
        private ServiceHost serviceHost;
        private SafeServicePolicyDecider policyDecider = null;
        private static string infoServiceAddress = Constants.InfoServiceAddress;
        private VLogger logger;

        public SafeServiceHost(VLogger logger, SafeServicePolicyDecider consumer, object instance, params string[] addresses)
        {
           
            this.policyDecider = consumer;
            this.logger = logger;
            List<Uri> addressList = new List<Uri>();
            foreach (String address in addresses)
            {
                addressList.Add(new Uri(address));
            }

            serviceHost = new ServiceHost(instance, addressList.ToArray());
            serviceHost.Authentication.ServiceAuthenticationManager = new SafeServiceAuthenticationManager();
            serviceHost.Authorization.ServiceAuthorizationManager = new SafeServiceAuthorizationManager(consumer, this);
        }

        public SafeServiceHost(VLogger logger, Type contractType, SafeServicePolicyDecider consumer, string webAddressSuffix, params string[] addresses)
        {
            object instance = consumer;
            this.policyDecider = consumer;
            this.logger = logger;
            List<Uri> addressList = new List<Uri>();
            foreach (String address in addresses)
            {
                addressList.Add(new Uri(address+webAddressSuffix));
            }

            serviceHost = new ServiceHost(instance, addressList.ToArray());
            serviceHost.Authentication.ServiceAuthenticationManager = new SafeServiceAuthenticationManager();
            serviceHost.Authorization.ServiceAuthorizationManager = new SafeServiceAuthorizationManager(consumer, this);
            
            foreach (string address in addresses)
            {
                var contract = ContractDescription.GetContract(contractType);
                var webBinding = new WebHttpBinding();
                var webEndPoint = new ServiceEndpoint(contract, webBinding, new EndpointAddress(address+webAddressSuffix));
                webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());
                serviceHost.AddServiceEndpoint(webEndPoint);
            }

        }

        public SafeServiceHost(VLogger logger,Type contractType, object contractObject, SafeServicePolicyDecider consumer, string webAddressSuffix, params string[] addresses)
        {
            this.policyDecider = consumer;
            this.logger = logger;
            List<Uri> addressList = new List<Uri>();
            foreach (String address in addresses)
            {
                addressList.Add(new Uri(address + webAddressSuffix));
            }

            serviceHost = new ServiceHost(contractObject, addressList.ToArray());
            serviceHost.Authentication.ServiceAuthenticationManager = new SafeServiceAuthenticationManager();
            serviceHost.Authorization.ServiceAuthorizationManager = new SafeServiceAuthorizationManager(consumer, this);

            foreach (string address in addresses)
            {
                var contractDesc = ContractDescription.GetContract(contractType, contractObject);
                var webBinding = new WebHttpBinding();
                var webEndPoint = new ServiceEndpoint(contractDesc, webBinding, new EndpointAddress(address + webAddressSuffix));
                webEndPoint.EndpointBehaviors.Add(new WebHttpBehavior());
                serviceHost.AddServiceEndpoint(webEndPoint);
            }

        }

        //internal SafeServiceHost(ServiceAuthenticationManager serviceAuthenticationManager, ServiceAuthorizationManager serviceAuthorizationManager , VLogger logger, ModuleBase moduleBase, object instance, params string[] addresses)
        //    :this(logger,moduleBase,instance,addresses)
        //{
        //    serviceHost.Authentication.ServiceAuthenticationManager = serviceAuthenticationManager;
        //    serviceHost.Authorization.ServiceAuthorizationManager = serviceAuthorizationManager;
        //}

        //public SafeServiceHost(ServiceAuthenticationManager serviceAuthenticationManager, ServiceAuthorizationManager serviceAuthorizationManager, VLogger logger, SafeServiceHostViewOfPlatform platform, object instance, params string[] addresses)
        //    : this(logger,platform,instance,addresses)
        //{
        //    serviceHost.Authentication.ServiceAuthenticationManager = serviceAuthenticationManager;
        //    serviceHost.Authorization.ServiceAuthorizationManager = serviceAuthorizationManager;
           
        //}




        public void AddServiceEndpoint(ServiceEndpoint endpoint)
        {
            if (endpoint.Binding.GetType().Equals(typeof(WebHttpBinding)))
            {
                var webBinding = (WebHttpBinding)endpoint.Binding;
            }

            WebHttpBehavior b = (WebHttpBehavior)endpoint.EndpointBehaviors[0];
            b.HelpEnabled = true;
            serviceHost.AddServiceEndpoint(endpoint);
        }

        public void AddServiceMetadataBehavior(ServiceMetadataBehavior smb)
        {
                 serviceHost.Description.Behaviors.Add(smb);
               serviceHost.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
        }

        public void Open()
        {
            try
            {
                serviceHost.Open();
            }
            catch (Exception e)
            {
                logger.Log("Exception in opening ServiceHost: " + e);
            }
        }


        public void Close()
        {
            try
            {
                serviceHost.Close();
            }
            catch (Exception e)
            {
                logger.Log("Exception in Closing ServiceHost: " + e);
            }
        }

        public void Abort()
        {
            try
            {
                serviceHost.Abort();
            }
            catch (Exception e)
            {
                logger.Log("Exception in Aborting ServiceHost: " + e);
            }
        }

        public Uri[] BaseAddresses()
        {
            Uri[] retVal;
            retVal = new Uri[serviceHost.BaseAddresses.Count];
            serviceHost.BaseAddresses.CopyTo(retVal, 0);
            return retVal;
        }


    }


    public class SafeServiceAuthenticationManager : ServiceAuthenticationManager
    {
        public SafeServiceAuthenticationManager()
            : base()
        {
        }

        public override ReadOnlyCollection<IAuthorizationPolicy> Authenticate(ReadOnlyCollection<IAuthorizationPolicy> authPolicy, Uri listenUri, ref Message message)
        {
           
            WebHeaderCollection headers = WebOperationContext.Current.IncomingRequest.Headers;
            message.Properties["Host"] = headers.Get("Host");
            string authHeader = "";
            authHeader = headers.Get("Authorization");

            if (string.IsNullOrEmpty(authHeader))
            {
                message.Properties["AuthScheme"] = "";
                message.Properties["AuthParameter"] = "";
                return authPolicy;
            }

            string[]  schemaParamPair = authHeader.Split(' ');

            if (schemaParamPair.Count().Equals(0)) // both schema and parameter values missing
            {
                message.Properties["AuthScheme"] = "";
                message.Properties["AuthParameter"] = "";
                return authPolicy;
            }

            if (schemaParamPair.Count().Equals(1)) // schema value present but parameter value missing
            {
                message.Properties["AuthScheme"] = schemaParamPair[0];
                message.Properties["AuthParameter"] = "";
                return authPolicy;
            }
            if (schemaParamPair.Count()>=2) // schema value present but parameter value missing
            {
                message.Properties["AuthScheme"] = schemaParamPair[0];
                message.Properties["AuthParameter"] = schemaParamPair[1];
                return authPolicy;
            }
            
            return authPolicy;
        }

    }

    public class SafeServiceAuthorizationManager : ServiceAuthorizationManager
    {
        private SafeServicePolicyDecider consumer;

        private SafeServiceHost safeServiceHost; 
      
        private Dictionary<string, Dictionary<DateTime,bool> > hostTokenResultCache;
        
        private static int hostTokenResultCacheSize = 10;

        private bool enforcePolicies; 

        public SafeServiceAuthorizationManager(SafeServicePolicyDecider consumer, SafeServiceHost safeServiceHost)
            : base()
         {
             hostTokenResultCache = new Dictionary<string, Dictionary<DateTime,bool>>();
             this.consumer = consumer;
             this.safeServiceHost = safeServiceHost;
             this.enforcePolicies = true; 

             // stub to check if policies are not to be enforced
            // Assumption: if the policy is to allow every user from every domain access to every module => EnforcePolicies = false
             string accessedModuleName = safeServiceHost.BaseAddresses()[0].LocalPath.Split('/').ElementAt(2);
             if ((ResultCode)consumer.IsValidAccess(accessedModuleName, "*", "*", "*") == ResultCode.Allow)
                 this.enforcePolicies = false; 

         }

        protected override bool CheckAccessCore(OperationContext operationContext)
        {

            string scheme = null, parameter = null, host= null;

            if (!this.enforcePolicies)
                return true;

            if (OperationContext.Current.IncomingMessageProperties.ContainsKey("Host"))
                host = OperationContext.Current.IncomingMessageProperties["Host"].ToString();

            if (OperationContext.Current.IncomingMessageProperties.ContainsKey("AuthScheme"))
                scheme = OperationContext.Current.IncomingMessageProperties["AuthScheme"].ToString();

            if (OperationContext.Current.IncomingMessageProperties.ContainsKey("AuthParameter"))
                parameter = OperationContext.Current.IncomingMessageProperties["AuthParameter"].ToString();

            if (string.IsNullOrEmpty(scheme) || string.IsNullOrEmpty(parameter) ) // if scheme or parameter are null. throw a forbidden
                ThrowRejection(HttpStatusCode.Unauthorized, "No Valid Token Received In Authorization Header.");

            if (string.IsNullOrEmpty(host)) // if host is null we cannot decide access hence null 
                ThrowRejection(HttpStatusCode.Unauthorized, "Http request has no host parameter.");

            
            if (string.Equals(scheme, Constants.LiveId, StringComparison.OrdinalIgnoreCase)) 
                return HandleLiveId(parameter,host);// handle LiveId token

            else if (string.Equals(scheme, Constants.SystemLow, StringComparison.OrdinalIgnoreCase) || string.Equals(scheme, Constants.SystemHigh, StringComparison.OrdinalIgnoreCase) ) 
            {
                return HandleSystem(parameter, host); 
            }
           

            else
                ThrowRejection(HttpStatusCode.BadRequest, "Given Authorization Scheme Not Supported");

            return false;
        }

        private bool HandleLiveId(string token, string host)
        {
            
                Tuple<bool, bool> inCache = IsInCache(host, token);
                if (inCache.Item1)
                    return inCache.Item2;

                WindowsLiveLogin wll = new WindowsLiveLogin(Constants.LiveIdappId, Constants.LiveIdappsecret, Constants.LiveIdsecurityAlgorithm, true, Constants.LiveIdpolicyURL, Constants.LiveIdreturnURL);
                WindowsLiveLogin.User user = wll.ProcessToken(token);


                if (user == null)
                    ThrowRejection(HttpStatusCode.Unauthorized, "Invalid user token in authorization header.");

                if (DateTime.UtcNow.Subtract(user.Timestamp).TotalMilliseconds > Constants.PrivilegeLevelTokenExpiry[Constants.LiveId] * 1000)
                    ThrowRejection(HttpStatusCode.Unauthorized, "Expired token being presented. Token Expiry: " + Constants.PrivilegeLevelTokenExpiry[Constants.LiveId] + " seconds");

                bool retVal = IsValidAccess(host, Constants.LiveId, user.Id);

                UpdateCache(host, token, user.Timestamp, retVal); // *** updating cache

                return retVal;
        
        }

        private bool HandleSystem(string token, string host)
        {
           
                Tuple<bool, bool> inCache = IsInCache(host, token);
                if (inCache.Item1)
                    return inCache.Item2;

                HomeOS.Hub.Common.SafeTokenHandler.SafeTokenHandler tokenHandler = new HomeOS.Hub.Common.SafeTokenHandler.SafeTokenHandler(Constants.TokenEncryptionSecret);
                HomeOS.Hub.Common.SafeTokenHandler.SafeTokenUser user = tokenHandler.ProcessToken(token);

                if (user == null)
                    ThrowRejection(HttpStatusCode.Unauthorized, "Invalid user token in authorization header.");
                if (!(user.Name.Equals(Constants.SystemLow) || user.Name.Equals(Constants.SystemHigh)))
                    ThrowRejection(HttpStatusCode.Unauthorized, "Invalid user token in authorization header.");

                if (DateTime.UtcNow.Subtract(user.Timestamp).TotalMilliseconds > Constants.PrivilegeLevelTokenExpiry[user.Name] * 1000)
                    ThrowRejection(HttpStatusCode.Unauthorized, "Expired token being presented. Token Expiry: " + Constants.PrivilegeLevelTokenExpiry[user.Name] + " seconds");

                bool retVal = IsValidAccess(host, user.Name, user.Name);

                UpdateCache(host, token, user.Timestamp, retVal); // *** updating cache
                //hostTokenResultCache[host + "," + token]= new Dictionary<DateTime, bool>() { { user.Timestamp, retVal } };
                return retVal;
           
        }

        private void ThrowRejection(HttpStatusCode status, string message)
        {
            var rejectEx = new WebFaultException<String>(message, status);
            rejectEx.Data.Add("HttpStatusCode", rejectEx.StatusCode);

            //this exception is expected, as it triggers the authentication
            //to ignore this error while running in VS debugger uncheck the "break when this exception is user handled" box
            throw rejectEx;
        }

        private bool IsValidAccess(string domainOfAccess, string privilegeLevel, string userIdentifier)
        {
            ResultCode result;

            //NOTE: if this class in the platform appdomain then the platform has no reference to ModuleBase and hence Module Friendly Name
            // Hence we construct "module friendly name" (e.g., GuiWeb, scouts) by looking at the base addresses of the service host

            string accessedModuleName = safeServiceHost.BaseAddresses()[0].LocalPath.Split('/').ElementAt(2);
            result = (ResultCode)consumer.IsValidAccess(accessedModuleName, domainOfAccess, privilegeLevel, userIdentifier);


            if (result == ResultCode.InSufficientPrivilege)
                ThrowRejection(HttpStatusCode.Unauthorized, "Insufficient privilege of given token.");
            if (result == ResultCode.InvalidUser)
                ThrowRejection(HttpStatusCode.Forbidden, "User access for given user not authorized.");
            if (result == ResultCode.ForbiddenAccess)
                ThrowRejection(HttpStatusCode.Forbidden, "User access forbidden by policy.");
            if (result == ResultCode.Allow)
                return true;

            return false;

        }

        private Tuple<bool, bool> IsInCache(string host, string token)
        {
            string key = host + "," + token; 

            if (hostTokenResultCache.ContainsKey(key))
            {
                if (DateTime.UtcNow.Subtract(hostTokenResultCache[key].Keys.ElementAt(0)).TotalMilliseconds <= Constants.PrivilegeLevelTokenExpiry[Constants.LiveId] * 1000)
                {
                    return new Tuple<bool, bool>(true, hostTokenResultCache[key].Values.ElementAt(0));
                }
                else
                {
                    hostTokenResultCache.Remove(key);
                    return new Tuple<bool, bool>(false, true);
                }
            }
            else if (hostTokenResultCache.Count >= hostTokenResultCacheSize)
            {
                hostTokenResultCache.Clear();
                return new Tuple<bool, bool>(false, false);
            }
            else
            {
                return new Tuple<bool, bool>(false, false);
            }
        }

        private void UpdateCache(string host, string token, DateTime timestamp, bool result)
        {
            hostTokenResultCache[host + "," + token] = new Dictionary<DateTime, bool>() { { timestamp, result } };
        }


    }

}




/*
private List<Uri> GetBaseAddresses(string[] addressSuffixes)
{

    List<Uri> retVals = new List<Uri>();
    foreach (String address in addressSuffixes)
    {
        Uri ret;
        if (inModule)
        {
            string friendlyName = moduleBase.GetInfo().FriendlyName();
            ret = new Uri(infoServiceAddress + HomeIdPart() + "/" + friendlyName + address);
        }
        else
            ret = new Uri(infoServiceAddress + HomeIdPart() + "/" + address);

        retVals.Add(ret);

    }
    return retVals;
}

private string HomeIdPart()
{
    string homeId;
    if (inModule)
        homeId = moduleBase.GetConfSetting("HomeId");
    else
        homeId = platform.GetConfSetting("HomeId");

    string homeIdPart = string.Empty;
    if (!string.IsNullOrEmpty(homeId))
        homeIdPart = "/" + homeId;

    return homeIdPart;
}

*/