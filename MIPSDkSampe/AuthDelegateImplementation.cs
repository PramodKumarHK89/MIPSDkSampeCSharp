using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.InformationProtection;
using Microsoft.Identity.Client;
using System.Configuration;

namespace MIPSDkSampe
{
    public class AuthDelegateImplementation : IAuthDelegate
    {
        private ApplicationInfo _appInfo;
        // Microsoft Authentication Library IPublicClientApplication
        private IPublicClientApplication _app;
        public AuthDelegateImplementation(ApplicationInfo appInfo)
        {
            _appInfo = appInfo;
        }
        public string AcquireToken(Identity identity, string authority, string resource, string claims)
        {
            //var authorityUri = new Uri(authority);
            //authority = String.Format("https://{0}/{1}", authorityUri.Host, ConfigurationManager.AppSettings.Get("TenantGuid"));

            //_app = PublicClientApplicationBuilder.Create(_appInfo.ApplicationId).WithAuthority(authority).WithDefaultRedirectUri().Build();
            //var accounts = (_app.GetAccountsAsync()).GetAwaiter().GetResult();

            // Append .default to the resource passed in to AcquireToken().
            string[] scopes = new string[] { resource[resource.Length - 1].Equals('/') ? $"{resource}.default" : $"{resource}/.default" };
            //var result = _app.AcquireTokenInteractive(scopes).WithAccount(accounts.FirstOrDefault()).WithPrompt(Prompt.SelectAccount)
            //           .ExecuteAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            return AuthClass.SignInUserAndGetTokenUsingMSAL(scopes).Result;
        }

    }
}
