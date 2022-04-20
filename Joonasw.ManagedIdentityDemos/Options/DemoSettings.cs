using System.Runtime.Serialization;

namespace Joonasw.ManagedIdentityDemos.Options
{
    public class DemoSettings
    {
        public string ManagedIdentityTenantId { get; set; }

        public string StorageAccountName { get; set; }
        public string StorageContainerName { get; set; }
        public string StorageBlobName { get; set; }

        public string SqlConnectionString { get; set; }

        public string KeyVaultBaseUrl { get; set; }
        public string KeyVaultSecret { get; set; }

        public string CustomApiBaseUrl { get; set; }
        public string CustomApiApplicationIdUri { get; set; }
        public string CustomApiTenantId { get; set; }
        public string CustomApiClientId { get; set; }
        public string CustomApiClientSecret { get; set; }

        public string ServiceBusNamespace { get; set; }
        public string ServiceBusQueueName { get; set; }

        public string EventHubNamespace { get; set; }
        public string EventHubName { get; set; }
        public string EventHubStorageConnectionString { get; set; }
        public string EventHubStorageContainerName { get; set; }

        public string DataLakeStoreName { get; set; }
        public string DataLakeFileName { get; set; }
    }
}
