using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;

namespace SlalomTracker.WebApi.Hubs
{
    //TODO! Rename this to something like VideoProcessedHub
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly IConfiguration _config;

        public NotificationHub(ILogger<NotificationHub> logger, IConfiguration config)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendToAll(string name, string message)
        {
            await Clients.All.SendAsync("sendToAll", name, message);
        }
    }
}