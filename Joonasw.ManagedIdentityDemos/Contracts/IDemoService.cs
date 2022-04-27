using System.Threading.Tasks;
using Joonasw.ManagedIdentityDemos.Models;

namespace Joonasw.ManagedIdentityDemos.Contracts
{
    public interface IDemoService
    {
        Task<KeyVaultConfigViewModel> AccessKeyVault();
        Task<StorageViewModel> AccessStorage();
        Task<SqlDatabaseViewModel> AccessSqlDatabase();
        Task<CosmosDbViewModel> AccessCosmosDb();
        Task<CustomServiceViewModel> AccessCustomApi();
        Task SendServiceBusQueueMessage();
        Task SendEventHubsMessage();
        Task<DataLakeViewModel> AccessDataLake();
        Task<CognitiveServicesResultsViewModel> AccessCognitiveServices(string input);
    }
}
