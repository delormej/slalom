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
        public event EventHandler Completed;

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
            lock(_queueClient)
            {
                if (!_queueClient.IsClosedOrClosing)
                {
                    var task = _queueClient.CloseAsync();

                    System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                    long peakMemory = process.PeakWorkingSet64;           
                    Logger.Log($"Stopping...  Peak memory was: {(peakMemory/1024/1024).ToString("F1")} mb");            

                    task.Wait();
                }
                else
                {
                    Logger.Log("Queue already closing.");
                }
            }
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
                MaxAutoRenewDuration = TimeSpan.FromMinutes(60)
            };

            // Register the function that processes messages.
            _queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);            
        }         

        async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            if (_queueClient.IsClosedOrClosing)
            {
                Logger.Log("Queue is closing, abandoning message.");
                await _queueClient.AbandonAsync(message.SystemProperties.LockToken);
                
                return;
            }

            try
            {
                // Process the message.
                Logger.Log($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} " +
                    $"Body:{Encoding.UTF8.GetString(message.Body)}");

                string json = Encoding.UTF8.GetString(message.Body);
                IProcessor processor = QueueMessageParser.GetProcessor(json);
                
                await processor.ProcessAsync();
                
                await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
            }
            catch (Exception e)
            {
                if (message.SystemProperties.DeliveryCount <= 2)
                {
                    Logger.Log($"Abandoned message.", e);
                    // abandon and allow another to try in case of transient errors
                    await _queueClient.AbandonAsync(message.SystemProperties.LockToken); 
                }
                else
                {
                    Logger.Log($"Dead lettering message.", e);
                    await _queueClient.DeadLetterAsync(message.SystemProperties.LockToken,
                        e.Message, e.InnerException?.Message);
                }
            }
            finally
            {
                Logger.Log($"Message handler completed.");
                if (Completed != null)
                    Completed(this, null);
            }
        }        

        // Use this handler to examine the exceptions received on the message pump.
        Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Logger.Log($"Message handler encountered an exception", exceptionReceivedEventArgs.Exception);
            return Task.CompletedTask;
        }      

        /// <summary>
        /// This is just a way to review and drain the deadletter queue.
        /// </summary>
        void ReadDeadLetter()
        {
            _queueClient.RegisterMessageHandler( (message, cancel) => {
                Logger.Log($"Dead letter message: {Encoding.UTF8.GetString(message.Body)}");
                Logger.Log($"Reason: {message.UserProperties["DeadLetterReason"]}");
                return Task.CompletedTask;
            }, new MessageHandlerOptions(ExceptionReceivedHandler) { AutoComplete = true });
        }  
    }
}

