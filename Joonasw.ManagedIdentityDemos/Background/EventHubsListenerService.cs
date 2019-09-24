using Joonasw.ManagedIdentityDemos.Options;
using Joonasw.ManagedIdentityDemos.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.Storage;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Joonasw.ManagedIdentityDemos.Background
{
    public class EventHubsListenerService : HostedService, IEventProcessorFactory
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IHubContext<EventHubMessageHub, IClientReceiver> _messageHub;
        private readonly DemoSettings _demoSettings;

        public EventHubsListenerService(
            TelemetryClient telemetryClient,
            IHubContext<EventHubMessageHub, IClientReceiver> messageHub,
            IOptions<DemoSettings> demoSettings)
        {
            _telemetryClient = telemetryClient;
            _messageHub = messageHub;
            _demoSettings = demoSettings.Value;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new EventHubProcessor(_telemetryClient, _messageHub);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            string hubNamespace = _demoSettings.EventHubNamespace;
            var endpoint = new Uri($"sb://{hubNamespace}.servicebus.windows.net/");
            string hubName = _demoSettings.EventHubName;
            var storageAccount = CloudStorageAccount.Parse(_demoSettings.EventHubStorageConnectionString);

            var host = new EventProcessorHost(
                endpoint,
                hubName,
                "$Default",
                new ManagedIdentityEventHubsTokenProvider(_demoSettings.ManagedIdentityTenantId),
                storageAccount,
                _demoSettings.EventHubStorageContainerName);
            try
            {
                await host.RegisterEventProcessorFactoryAsync(this);

                await Task.Delay(-1, cancellationToken);
            }
            finally
            {
                await host.UnregisterEventProcessorAsync();
            }
        }
    }
}
