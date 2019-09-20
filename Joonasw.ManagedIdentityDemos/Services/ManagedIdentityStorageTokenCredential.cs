using Azure.Core;
using Microsoft.Azure.Services.AppAuthentication;
using System.Threading;
using System.Threading.Tasks;

namespace Joonasw.ManagedIdentityDemos.Services
{
    public class ManagedIdentityStorageTokenCredential : TokenCredential
    {
        private const string Resource = "https://storage.azure.com/";
        private readonly string _managedIdentityTenantId;

        public ManagedIdentityStorageTokenCredential(string managedIdentityTenantId)
        {
            _managedIdentityTenantId = managedIdentityTenantId;
        }

        public override AccessToken GetToken(string[] scopes, CancellationToken cancellationToken)
        {
            return GetTokenAsync(scopes, cancellationToken).GetAwaiter().GetResult();
        }

        public override async Task<AccessToken> GetTokenAsync(string[] scopes, CancellationToken cancellationToken)
        {
            var authProvider = new AzureServiceTokenProvider();
            string tenantId = _managedIdentityTenantId;

            if (tenantId != null && tenantId.Length == 0)
            {
                tenantId = null; //We want to clearly indicate to the provider if we do not specify a tenant, so no empty strings
            }

            AppAuthenticationResult result = await authProvider.GetAuthenticationResultAsync(Resource, tenantId);
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }
    }
}
