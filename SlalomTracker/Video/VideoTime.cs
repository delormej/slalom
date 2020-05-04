using System;
using System.IO;
using SlalomTracker.Cloud;
using Newtonsoft.Json;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker.Video 
{
    public class VideoTime
    {
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
                string localJson = Storage.DownloadVideo(_videoJsonUrl);
            
                Logger.Log($"Found override json: {_videoJsonUrl}");
                VideoTime obj = FromJsonFile(localJson);
                this.Start = obj.Start;
                this.Duration = obj.Duration;
            }
            catch (System.Net.WebException)
            {
                Logger.Log("No json override found for " + _videoJsonUrl);
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