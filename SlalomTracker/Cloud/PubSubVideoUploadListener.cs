using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Google.Api.Gax;
using SlalomTracker.Cloud;
using SlalomTracker.Video;
using Logger = jasondel.Tools.Logger;

namespace SkiConsole
{
    public class PubSubVideoUploadListener : IUploadListener
    {
        public event EventHandler Completed;
        
        static string _projectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");

        SubscriberClient _subscriber;
        SubscriptionName _subscriptionName;
        Task _processor;
        
        public PubSubVideoUploadListener(string subscriptionId, bool readDeadLetter = false)
        {
            if (string.IsNullOrEmpty(_projectId))
                throw new ApplicationException("GOOGLE_PROJECT_ID env variable must be set.");

            _subscriptionName = SubscriptionName.FromProjectSubscription(_projectId, subscriptionId);

            Task<SubscriberClient> task = SubscriberClient.CreateAsync(_subscriptionName,
                settings: new SubscriberClient.Settings()
                {
                    // AckExtensionWindow = TimeSpan.FromSeconds(4),
                    // AckDeadline = TimeSpan.FromSeconds(10),
                    FlowControlSettings = new FlowControlSettings(maxOutstandingElementCount: 1, maxOutstandingByteCount: null)
                });
            task.Wait();
            _subscriber = task.Result;
        }

        public void Start()
        {
            _processor = _subscriber.StartAsync(ProcessMessageAsync);
        }

        public void Stop()
        {
            _subscriber.StopAsync(CancellationToken.None).Wait();
            _processor?.Wait(500);
            Logger.Log("Stopped listening for events.");
        }

        private async Task<SubscriberClient.Reply> ProcessMessageAsync(PubsubMessage message, CancellationToken cancel)
        {
            try
            {               
                // Process the message.
                string json = Encoding.UTF8.GetString(message.Data.ToArray());
                Logger.Log($"Received message id:{message.MessageId} Body:{json}");

                IProcessor processor = QueueMessageParser.GetProcessor(json);               
                await processor.ProcessAsync();
                
                Logger.Log($"Returning Ack for message id:{message.MessageId}");

                return SubscriberClient.Reply.Ack;
            }
            catch (Exception e)
            {
                int? attempt = message?.GetDeliveryAttempt().Value ?? 0;
                Logger.Log($"ERROR: Attempt #{attempt} for message.", e);

                return SubscriberClient.Reply.Nack;
            }
            finally
            {
                Logger.Log($"Message handler completed.");

                if (Completed != null)
                    Completed(this, null);                       
            }
        }
    }
}