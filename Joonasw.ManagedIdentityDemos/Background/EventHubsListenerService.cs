using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Joonasw.ManagedIdentityDemos.Options;
using Joonasw.ManagedIdentityDemos.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Joonasw.ManagedIdentityDemos.Background
{
    public class EventHubsListenerService : BackgroundService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IHubContext<EventHubMessageHub, IClientReceiver> _messageHub;
        private readonly EventProcessorClient _eventProcessorClient;

        public EventHubsListenerService(
            TelemetryClient telemetryClient,
            IHubContext<EventHubMessageHub, IClientReceiver> messageHub,
            BlobServiceClient blobServiceClient,
            TokenCredential tokenCredential,
            IOptions<DemoSettings> demoSettings)
        {
            _telemetryClient = telemetryClient;
            _messageHub = messageHub;
            _eventProcessorClient = new EventProcessorClient(
                blobServiceClient.GetBlobContainerClient(demoSettings.Value.EventHubStorageContainerName),
                "$Default",
                $"{demoSettings.Value.EventHubNamespace}.servicebus.windows.net",
                demoSettings.Value.EventHubName,
                tokenCredential);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _eventProcessorClient.ProcessEventAsync += ProcessEventAsync;
            _eventProcessorClient.ProcessErrorAsync += ProcessErrorAsync;

            await _eventProcessorClient.StartProcessingAsync(cancellationToken);

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
            }

            try
            {
                await _eventProcessorClient.StopProcessingAsync();
            }
            finally
            {
                _eventProcessorClient.ProcessEventAsync -= ProcessEventAsync;
                _eventProcessorClient.ProcessErrorAsync -= ProcessErrorAsync;
            }
        }

        public async Task ProcessEventAsync(ProcessEventArgs args)
        {
            string message = args.Data.EventBody.ToString();
            await _messageHub.Clients.All.ReceiveMessage(message);
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _telemetryClient.TrackException(args.Exception);
            return Task.CompletedTask;
        }
    }
}
