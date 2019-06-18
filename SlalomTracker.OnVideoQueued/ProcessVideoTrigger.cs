using System;
using System.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using SlalomTracker;
using SlalomTracker.Cloud;
using MetadataExtractor;
using Newtonsoft.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

namespace SlalomTracker.OnVideoQueued
{
    public class ProcessVideo
    {
        const string QueueName = "skiqueue";
        const string StorageConnectionEnvVar = Storage.ENV_SKIBLOBS;

        const string EVENT_FUNCTION_STARTED = "ProcessVideoTrigger_Started";
        const string EVENT_VIDEO_DEQUEUED = "Video_Dequeued";
        const string EVENT_VIDEO_PROCESSED = "Video_Processed";

        private TelemetryClient insights;

        public ProcessVideo(TelemetryConfiguration config)
        {
            this.insights = new TelemetryClient(config);
            TrackEvent(EVENT_FUNCTION_STARTED, "with_config");
        }

        [FunctionName("ProcessVideoTrigger")]
        public void Run(
            [QueueTrigger(QueueName, Connection=StorageConnectionEnvVar)]
            string videoItem, ILogger log)
        {
            TrackEvent(EVENT_VIDEO_DEQUEUED, videoItem);
            string videoUrl = QueueMessageParser.GetUrl(videoItem);
            if (videoUrl == null)
            {
                Console.WriteLine("No videoUrl found in message.");
            }
            else
            {
                ContainerInstance.Create(videoUrl);
                TrackEvent(EVENT_VIDEO_PROCESSED, videoUrl);
                Console.WriteLine($"Succesfully processed: {videoUrl}");
            }          
        }
        
        private void TrackEvent(string eventName, string value)
        {
            if (this.insights != null)
            {
                var evt = new EventTelemetry(eventName);
                evt.Properties.Add("value", value);
                this.insights.TrackEvent(evt);
            }
            else
            {
                Console.Error.WriteLine("Insights object not initialized, error trying to record: " + 
                    eventName + ", " + value);
            }
        }
    }
}
