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
        public const int UnlimitedMessages = int.MaxValue;

        public event EventHandler Completed;
        
        static string _projectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");

        SubscriberClient _subscriber;
        SubscriptionName _subscriptionName;
        Task _processorTask;
        bool _deadLetter;
        int _maxMessagesToProcess;
        
        public PubSubVideoUploadListener(string subscriptionId, bool readDeadLetter = false,
            int maxMessagesToProcess = UnlimitedMessages)
        {
            _maxMessagesToProcess = maxMessagesToProcess;
            _deadLetter = readDeadLetter;

            if (string.IsNullOrEmpty(subscriptionId))
                throw new ArgumentNullException("subscriptionId", "Must provide a subscription Id as parameter.");

            if (string.IsNullOrEmpty(_projectId))
                throw new ApplicationException("GOOGLE_PROJECT_ID env variable must be set.");

            _subscriptionName = SubscriptionName.FromProjectSubscription(_projectId, subscriptionId);
            _subscriber = CreateSubscriber();
        }

        public void Start()
        {
            _processorTask = _subscriber.StartAsync(ProcessMessageAsync);
        }

        public void Stop()
        {
            if (!_processorTask.IsCompleted)
                Logger.Log("Waiting for processor to complete.");
            _processorTask.Wait();
            
            Logger.Log("Stopped");
        }

        private void InternalStop()
        {
            Logger.Log("Stopping...");
            // Subscriber will stop when the last message is fully processed.
            _subscriber.StopAsync(CancellationToken.None).ContinueWith(_ =>
            {
                if (Completed != null)
                    Completed(this, null);
                else 
                    Stop();
            });
        }

        private SubscriberClient CreateSubscriber() 
        {
            var task = SubscriberClient.CreateAsync(
                subscriptionName: _subscriptionName,
                settings: new SubscriberClient.Settings() {
                    FlowControlSettings = new Google.Api.Gax.FlowControlSettings(
                        maxOutstandingElementCount: 1,
                        maxOutstandingByteCount: null),
                    MaxTotalAckExtension = TimeSpan.FromHours(1) 
                }
            );
            task.Wait();
            return task.Result;
        }

        private async Task<SubscriberClient.Reply> ProcessMessageAsync(PubsubMessage message, 
            CancellationToken cancel)
        {
            int messagesProcessed = 0;
            SubscriberClient.Reply reply = SubscriberClient.Reply.Nack;

            if (cancel.IsCancellationRequested)
            {
                InternalStop();
                return reply;
            }

            int? attempt = message.GetDeliveryAttempt() ?? 0;

            try
            {               
                // Process the message.
                string json = Encoding.UTF8.GetString(message.Data.ToArray());
                Logger.Log($"Received message id:{message.MessageId}, attempt:{attempt} Body:{json}");

                if (!_deadLetter)
                {
                    IProcessor processor = QueueMessageParser.GetProcessor(json);               
                    await processor.ProcessAsync();
                }

                reply = SubscriberClient.Reply.Ack;
            }
            catch (Exception e)
            {
                Logger.Log($"ERROR: Attempt #{attempt} for message {message.MessageId}.", e);
            }
            
            Logger.Log($"Message handler completed with {reply} for {message.MessageId}.");
            
            if (++messagesProcessed >= _maxMessagesToProcess)
                InternalStop();

            return reply;
        }
    }
}