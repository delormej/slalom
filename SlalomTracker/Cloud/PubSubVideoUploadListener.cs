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
        const int AckDeadlineExtensionMilliseconds = 9 * 60 * 1000;

        public event EventHandler Completed;
        
        static string _projectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");

        CancellationTokenSource _cancel;
        SubscriberServiceApiClient _subscriber;
        SubscriptionName _subscriptionName;
        Task _processor;
        
        public PubSubVideoUploadListener(string subscriptionId, bool readDeadLetter = false)
        {
            if (string.IsNullOrEmpty(subscriptionId))
                throw new ArgumentNullException("subscriptionId", "Must provide a subscription Id as parameter.");

            if (string.IsNullOrEmpty(_projectId))
                throw new ApplicationException("GOOGLE_PROJECT_ID env variable must be set.");

            _subscriptionName = SubscriptionName.FromProjectSubscription(_projectId, subscriptionId);
            _subscriber = SubscriberServiceApiClient.Create();
            _cancel = new CancellationTokenSource();
        }

        public void Start()
        {
            // can we eliminate _processor variable? Serves no purpose?
            _processor = Task.Run(ListenAsync, _cancel.Token);
        }

        public void Stop()
        {
            Logger.Log("Stopping...");
            _cancel.Cancel();
        }

        private async Task ListenAsync()
        {
            ReceivedMessage received = null;
            Timer extendAck = null;

            try
            {
                while (received == null && !_cancel.IsCancellationRequested)
                    received = PullMessage();

                extendAck = new Timer(ExtendAckDeadline, received.AckId, 
                    AckDeadlineExtensionMilliseconds, AckDeadlineExtensionMilliseconds);
                
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
            finally 
            {
                extendAck?.Dispose();
            }

            if (Completed != null)
                Completed(this, null);            
        }

        private ReceivedMessage PullMessage()
        {
            Logger.Log("Attempting to pull message.");
            PullResponse response = _subscriber.Pull(_subscriptionName, 
                returnImmediately: false, maxMessages: 1);
            ReceivedMessage received = response.ReceivedMessages.FirstOrDefault();
            
            return received;
        }

        private void ExtendAckDeadline(object state)
        {
            string[] ackId = new string[] { state.ToString() };
            _subscriber.ModifyAckDeadline(_subscriptionName, ackId, 
                AckDeadlineExtensionMilliseconds / 1000);                
            
            Logger.Log($"Extended {_subscriptionName} ackId:{state} by {(AckDeadlineExtensionMilliseconds / 1000)} seconds.");
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