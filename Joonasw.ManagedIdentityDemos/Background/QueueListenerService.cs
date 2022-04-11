using Azure.Messaging.ServiceBus;
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
    public class QueueListenerService : BackgroundService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IHubContext<QueueMessageHub, IClientReceiver> _messageHub;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly DemoSettings _settings;

        public QueueListenerService(
            TelemetryClient telemetryClient,
            IHubContext<QueueMessageHub, IClientReceiver> messageHub,
            ServiceBusClient serviceBusClient,
            IOptions<DemoSettings> demoSettings)
        {
            _telemetryClient = telemetryClient;
            _messageHub = messageHub;
            _serviceBusClient = serviceBusClient;
            _settings = demoSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await using ServiceBusProcessor processor = _serviceBusClient.CreateProcessor(_settings.ServiceBusQueueName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = true,
            });

            processor.ProcessMessageAsync += ProcessMessageAsync;
            processor.ProcessErrorAsync += ProcessErrorAsync;

            await processor.StartProcessingAsync(cancellationToken);

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
            }

            try
            {
                await processor.StopProcessingAsync();
            }
            finally
            {
                processor.ProcessMessageAsync -= ProcessMessageAsync;
                processor.ProcessErrorAsync -= ProcessErrorAsync;
            }
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            string message = args.Message.Body.ToString();
            await _messageHub.Clients.All.ReceiveMessage(message);
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _telemetryClient.TrackException(args.Exception);
            return Task.CompletedTask;
        }
    }
}
