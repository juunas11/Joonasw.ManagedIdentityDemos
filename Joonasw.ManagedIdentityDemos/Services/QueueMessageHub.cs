using Microsoft.AspNetCore.SignalR;

namespace Joonasw.ManagedIdentityDemos.Services
{
    public class QueueMessageHub : Hub<IClientReceiver>
    {
    }
}
