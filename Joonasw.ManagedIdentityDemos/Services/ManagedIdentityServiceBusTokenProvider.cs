using System;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.ServiceBus.Primitives;

namespace Joonasw.ManagedIdentityDemos.Services
{
    public class ManagedIdentityServiceBusTokenProvider : TokenProvider
    {
        private readonly string _managedIdentityTenantId;

        public ManagedIdentityServiceBusTokenProvider(string managedIdentityTenantId)
        {
            _managedIdentityTenantId = managedIdentityTenantId;
        }

        public override async Task<SecurityToken> GetTokenAsync(string appliesTo, TimeSpan timeout)
        {
            string accessToken = await GetAccessToken("https://servicebus.azure.net/");
            return new JsonSecurityToken(accessToken, appliesTo);
        }

        private async Task<string> GetAccessToken(string resource)
        {
            var authProvider = new AzureServiceTokenProvider();
            string tenantId = _managedIdentityTenantId;

            if (tenantId != null && tenantId.Length == 0)
            {
                tenantId = null; //We want to clearly indicate to the provider if we do not specify a tenant, so no empty strings
            }

            return await authProvider.GetAccessTokenAsync(resource, tenantId);
        }
    }
}
