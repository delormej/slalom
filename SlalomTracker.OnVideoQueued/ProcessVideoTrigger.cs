using System;
using System.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using SlalomTracker;
using SlalomTracker.Cloud;
using MetadataExtractor;
using Newtonsoft.Json;

namespace SlalomTracker.OnVideoQueued
{
    public static class ProcessVideo
    {
        const string QueueName = "skiqueue";
        const string StorageConnection = "SKIBLOBS";

        [FunctionName("ProcessVideoTrigger")]
        public static void Run(
            [QueueTrigger(QueueName, Connection=StorageConnection)]string videoItem, ILogger log)
        {
            try 
            {
                SkiVideoEntity video = JsonConvert.DeserializeObject<SkiVideoEntity>(videoItem);
                log.LogInformation($"C# Queue trigger function processed: {videoItem}");
                Console.WriteLine($"C# Queue trigger function processed: {video.Url}");
                ProcessVideoMetadata(video.Url);
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
    }
}
