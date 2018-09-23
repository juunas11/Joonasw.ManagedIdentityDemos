using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace Joonasw.ManagedIdentityDemos.Extensions
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseAzureKeyVaultConfiguration(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureAppConfiguration(builder =>
            {
                var config = builder.Build();
                string keyVaultUrl = config["Demo:KeyVaultBaseUrl"];

                if (!string.IsNullOrEmpty(keyVaultUrl))
                {
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var kvClient = new KeyVaultClient(
                        (authority, resource, scope) => azureServiceTokenProvider.KeyVaultTokenCallback(authority, resource, scope));
                    builder.AddAzureKeyVault(keyVaultUrl, kvClient, new DefaultKeyVaultSecretManager());
                }
            });
        }
    }
}
