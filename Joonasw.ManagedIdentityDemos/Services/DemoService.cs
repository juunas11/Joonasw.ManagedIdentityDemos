using Azure;
using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.DataLake;
using Joonasw.ManagedIdentityDemos.Contracts;
using Joonasw.ManagedIdentityDemos.Data;
using Joonasw.ManagedIdentityDemos.Models;
using Joonasw.ManagedIdentityDemos.Options;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Joonasw.ManagedIdentityDemos.Services
{
    public class DemoService : IDemoService
    {
        private static AccessToken CachedAdoNetToken;
        private readonly DemoSettings _settings;
        private readonly SecretClient _secretClient;
        private readonly MsiDbContext _dbContext;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly DataLakeServiceClient _dataLakeServiceClient;
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly TokenCredential _tokenCredential;
        private readonly CustomApiClient _customApiClient;

        public DemoService(
            IOptionsSnapshot<DemoSettings> settings,
            SecretClient secretClient,
            MsiDbContext dbContext,
            BlobServiceClient blobServiceClient,
            DataLakeServiceClient dataLakeServiceClient,
            EventHubProducerClient eventHubProducerClient,
            ServiceBusClient serviceBusClient,
            TokenCredential tokenCredential,
            CustomApiClient customApiClient)
        {
            _settings = settings.Value;
            _secretClient = secretClient;
            _dbContext = dbContext;
            _blobServiceClient = blobServiceClient;
            _dataLakeServiceClient = dataLakeServiceClient;
            _eventHubProducerClient = eventHubProducerClient;
            _serviceBusClient = serviceBusClient;
            _tokenCredential = tokenCredential;
            _customApiClient = customApiClient;
        }

        public async Task<KeyVaultConfigViewModel> AccessKeyVault()
        {
            var secret = await _secretClient.GetSecretAsync("Demo--KeyVaultSecret");

            return new KeyVaultConfigViewModel
            {
                SecretValueFromConfig = _settings.KeyVaultSecret,
                SecretValueFromVault = secret.Value.Value,
            };
        }

        public async Task<StorageViewModel> AccessStorage()
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_settings.StorageContainerName);
            BlobClient blobClient = containerClient.GetBlobClient(_settings.StorageBlobName);
            Response<BlobDownloadInfo> response = await blobClient.DownloadAsync();

            using (var reader = new StreamReader(response.Value.Content))
            {
                // We download the whole file here because we are going to show it on the Razor view
                // Usually when reading files from Storage you should return the file via a Stream
                string content = await reader.ReadToEndAsync();
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

            AccessToken accessToken;
            if (CachedAdoNetToken.ExpiresOn > DateTime.UtcNow.AddMinutes(4))
            {
                accessToken = CachedAdoNetToken;
            }
            else
            {
                accessToken = await _tokenCredential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://database.windows.net/" }),
                    default);
                CachedAdoNetToken = accessToken;
            }

            await using (var conn = new SqlConnection(_settings.SqlConnectionString))
            {
                conn.AccessToken = accessToken.Token;

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

                await reader.CloseAsync();
            }

            return results;
        }

        public async Task<CustomServiceViewModel> AccessCustomApi()
        {
            var response = await _customApiClient.Request();
            return new CustomServiceViewModel
            {
                Claims = response
            };
        }

        public async Task SendServiceBusQueueMessage()
        {
            ServiceBusSender sender = _serviceBusClient.CreateSender(_settings.ServiceBusQueueName);

            await sender.SendMessageAsync(new ServiceBusMessage($"Test message {Guid.NewGuid()} ({DateTime.UtcNow:HH:mm:ss})"));
        }

        public async Task SendEventHubsMessage()
        {
            using EventDataBatch batch = await _eventHubProducerClient.CreateBatchAsync();
            batch.TryAdd(new EventData($"Test message {Guid.NewGuid()} ({DateTime.UtcNow:HH:mm:ss})"));

            await _eventHubProducerClient.SendAsync(batch);
        }

        public async Task<DataLakeViewModel> AccessDataLake()
        {
            var x = _dataLakeServiceClient.GetFileSystemClient("test");
            var y = x.GetDirectoryClient("test");
            var z = y.GetFileClient("test.txt");
            var stream = await z.OpenReadAsync();
            using (var reader = new StreamReader(stream))
            {
                // We download the whole file here because we are going to show it on the Razor view
                // Usually when reading files from Storage you should return the file via a Stream
                string content = await reader.ReadToEndAsync();
                return new DataLakeViewModel
                {
                    FileContent = content
                };
            }

        }
    }
}
