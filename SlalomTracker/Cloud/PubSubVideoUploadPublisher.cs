using System;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Logger = jasondel.Tools.Logger;

namespace SkiConsole
{
    public class PubSubVideoUploadPublisher
    {       
        static string _projectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
        TopicName _topicName;
        
        public PubSubVideoUploadPublisher(string topicName)
        {
            _topicName = new TopicName(_projectId, topicName);
        }

        public async Task PublishAsync(string videoUrl)
        {
            PublisherClient publisher = await PublisherClient.CreateAsync(_topicName);
            // PublishAsync() has various overloads. Here we're using the string overload.
            string message = "{Url: \"" + videoUrl + "\"}";
            string messageId = await publisher.PublishAsync(message);
            Logger.Log($"Republished {videoUrl}");
        }
    }
}