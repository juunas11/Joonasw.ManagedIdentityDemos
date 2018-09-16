namespace Joonasw.ManagedIdentityDemos.Options
{
    public class DemoSettings
    {
        public string ManagedIdentityTenantId { get; set; }

        public string StorageAccountName { get; set; }
        public string StorageContainerName { get; set; }
        public string StorageBlobName { get; set; }

        public string SqlConnectionString { get; set; }

        public string KeyVaultSecret { get; set; }

        public string CustomApiBaseUrl { get; set; }
        public string CustomApiApplicationIdUri { get; set; }

        public string ServiceBusNamespace { get; set; }
        public string ServiceBusQueueName { get; set; }
    }
}
