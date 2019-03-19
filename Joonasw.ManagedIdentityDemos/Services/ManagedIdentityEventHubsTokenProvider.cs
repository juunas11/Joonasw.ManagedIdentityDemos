using System;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.Services.AppAuthentication;

namespace Joonasw.ManagedIdentityDemos.Services
{
    public class ManagedIdentityEventHubsTokenProvider : ITokenProvider
    {
        private readonly string _managedIdentityTenantId;

        public ManagedIdentityEventHubsTokenProvider(string managedIdentityTenantId)
        {
            _managedIdentityTenantId = managedIdentityTenantId;
        }

        public async Task<SecurityToken> GetTokenAsync(string appliesTo, TimeSpan timeout)
        {
            string accessToken = await GetAccessToken("https://eventhubs.azure.net/");
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
