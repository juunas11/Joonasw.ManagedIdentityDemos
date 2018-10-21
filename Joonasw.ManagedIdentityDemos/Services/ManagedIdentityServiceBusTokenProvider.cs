using System;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.ServiceBus.Primitives;

namespace Joonasw.ManagedIdentityDemos.Services
{
    public class ManagedIdentityServiceBusTokenProvider : TokenProvider
    {
        private readonly string _managedIdentityTenantId;

        public ManagedIdentityServiceBusTokenProvider(string managedIdentityTenantId = null)
        {
            if (string.IsNullOrEmpty(managedIdentityTenantId))
            {
                // Ensure tenant id is null if none given
                _managedIdentityTenantId = null;
            }
            else
            {
                _managedIdentityTenantId = managedIdentityTenantId;
            }
        }

        public override async Task<SecurityToken> GetTokenAsync(string appliesTo, TimeSpan timeout)
        {
            string accessToken = await GetAccessToken("https://servicebus.azure.net/");
            return new JsonSecurityToken(accessToken, appliesTo);
        }

        private async Task<string> GetAccessToken(string resource)
        {
            var authProvider = new AzureServiceTokenProvider();
            return await authProvider.GetAccessTokenAsync(resource, _managedIdentityTenantId);
        }
    }
}
