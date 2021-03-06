using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using SlalomTracker.Cloud;
using SlalomTracker.Video;
using Microsoft.Extensions.Logging;
using SlalomTracker.Logging;

namespace SlalomTracker.Cloud
{
    public class PubSubVideoUploadListener : IUploadListener
    {
        private ILogger<PubSubVideoUploadListener> _log = 
            SkiLogger.Factory.CreateLogger<PubSubVideoUploadListener>();

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
                _log.LogInformation("Waiting for processor to complete.");
            _processorTask.Wait();
            
            _log.LogInformation("Stopped");
        }

        private void InternalStop()
        {
            _log.LogInformation("Stopping...");
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
                _log.LogInformation($"Received message id:{message.MessageId}, attempt:{attempt} Body:{json}");

                if (!_deadLetter)
                {
                    IProcessor processor = VideoParserFactory.CreateFromMessage(json);
                    await processor.ProcessAsync();
                }

                reply = SubscriberClient.Reply.Ack;
            }
            catch (Exception e)
            {
                _log.LogError($"ERROR: Attempt #{attempt} for message {message.MessageId}.", e);
            }
            
            _log.LogInformation($"Message handler completed with {reply} for {message.MessageId}.");
            
            if (++messagesProcessed >= _maxMessagesToProcess)
                InternalStop();

            return reply;
        }
    }
}