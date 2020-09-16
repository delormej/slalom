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
                    while (received == null && !_cancel.IsCancellationRequested)
                        received = PullMessage();

                    PubsubMessage message = received.Message;

                    if (await ProcessMessageAsync(message) == SubscriberClient.Reply.Ack)
                    {
                        _subscriber.Acknowledge(_subscriptionName, new string[] {received.AckId});
                        Logger.Log($"Acknowledged message id:{message.MessageId}");
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Error pulling message.", e);
                }

                if (Completed != null)
                    Completed(this, null);
                    
            }, _cancel.Token);
        }

        public void Stop()
        {
            Logger.Log("Stopping...");
            _cancel.Cancel();
        }

        private ReceivedMessage PullMessage()
        {
            Logger.Log("Attempting to pull message.");
            PullResponse response = _subscriber.Pull(_subscriptionName, 
                returnImmediately: false, maxMessages: 1);
            ReceivedMessage received = response.ReceivedMessages.FirstOrDefault();
            
            return received;
        }

        private async Task<SubscriberClient.Reply> ProcessMessageAsync(PubsubMessage message)
        {

            SubscriberClient.Reply reply = SubscriberClient.Reply.Nack;

            int? attempt = message.GetDeliveryAttempt() ?? 0;

            try
            {               
                // Process the message.
                string json = Encoding.UTF8.GetString(message.Data.ToArray());
                Logger.Log($"Received message id:{message.MessageId}, attempt:{attempt} Body:{json}");

                IProcessor processor = QueueMessageParser.GetProcessor(json);               
                await processor.ProcessAsync();

                reply = SubscriberClient.Reply.Ack;
            }
            catch (Exception e)
            {
                Logger.Log($"ERROR: Attempt #{attempt} for message {message.MessageId}.", e);
            }
            
            Logger.Log($"Message handler completed with {reply} for {message.MessageId}.");
            
            return reply;
        }
    }
}