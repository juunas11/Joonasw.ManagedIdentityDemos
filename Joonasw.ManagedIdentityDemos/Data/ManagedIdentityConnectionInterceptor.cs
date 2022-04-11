using Azure.Core;
using Azure.Identity;
using Joonasw.ManagedIdentityDemos.Options;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Joonasw.ManagedIdentityDemos.Data
{
    public class ManagedIdentityConnectionInterceptor : DbConnectionInterceptor
    {
        private static readonly string[] Scopes = new[] { "https://database.windows.net/" };
        private readonly string _tenantId;
        private readonly TokenCredential _tokenCredential;
        private AccessToken _cachedToken;

        public ManagedIdentityConnectionInterceptor(DemoSettings options)
        {
            _tenantId = options.ManagedIdentityTenantId;
            if (string.IsNullOrEmpty(_tenantId))
            {
                _tenantId = null;
            }

            _tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                SharedTokenCacheTenantId = _tenantId,
                VisualStudioCodeTenantId = _tenantId,
                VisualStudioTenantId = _tenantId,
            });
        }

        public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            // Have to cast DbConnection to SqlConnection
            // AccessToken property does not exist on the base class
            var sqlConnection = (SqlConnection)connection;
            string accessToken = await GetAccessTokenAsync(cancellationToken);
            sqlConnection.AccessToken = accessToken;

            return result;
        }

        public override InterceptionResult ConnectionOpening(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result)
        {
            // Have to cast DbConnection to SqlConnection
            // AccessToken property does not exist on the base class
            var sqlConnection = (SqlConnection)connection;
            string accessToken = GetAccessToken();
            sqlConnection.AccessToken = accessToken;

            return result;
        }

        private async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            if (CachedTokenIsValid())
            {
                return _cachedToken.Token;
            }

            _cachedToken = await _tokenCredential.GetTokenAsync(new TokenRequestContext(Scopes), cancellationToken);
            return _cachedToken.Token;
        }

        private string GetAccessToken()
        {
            if (CachedTokenIsValid())
            {
                return _cachedToken.Token;
            }

            _cachedToken = _tokenCredential.GetToken(new TokenRequestContext(Scopes), default);
            return _cachedToken.Token;
        }

        private bool CachedTokenIsValid()
        {
            // Refresh only when there is 4 minutes or less remaining
            // Managed Identity endpoint itself caches tokens
            // until 5 minutes before expiry.
            // Trying to request a new one before that would only result
            // in getting the same token again.
            return _cachedToken.ExpiresOn > DateTime.UtcNow.AddMinutes(4);
        }
    }
}
