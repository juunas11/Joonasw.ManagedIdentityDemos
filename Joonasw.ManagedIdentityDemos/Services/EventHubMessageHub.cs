using Microsoft.AspNetCore.SignalR;

namespace Joonasw.ManagedIdentityDemos.Services
{
    public class EventHubMessageHub : Hub<IClientReceiver>
    {
    }
}
