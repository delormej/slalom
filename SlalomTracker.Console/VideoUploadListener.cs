using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Logger = jasondel.Tools.Logger;
using Microsoft.Azure.ServiceBus;
using SlalomTracker.Video;
using SlalomTracker.Cloud;

namespace SkiConsole
{
    public class VideoUploadListener
    {
        const string ENV_VIDEOQUEUE = "SKIQUEUE";
        const string ENV_SERVICEBUS = "SKISB";

        const string DefaultQueueName = "video-uploaded";
        private IQueueClient _queueClient;        
        
        private bool _deadLetterMode = false;

        public VideoUploadListener(string queueName = null, bool readDeadLetter = false)
        {
            string serviceBusConnectionString = Environment.GetEnvironmentVariable(ENV_SERVICEBUS);
            if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
                throw new ApplicationException($"Missing service bus connection string in env variable: {ENV_SERVICEBUS}");

            if (queueName == null)
                queueName = Environment.GetEnvironmentVariable(ENV_VIDEOQUEUE) ?? DefaultQueueName;
            
            _deadLetterMode = readDeadLetter;
            if (_deadLetterMode)
                queueName += "/$DeadLetterQueue";

            _queueClient = new QueueClient(serviceBusConnectionString, queueName);
            
            Logger.Log($"Connected to queue {queueName}");
        }

        public void Start()
        {
            if (_deadLetterMode)
            {
                Logger.Log($"Reading dead letter messages...");
                ReadDeadLetter();
            }
            else 
            {
                RegisterOnMessageHandlerAndReceiveMessages();
                Logger.Log($"Listening for messages...");
            }
        }

        public void Stop()
        {
            System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
            long peakMemory = process.PeakWorkingSet64;           

            Logger.Log($"Stopping...  Peak memory was: {peakMemory}");            
            _queueClient.CloseAsync().Wait();
        }

        void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether the message pump should automatically complete the messages after returning from user callback.
                // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
                AutoComplete = false,
                
                // Setting this to the absolute max time it should take to process a video avoids this error: 
                // The lock supplied is invalid. Either the lock expired, or the message has already been removed from the queue.
                MaxAutoRenewDuration = TimeSpan.FromMinutes(30)
            };

            // Register the function that processes messages.
            _queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }         

        async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            // Process the message.
            Logger.Log($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

            string json = Encoding.UTF8.GetString(message.Body);
            string videoUrl = QueueMessageParser.GetUrl(json);
            Logger.Log($"Received this videoUrl: {videoUrl}");

            if (videoUrl.EndsWith("test.MP4"))
                throw new ApplicationException("Something broke, fail safe for testing exceptions!");
           
            SkiVideoProcessor processor = new SkiVideoProcessor(videoUrl);
            await processor.ProcessAsync();
            await _queueClient.CompleteAsync(message.SystemProperties.LockToken);

            // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
            // If queueClient has already been closed, you can choose to not call CompleteAsync() or AbandonAsync() etc.
            // to avoid unnecessary exceptions.
        }        

        // Use this handler to examine the exceptions received on the message pump.
        Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Logger.Log($"Message handler encountered an exception", exceptionReceivedEventArgs.Exception);
            Logger.Log($"StackTrace == " + exceptionReceivedEventArgs.Exception.StackTrace);
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Logger.Log("Exception context for troubleshooting:");
            Logger.Log($"- Endpoint: {context.Endpoint}");
            Logger.Log($"- Entity Path: {context.EntityPath}");
            Logger.Log($"- Executing Action: {context.Action}");

            return Task.CompletedTask;
        }      

        /// <summary>
        /// This is just a way to review and drain the deadletter queue.
        /// </summary>
        void ReadDeadLetter()
        {
            _queueClient.RegisterMessageHandler( (message, cancel) => {
                Logger.Log($"Dead letter message: {Encoding.UTF8.GetString(message.Body)}");
                return Task.CompletedTask;
            }, new MessageHandlerOptions(ExceptionReceivedHandler) { AutoComplete = true });
        }  
    }
}

