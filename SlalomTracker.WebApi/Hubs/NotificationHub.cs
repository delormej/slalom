using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SlalomTracker.WebApi.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendToAll(string name, string message)
        {
            await Clients.All.SendAsync("sendToAll", name, message);
        }
    }
}