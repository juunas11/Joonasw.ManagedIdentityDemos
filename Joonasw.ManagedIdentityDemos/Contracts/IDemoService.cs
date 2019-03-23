using System.Threading.Tasks;
using Joonasw.ManagedIdentityDemos.Models;

namespace Joonasw.ManagedIdentityDemos.Contracts
{
    public interface IDemoService
    {
        Task<StorageViewModel> AccessStorage();
        Task<SqlDatabaseViewModel> AccessSqlDatabase();
        Task<CustomServiceViewModel> AccessCustomApi();
        Task SendServiceBusQueueMessage();
        Task SendEventHubsMessage();
        Task<DataLakeViewModel> AccessDataLake();
    }
}
