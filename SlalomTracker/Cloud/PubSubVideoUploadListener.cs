using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using SlalomTracker.Cloud;
using SlalomTracker.Video;
using Logger = jasondel.Tools.Logger;

namespace SkiConsole
{
    public class PubSubVideoUploadListener : IUploadListener
    {
        public event EventHandler Completed;
        
        static string _projectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");

        SubscriberServiceApiClient _subscriber;
        SubscriptionName _subscriptionName;
        CancellationTokenSource _cancel;
        
        public PubSubVideoUploadListener(string subscriptionId, bool readDeadLetter = false)
        {
            if (string.IsNullOrEmpty(_projectId))
                throw new ApplicationException("GOOGLE_PROJECT_ID env variable must be set.");

            _cancel = new CancellationTokenSource();
            _subscriptionName = SubscriptionName.FromProjectSubscription(_projectId, subscriptionId);
            _subscriber = SubscriberServiceApiClient.Create();
        }

        public void Start()
        {
            Task.Run( () => ProcessMessageAsync(), _cancel.Token );
        }

        public void Stop()
        {
            _cancel.Cancel();
        }

        private async Task ProcessMessageAsync()
        {
            PubsubMessage message = null;

            try
            {
                PullResponse response = null;
                do {
                    Logger.Log($"Attempting to pull 1 message for subscription: {_subscriptionName}");
                    response = _subscriber.Pull(_subscriptionName, returnImmediately: false, maxMessages: 1);
                } while (response.ReceivedMessages.Count == 0);
                
                var received = response.ReceivedMessages.FirstOrDefault();                
                message = received.Message;
                
                // Process the message.
                string json = Encoding.UTF8.GetString(message.Data.ToArray());
                Logger.Log($"Received message id:{message.MessageId} Body:{json}");

                IProcessor processor = QueueMessageParser.GetProcessor(json);               
                await processor.ProcessAsync();
                
                _subscriber.Acknowledge(_subscriptionName, new string[] { received.AckId });
            }
            catch (Exception e)
            {
                int? attempt = message?.GetDeliveryAttempt().Value ?? 0;
                Logger.Log($"ERROR: Attempt #{attempt} for message.", e);
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