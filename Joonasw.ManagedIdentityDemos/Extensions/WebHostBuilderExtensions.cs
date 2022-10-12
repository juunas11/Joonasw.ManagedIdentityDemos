using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace Joonasw.ManagedIdentityDemos.Extensions
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseAzureKeyVaultAndAppConfiguration(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureAppConfiguration(builder =>
            {
                IConfigurationRoot config = builder.Build();
                string keyVaultUrl = config["Demo:KeyVaultBaseUrl"];
                string appConfigUrl = config["Demo:AppConfigUrl"];
                string tenantId = config["Demo:ManagedIdentityTenantId"];
                if (string.IsNullOrEmpty(tenantId))
                {
                    tenantId = null;
                }

                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    SharedTokenCacheTenantId = tenantId,
                    VisualStudioCodeTenantId = tenantId,
                    VisualStudioTenantId = tenantId,
                });

                if (!string.IsNullOrEmpty(appConfigUrl))
                {
                    builder.AddAzureAppConfiguration(options =>
                    {
                        options.Connect(new Uri(appConfigUrl), credential);
                    });
                }

                if (!string.IsNullOrEmpty(keyVaultUrl))
                {
                    builder.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
                }
            });
        }
    }
}
