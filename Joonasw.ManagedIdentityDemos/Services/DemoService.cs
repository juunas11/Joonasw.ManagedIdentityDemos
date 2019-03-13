using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;

namespace Joonasw.ManagedIdentityDemos.Services
{
    public class DemoService : IDemoService
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly DemoSettings _settings;
        private readonly MsiDbContext _dbContext;

        public DemoService(IOptionsSnapshot<DemoSettings> settings, MsiDbContext dbContext)
        {
            _settings = settings.Value;
            _dbContext = dbContext;
        }

        public async Task<StorageViewModel> AccessStorage()
        {
            string accessToken = await GetAccessToken("https://storage.azure.com/");

            var tokenCredential = new TokenCredential(accessToken);
            var storageCredentials = new StorageCredentials(tokenCredential);
            var uri = new Uri($"https://{_settings.StorageAccountName}.blob.core.windows.net/{_settings.StorageContainerName}/{_settings.StorageBlobName}");
            var blob = new CloudBlockBlob(uri, storageCredentials);
            // We download the whole file here because we are going to show it on the Razor view
            // Usually when reading files from Storage you should use the Stream APIs, e.g. blob.OpenReadAsync()
            string content = await blob.DownloadTextAsync();

            return new StorageViewModel
            {
                FileContent = content
            };
        }

        public async Task<SqlDatabaseViewModel> AccessSqlDatabase()
        {
            string accessToken = await GetAccessToken("https://database.windows.net/");

            //List<SqlRowModel> results = await GetSqlRowsWithAdoNet(accessToken);
            List<SqlRowModel> results = await GetSqlRowsWithEfCore(accessToken);

            return new SqlDatabaseViewModel
            {
                Results = results
            };
        }

        private async Task<List<SqlRowModel>> GetSqlRowsWithEfCore(string accessToken)
        {
            //Have to cast DbConnection to SqlConnection
            //AccessToken property does not exist on the base class
            var conn = (SqlConnection)_dbContext.Database.GetDbConnection();
            conn.AccessToken = accessToken;

            return await _dbContext
                .Tests
                .Select(t => new SqlRowModel
                {
                    Id = t.Id,
                    Value = t.Value
                })
                .ToListAsync();
        }

        private async Task<List<SqlRowModel>> GetSqlRowsWithAdoNet(string accessToken)
        {
            var results = new List<SqlRowModel>();

            using (var conn = new SqlConnection(_settings.SqlConnectionString))
            {
                conn.AccessToken = accessToken;

                await conn.OpenAsync();

                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT [Id], [Value] FROM [dbo].[Test]";
                var reader = await cmd.ExecuteReaderAsync();

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
            string accessToken = await GetAccessToken(_settings.CustomApiApplicationIdUri);
            var req = new HttpRequestMessage(HttpMethod.Get, $"{_settings.CustomApiBaseUrl}/api/test");
            req.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var res = await HttpClient.SendAsync(req);
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

        private async Task<string> GetAccessToken(string resource)
        {
            var authProvider = new AzureServiceTokenProvider();
            string tenantId = _settings.ManagedIdentityTenantId;

            if (tenantId != null && tenantId.Length == 0)
            {
                tenantId = null; //We want to clearly indicate to the provider if we do not specify a tenant, so no empty strings
            }

            return await authProvider.GetAccessTokenAsync(resource, tenantId);
        }
    }
}
