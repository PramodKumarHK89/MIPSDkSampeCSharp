using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MIPSDkSampe
{
    internal static class AuthClass
    {
        private static IPublicClientApplication application;

        internal static void InitializeMSAL()
        {
            string authority = string.Concat(ConfigurationManager.AppSettings.Get("Authority"), ConfigurationManager.AppSettings.Get("TenantGuid"));

            // Initialize the MSAL library by building a public client application
            application = PublicClientApplicationBuilder.Create(ConfigurationManager.AppSettings.Get("ClientId"))
                                                    .WithAuthority(authority)
                                                    .WithDefaultRedirectUri()
                                                    .Build();
        }
        internal static async Task<string> SignInUserAndGetTokenUsingMSAL(string[] scopes)
        {
            AuthenticationResult result;
            try
            {
                var accounts = await application.GetAccountsAsync();
                result = await application.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                 .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                result = await application.AcquireTokenInteractive(scopes)
                 .WithClaims(ex.Claims)
                 .ExecuteAsync();
            }
            return result.AccessToken;
        }
        internal static async Task<IAccount> SignInUserAndGetAccountUsingMSAL(string[] scopes)
        {
            AuthenticationResult result;
            try
            {
                var accounts = await application.GetAccountsAsync();
                result = await application.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                 .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                result = await application.AcquireTokenInteractive(scopes)
                 .WithClaims(ex.Claims)
                 .ExecuteAsync();
            }
            return result.Account;
        }
    }
}
