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

        CancellationTokenSource _cancel;
        SubscriberServiceApiClient _subscriber;
        SubscriptionName _subscriptionName;
        Task _processor;
        
        public PubSubVideoUploadListener(string subscriptionId, bool readDeadLetter = false)
        {
            if (string.IsNullOrEmpty(_projectId))
                throw new ApplicationException("GOOGLE_PROJECT_ID env variable must be set.");

            _subscriptionName = SubscriptionName.FromProjectSubscription(_projectId, subscriptionId);
            _subscriber = SubscriberServiceApiClient.Create();
            _cancel = new CancellationTokenSource();
        }

        public void Start()
        {
            _processor = Task.Run( async () => {
                try
                {
                    ReceivedMessage received = null;
                    while (received == null)
                        received = PullMessage();

                    PubsubMessage message = received.Message;
                    int? attempt = message.GetDeliveryAttempt() ?? 0;

                    if (await ProcessMessageAsync(message) == SubscriberClient.Reply.Ack)
                        _subscriber.Acknowledge(_subscriptionName, new string[] {received.AckId});
                }
                catch (Exception e)
                {
                    Logger.Log("Error pulling message.", e);
                }
            }, _cancel.Token);
        }

        public void Stop()
        {
            Logger.Log("Stopping...");
            _cancel.Cancel();
        }

        private ReceivedMessage PullMessage()
        {
            PullResponse response = _subscriber.Pull(_subscriptionName, 
                returnImmediately: false, maxMessages: 1);
            ReceivedMessage received = response.ReceivedMessages.FirstOrDefault();
            
            return received;
        }

        private async Task<SubscriberClient.Reply> ProcessMessageAsync(PubsubMessage message)
        {
            int? attempt = message.GetDeliveryAttempt() ?? 0;

            try
            {               
                // Process the message.
                string json = Encoding.UTF8.GetString(message.Data.ToArray());
                Logger.Log($"Received message id:{message.MessageId}, attempt:{attempt} Body:{json}");

                IProcessor processor = QueueMessageParser.GetProcessor(json);               
                await processor.ProcessAsync();
                
                Logger.Log($"Returning Ack for message id:{message.MessageId}");

                return SubscriberClient.Reply.Ack;
            }
            catch (Exception e)
            {
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