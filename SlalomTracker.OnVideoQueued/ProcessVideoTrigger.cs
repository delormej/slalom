using System;
using System.Net;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SlalomTracker.Cloud;
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

        const string WEBAPI_CREATE_CONTAINER = "http://ski-app.azurewebsites.net/api/processvideo";

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
            try 
            {
                string videoUrl = QueueMessageParser.GetUrl(videoItem);
                CreateContainerInstance(videoUrl);
                TrackEvent(EVENT_VIDEO_PROCESSED, videoUrl);
                Console.WriteLine($"Succesfully processed: {videoUrl}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error processing message: {videoItem}\nException: {e}");
                throw;
            }            
        }

        private void CreateContainerInstance(string videoUrl)
        {
            try 
            {
                // MSI does not seem to be working in Azure Functions in a Container,
                // so delegating this to just posting URL to the web api which kicks
                // off the container job.
                string payload = $"{{'Url': '{videoUrl}'}}";
                WebClient web = new WebClient();
                string response = web.UploadString(WEBAPI_CREATE_CONTAINER, payload);
                Console.WriteLine("CreateContainerInstance Response: " + response);
            }
            catch (Exception e)
            {
                string error = $"CreateContainerInstance: Unable to invoke web api to start container instance for {videoUrl}";
                Console.WriteLine(error +  e);
                throw;
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
