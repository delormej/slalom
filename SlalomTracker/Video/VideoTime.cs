using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SlalomTracker.Cloud;
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
            GetVideoJsonUrl(videoUrl);
        }

        public bool LoadVideoJson()
        {
            try 
            {
                Storage storage = new Storage();
                string localJson = Storage.DownloadVideo(_videoJsonUrl);
                ParseJson(localJson);
            }
            catch (System.Net.WebException e)
            {
                Logger.Log("No json override found for " + _videoJsonUrl);
                return false;
            }

            return true;
        }

        private void GetVideoJsonUrl(string videoUrl)
        {
            if (!videoUrl.ToUpper().EndsWith("MP4"))
                throw new ApplicationException("VideoUrl is not an MP4: " + videoUrl);
            
            _videoJsonUrl = videoUrl.Substring(0, videoUrl.Length - 3) + "json";
        }

        private void ParseJson(string jsonFile)
        {
            //JsonSerializer.Deserialize
        }
    }
}