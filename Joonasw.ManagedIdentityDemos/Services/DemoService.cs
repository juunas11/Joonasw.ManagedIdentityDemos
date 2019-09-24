using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Joonasw.ManagedIdentityDemos.Contracts;
using Joonasw.ManagedIdentityDemos.Data;
using Joonasw.ManagedIdentityDemos.Models;
using Joonasw.ManagedIdentityDemos.Options;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.DataLake.Store;
using System.IO;
using Azure.Storage.Blobs;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.Data.SqlClient;

namespace Joonasw.ManagedIdentityDemos.Services
{
    public class DemoService : IDemoService
    {
        private readonly DemoSettings _settings;
        private readonly MsiDbContext _dbContext;
        private readonly HttpClient _httpClient;

        public DemoService(
            IOptionsSnapshot<DemoSettings> settings,
            MsiDbContext dbContext,
            IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _dbContext = dbContext;
            _httpClient = httpClientFactory.CreateClient(HttpClients.CustomApi);
        }

        public async Task<StorageViewModel> AccessStorage()
        {
            var serviceClient = new BlobServiceClient(
                new Uri($"https://{_settings.StorageAccountName}.blob.core.windows.net"),
                new ManagedIdentityStorageTokenCredential(_settings.ManagedIdentityTenantId));
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(_settings.StorageContainerName);
            BlobClient blobClient = containerClient.GetBlobClient(_settings.StorageBlobName);
            Response<BlobDownloadInfo> response = await blobClient.DownloadAsync();

            using (var reader = new StreamReader(response.Value.Content))
            {
                // We download the whole file here because we are going to show it on the Razor view
                // Usually when reading files from Storage you should return the file via the Stream
                var content = await reader.ReadToEndAsync();
                return new StorageViewModel
                {
                    FileContent = content
                };
            }
        }

        public async Task<SqlDatabaseViewModel> AccessSqlDatabase()
        {
            List<SqlRowModel> adoNetResults = await GetSqlRowsWithAdoNet();
            List<SqlRowModel> efResults = await GetSqlRowsWithEfCore();

            return new SqlDatabaseViewModel
            {
                AdoNetResults = adoNetResults,
                EfResults = efResults
            };
        }

        private async Task<List<SqlRowModel>> GetSqlRowsWithEfCore()
        {
            // Data / ManagedIdentityConnectionInterceptor sets up the token for the connection
            // So no need to acquire token here
            return await _dbContext
                .Tests
                .Select(t => new SqlRowModel
                {
                    Id = t.Id,
                    Value = t.Value
                })
                .ToListAsync();
        }

        private async Task<List<SqlRowModel>> GetSqlRowsWithAdoNet()
        {
            var results = new List<SqlRowModel>();

            using (var conn = new SqlConnection(_settings.SqlConnectionString))
            {
                string accessToken = await GetAccessToken("https://database.windows.net/");
                conn.AccessToken = accessToken;

                await conn.OpenAsync();

                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT [Id], [Value] FROM [dbo].[Test]";
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        int id = (int)reader["Id"];
                        string value = (string)reader["Value"];

                        results.Add(new SqlRowModel
                        {
                            Id = id,
                            Value = value
                        });
                    }
                }

                reader.Close();
            }

            return results;
        }

        public async Task<CustomServiceViewModel> AccessCustomApi()
        {
            // This will not work in local environment
            // Local gets token as a user
            // But we need to use app permissions, i.e. acquire token as application only
            string accessToken = await GetAccessToken(_settings.CustomApiApplicationIdUri, _settings.CustomApiTokenProviderConnectionString);
            var req = new HttpRequestMessage(HttpMethod.Get, $"{_settings.CustomApiBaseUrl}/api/test");
            req.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage res = await _httpClient.SendAsync(req);
            string resJson = await res.Content.ReadAsStringAsync();

            return new CustomServiceViewModel
            {
                Claims = JsonConvert.DeserializeObject<Dictionary<string, string>>(resJson)
            };
        }

        public async Task SendServiceBusQueueMessage()
        {
            string endpoint = _settings.ServiceBusNamespace + ".servicebus.windows.net";
            string queueName = _settings.ServiceBusQueueName;
            // We could use ManagedServiceIdentityTokenProvider here
            // But it failed for me with an assembly not found error relating to the AppServices Authentication library
            var tokenProvider = new ManagedIdentityServiceBusTokenProvider(_settings.ManagedIdentityTenantId);
            var queueClient = new QueueClient(endpoint, queueName, tokenProvider);

            var message = new Message(
                Encoding.UTF8.GetBytes($"Test message {Guid.NewGuid()} ({DateTime.UtcNow:HH:mm:ss})"));
            await queueClient.SendAsync(message);
        }

        public async Task SendEventHubsMessage()
        {
            string hubNamespace = _settings.EventHubNamespace;
            var endpoint = new Uri($"sb://{hubNamespace}.servicebus.windows.net/");
            string hubName = _settings.EventHubName;
            var client = EventHubClient.CreateWithTokenProvider(
                endpoint,
                hubName,
                new ManagedIdentityEventHubsTokenProvider(_settings.ManagedIdentityTenantId));
            // You can also do this (but then you can't specify the tenant used):
            // var client = EventHubClient.CreateWithManagedIdentity(endpoint, hubName);
            byte[] bytes = Encoding.UTF8.GetBytes($"Test message {Guid.NewGuid()} ({DateTime.UtcNow:HH:mm:ss})");
            await client.SendAsync(new EventData(bytes));
        }

        public async Task<DataLakeViewModel> AccessDataLake()
        {
            string token = await GetAccessToken("https://datalake.azure.net/");
            string accountFqdn = $"{_settings.DataLakeStoreName}.azuredatalakestore.net";
            var client = AdlsClient.CreateClient(accountFqdn, $"Bearer {token}");

            string filename = _settings.DataLakeFileName;
            string content = null;
            using (var reader = new StreamReader(await client.GetReadStreamAsync(filename)))
            {
                content = await reader.ReadToEndAsync();
            }

            return new DataLakeViewModel
            {
                FileContent = content
            };
        }

        private async Task<string> GetAccessToken(string resource, string tokenProviderConnectionString = null)
        {
            var authProvider = new AzureServiceTokenProvider(tokenProviderConnectionString);
            string tenantId = _settings.ManagedIdentityTenantId;

            if (tenantId != null && tenantId.Length == 0)
            {
                tenantId = null; //We want to clearly indicate to the provider if we do not specify a tenant, so no empty strings
            }

            return await authProvider.GetAccessTokenAsync(resource, tenantId);
        }
    }
}
