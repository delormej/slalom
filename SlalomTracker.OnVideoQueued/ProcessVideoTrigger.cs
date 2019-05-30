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
        const string StorageConnection = "SKIBLOBS";

        const string EVENT_FUNCTION_STARTED = "ProcessVideoTrigger_Started";
        const string EVENT_VIDEO_DEQUEUED = "Video_Dequeued";
        const string EVENT_VIDEO_PROCESSED = "Video_Processed";

        private TelemetryClient insights;

        public ProcessVideo(TelemetryConfiguration config)
        {
            this.insights = new TelemetryClient(config);
            TrackEvent(EVENT_FUNCTION_STARTED, "");
        }

        [FunctionName("ProcessVideoTrigger")]
        public void Run(
            [QueueTrigger(QueueName, Connection=StorageConnection)]string videoItem, ILogger log)
        {
            TrackEvent(EVENT_VIDEO_DEQUEUED, videoItem);
            try 
            {
                SkiVideoEntity video = JsonConvert.DeserializeObject<SkiVideoEntity>(videoItem);
                log.LogInformation($"C# Queue trigger function processed: {videoItem}");
                Console.WriteLine($"C# Queue trigger function processed: {video.Url}");
                ProcessVideoMetadata(video.Url);
                TrackEvent(EVENT_VIDEO_PROCESSED, video.Url);
                Console.WriteLine("Succesfully processed: " + video.Url);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                throw;
            }            
        }
        
        private static void ProcessVideoMetadata(string url)
        {
            string localPath = Storage.DownloadVideo(url);
            string json = Extract.ExtractMetadata(localPath);
            Storage storage = new Storage();
            string blobName = Storage.GetBlobName(localPath);
            storage.AddMetadata(blobName, json);
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
                Console.Error.WriteLine("Insights object not initialized, error trying to record: " + eventName);
            }
        }
    }
}
