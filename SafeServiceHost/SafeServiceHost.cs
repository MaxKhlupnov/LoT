using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Policy;
using System.IdentityModel.Selectors;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;


namespace HomeOS.Hub.Common
{

    public sealed class SafeServiceHost : ServiceHost
    {

        public SafeServiceHost(object instance, params Uri[] baseAddresses)
            : base(instance, baseAddresses) 
        {
            this.Authentication.AuthenticationSchemes = AuthenticationSchemes.Basic;
            this.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = System.ServiceModel.Security.UserNamePasswordValidationMode.Custom;
            this.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new CustomUserNameValidator(); 
        }

       public override void AddServiceEndpoint(ServiceEndpoint endpoint)
       {
           if(endpoint.Binding.GetType().Equals(typeof(WebHttpBinding)))
           {
               var webBinding  = (WebHttpBinding)endpoint.Binding;
               webBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.InheritedFromHost;
           }
           base.AddServiceEndpoint(endpoint);

       }




     



    }



    public class CustomUserNameValidator : UserNamePasswordValidator
    {
        // This method validates users. It allows in two users, 
        // test1 and test2 with passwords 1tset and 2tset respectively.
        // This code is for illustration purposes only and 
        // MUST NOT be used in a production environment because it 
        // is NOT secure.
        public override void Validate(string userName, string password)
        {
            if (null == userName || null == password)
            {
                throw new ArgumentNullException();
            }

            if (!(userName == "test1" && password == "1tset") && !(userName == "test2" && password == "2tset"))
            {
                //  throw new SecurityTokenException("Unknown Username or Password");

                // throw new ArgumentException();
                //throw new SecurityTokenValidationException("yee haww");
                //  throw new FaultException("Unknown Username or Incorrect Password");
                var rejectEx = new WebFaultException(HttpStatusCode.Unauthorized);
                rejectEx.Data.Add("HttpStatusCode", rejectEx.StatusCode);
                throw rejectEx;
            }
        }
    }

}


