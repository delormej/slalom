using System;
using System.IO;
using SlalomTracker.Cloud;
using Newtonsoft.Json;
using SlalomTracker.Logging;
using Microsoft.Extensions.Logging;

namespace SlalomTracker.Video 
{
    public class VideoTime
    {
        private ILogger<VideoTime> _log = 
            SkiLogger.Factory.CreateLogger<VideoTime>();        

        private string _videoJsonUrl;

        public double Start { get; set; }
        public double Duration { get; set; }

        public VideoTime() {}

        public VideoTime(string videoUrl) 
        {
            _videoJsonUrl = GetVideoJsonUrl(videoUrl);
        }

        public bool LoadVideoJson()
        {
            try 
            {
                IStorage storage = new AzureStorage();
                string localJson = storage.DownloadVideo(_videoJsonUrl);
            
                _log.LogInformation($"Found override json: {_videoJsonUrl}");
                VideoTime obj = FromJsonFile(localJson);
                this.Start = obj.Start;
                this.Duration = obj.Duration;
            }
            catch (System.Net.WebException)
            {
                _log.LogDebug("No json override found for " + _videoJsonUrl);
                return false;
            }

            return true;
        }

        public static string GetVideoJsonUrl(string videoUrl)
        {
            if (!videoUrl.ToUpper().EndsWith("MP4"))
                throw new ApplicationException("VideoUrl is not an MP4: " + videoUrl);
            
            return videoUrl.Substring(0, videoUrl.Length - 3) + "json";
        }

        public static VideoTime FromJsonFile(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            VideoTime obj = JsonConvert.DeserializeObject<VideoTime>(json);

            return obj;
        }
    }
}