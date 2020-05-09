using System;
using System.Text;
using System.Threading.Tasks;
using Logger = jasondel.Tools.Logger;
using Microsoft.Azure.ServiceBus;

namespace SlalomTracker.Cloud
{
    public class VideoProcessedNotifier
    {
        const string ENV_SERVICEBUS = "SKISB";
        const string DefaultQueueName = "video-processed";
        private IQueueClient _queueClient;        

        public VideoProcessedNotifier(string queueName = DefaultQueueName)
        {
            string serviceBusConnectionString = Environment.GetEnvironmentVariable(ENV_SERVICEBUS);
            if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
                throw new ApplicationException($"Missing service bus connection string in env variable: {ENV_SERVICEBUS}");

            _queueClient = new QueueClient(serviceBusConnectionString, queueName);            
            Logger.Log($"Connected to queue {queueName}");
        }

        public Task NotifyAsync(string skier, string video)
        {
            Logger.Log($"Notifying of {video}");
            string value = Newtonsoft.Json.JsonConvert.SerializeObject(
                new { Skier = skier, Video = video }
            );    

            byte[] bytes = Encoding.ASCII.GetBytes(value);
            return _queueClient.SendAsync(new Message(bytes));
        }
    }
}