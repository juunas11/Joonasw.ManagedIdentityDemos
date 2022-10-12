using Azure;
using Azure.AI.TextAnalytics;
using Azure.Core;
using Azure.Data.AppConfiguration;
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
using Joonasw.ManagedIdentityDemos.Models.AzureMaps;
using Joonasw.ManagedIdentityDemos.Options;
using Microsoft.Azure.Cosmos;
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
        private readonly DemoSettings _settings;
        private readonly SecretClient _secretClient;
        private readonly MsiDbContext _dbContext;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly DataLakeServiceClient _dataLakeServiceClient;
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly CustomApiClient _customApiClient;
        private readonly CosmosClient _cosmosClient;
        private readonly TextAnalyticsClient _textAnalyticsClient;
        private readonly MapsApiClient _mapsApiClient;
        private readonly ConfigurationClient _configurationClient;

        public DemoService(
            IOptionsSnapshot<DemoSettings> settings,
            SecretClient secretClient,
            MsiDbContext dbContext,
            BlobServiceClient blobServiceClient,
            DataLakeServiceClient dataLakeServiceClient,
            EventHubProducerClient eventHubProducerClient,
            ServiceBusClient serviceBusClient,
            CustomApiClient customApiClient,
            CosmosClient cosmosClient,
            TextAnalyticsClient textAnalyticsClient,
            MapsApiClient mapsApiClient,
            ConfigurationClient configurationClient)
        {
            _settings = settings.Value;
            _secretClient = secretClient;
            _dbContext = dbContext;
            _blobServiceClient = blobServiceClient;
            _dataLakeServiceClient = dataLakeServiceClient;
            _eventHubProducerClient = eventHubProducerClient;
            _serviceBusClient = serviceBusClient;
            _customApiClient = customApiClient;
            _cosmosClient = cosmosClient;
            _textAnalyticsClient = textAnalyticsClient;
            _mapsApiClient = mapsApiClient;
            _configurationClient = configurationClient;
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
            Azure.Response<BlobDownloadInfo> response = await blobClient.DownloadAsync();

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

            await using (var conn = new SqlConnection(_settings.SqlConnectionString))
            {
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

        public async Task<CosmosDbViewModel> AccessCosmosDb()
        {
            Container container = _cosmosClient.GetContainer(_settings.CosmosDbDatabaseId, _settings.CosmosDbContainerId);
            FeedIterator<CosmosItemModel> iterator =
                container.GetItemQueryIterator<CosmosItemModel>("SELECT * FROM c");
            var result = new CosmosDbViewModel
            {
                Documents = new List<CosmosDocumentModel>()
            };

            while (iterator.HasMoreResults)
            {
                FeedResponse<CosmosItemModel> response = await iterator.ReadNextAsync();
                result.Documents.AddRange(response.Select(x => new CosmosDocumentModel
                {
                    Id = x.Id,
                    Value = x.Value
                }));
            }

            return result;
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

        public async Task<CognitiveServicesModel> AccessCognitiveServices(string input)
        {
            Azure.Response<DocumentSentiment> response =
                await _textAnalyticsClient.AnalyzeSentimentAsync(input);
            DocumentSentiment sentiment = response.Value;

            return new CognitiveServicesModel
            {
                Sentiment = sentiment.Sentiment.ToString(),
                ConfidenceScores = new Dictionary<string, double>
                {
                    [TextSentiment.Positive.ToString()] = sentiment.ConfidenceScores.Positive,
                    [TextSentiment.Negative.ToString()] = sentiment.ConfidenceScores.Negative,
                    [TextSentiment.Neutral.ToString()] = sentiment.ConfidenceScores.Neutral,
                }
            };
        }

        public async Task<AzureMapsViewModel> AccessAzureMaps(string input)
        {
            MapsPoiResults results = await _mapsApiClient.SearchPointsOfInterest(input);
            return new AzureMapsViewModel
            {
                Results = results
            };
        }

        public async Task<AppConfigViewModel> AccessAppConfig()
        {
            Azure.Response<ConfigurationSetting> response =
                await _configurationClient.GetConfigurationSettingAsync("Demo:AppConfigValue");

            return new AppConfigViewModel
            {
                ValueFromConfig = _settings.AppConfigValue,
                ValueFromAppConfigApi = response.Value.Value
            };
        }
    }
}
