using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Joonasw.ManagedIdentityDemos.Options;
using Joonasw.ManagedIdentityDemos.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;

namespace Joonasw.ManagedIdentityDemos.Background
{
    public class QueueListenerService : HostedService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IHubContext<QueueMessageHub> _messageHub;
        private readonly DemoSettings _settings;

        public QueueListenerService(
            TelemetryClient telemetryClient,
            IHubContext<QueueMessageHub> messageHub,
            IOptions<DemoSettings> demoSettings)
        {
            _telemetryClient = telemetryClient;
            _messageHub = messageHub;
            _settings = demoSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            string endpoint = _settings.ServiceBusNamespace + ".servicebus.windows.net";
            string queueName = _settings.ServiceBusQueueName;
            // We could use ManagedServiceIdentityTokenProvider here
            // But it failed for me with an assembly not found error relating to the AppServices Authentication library
            var tokenProvider = new ManagedIdentityServiceBusTokenProvider(_settings.ManagedIdentityTenantId);
            var queueClient = new QueueClient(endpoint, queueName, tokenProvider);

            var messageHandlerOptions = new MessageHandlerOptions(HandleException)
            {
                AutoComplete = true
            };
            queueClient.RegisterMessageHandler(HandleMessage, messageHandlerOptions);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(60_000, cancellationToken);
            }

            await queueClient.CloseAsync();
        }

        private async Task HandleMessage(Message msg, CancellationToken ct)
        {
            string message = Encoding.UTF8.GetString(msg.Body);
            await _messageHub.Clients.All.SendAsync("ReceiveMessage", message, ct);
        }

        private Task HandleException(ExceptionReceivedEventArgs errArgs)
        {
            _telemetryClient.TrackException(errArgs.Exception);
            return Task.CompletedTask;
        }
    }
}
