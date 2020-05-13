using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Hosting;

namespace SlalomTracker.WebApi.Services
{
    /// <summary>
    /// Listens to a queue which receives messages when videos are processed and then
    /// notifies and listening SignalR clients.
    /// </summary>
    public class VideoProcessedNotificationService : IHostedService
    {
        const string QueueName = "video-processed";
        const string HubName = "notification";
        const string ENV_SIGNALR = "SKISIGNALR";
        const string ENV_SERVICEBUS = "SKISB";
        private IServiceHubContext _notificationHub;
        private readonly ILogger<VideoProcessedNotificationService> _logger;
        private readonly IConfiguration _config;
        private IQueueClient _queueClient;   

        public VideoProcessedNotificationService(ILogger<VideoProcessedNotificationService> logger, IConfiguration config)
        {
            _config = config;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var serviceManager = new ServiceManagerBuilder().WithOptions(option =>
            {
                option.ConnectionString = _config[ENV_SIGNALR];
            }).Build();

            _notificationHub = await serviceManager.CreateHubContextAsync(HubName);            
            ConnectToQueue();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void ConnectToQueue()
        {
            _queueClient = new QueueClient(_config[ENV_SERVICEBUS], QueueName);
            
            // Register the function that processes messages.
            _queueClient.RegisterMessageHandler(ProcessMessagesAsync, (e) => 
            {
                _logger.LogError($"Error processing notification message: {e.Exception.Message}");
                return Task.CompletedTask;
            });
            _logger.LogInformation($"Connected to queue {QueueName}.");
        }

         async Task ProcessMessagesAsync(Message message, CancellationToken token)       
         {
             string json = Encoding.UTF8.GetString(message.Body);
             await _notificationHub.Clients.All.SendAsync("sendToAll", "videoJson", json);
         }
    }
}