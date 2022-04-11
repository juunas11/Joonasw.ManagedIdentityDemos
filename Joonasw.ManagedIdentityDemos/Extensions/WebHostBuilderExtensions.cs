using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace Joonasw.ManagedIdentityDemos.Extensions
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseAzureKeyVaultConfiguration(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureAppConfiguration(builder =>
            {
                IConfigurationRoot config = builder.Build();
                string keyVaultUrl = config["Demo:KeyVaultBaseUrl"];
                string tenantId = config["Demo:ManagedIdentityTenantId"];
                if (string.IsNullOrEmpty(tenantId))
                {
                    tenantId = null;
                }

                if (!string.IsNullOrEmpty(keyVaultUrl))
                {
                    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        SharedTokenCacheTenantId = tenantId,
                        VisualStudioCodeTenantId = tenantId,
                        VisualStudioTenantId = tenantId,
                    });
                    builder.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
                }
            });
        }
    }
}
