using Joonasw.ManagedIdentityDemos.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Joonasw.ManagedIdentityDemos.Background
{
    public class EventHubProcessor : IEventProcessor
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IHubContext<EventHubMessageHub> _messageHub;

        public EventHubProcessor(TelemetryClient telemetryClient, IHubContext<EventHubMessageHub> messageHub)
        {
            _telemetryClient = telemetryClient;
            _messageHub = messageHub;
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {
                var data = Encoding.UTF8.GetString(
                    eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                await _messageHub.Clients.All.SendAsync("ReceiveMessage", data);
            }

            await context.CheckpointAsync();
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            _telemetryClient.TrackException(error);
            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            return Task.CompletedTask;
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            return Task.CompletedTask;
        }
    }
}
