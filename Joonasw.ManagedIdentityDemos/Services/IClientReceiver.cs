using System.Threading.Tasks;

namespace Joonasw.ManagedIdentityDemos.Services
{
    public interface IClientReceiver
    {
        Task ReceiveMessage(string message);
    }
}
