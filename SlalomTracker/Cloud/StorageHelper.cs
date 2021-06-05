using System;
using System.IO;
using System.Net;
using SlalomTracker.Logging;
using Microsoft.Extensions.Logging;

namespace SlalomTracker.Cloud
{
    public class StorageHelper
    {
        static ILogger<StorageHelper> _log = 
            SkiLogger.Factory.CreateLogger<StorageHelper>();        

        public static string GetLocalPath(string videoUrl)
        {
            var uri = new UriBuilder(videoUrl);
            string filename = Path.GetFileName(uri.Path);

            return filename;
        }

        public static string GetBlobName(string localFile, DateTime creationTime)
        {
            string dir = GetBlobDirectory(creationTime);
            string blob = dir + Path.GetFileName(localFile);
            return blob;
        }

        /// <summary>
        /// Returns a date followed by '/', eg: "YYYY-MM-DD/"
        /// </summary>
        /// <param name="localFile"></param>
        /// <returns></returns>
        public static string GetBlobDirectory(DateTime creationTime)
        {
            return creationTime.ToString("yyyy-MM-dd") + "/";
        }

        public static string DownloadVideo(string videoUrl)
        {
            string path = StorageHelper.GetLocalPath(videoUrl);
            if (File.Exists(path)) 
            {
                _log.LogInformation("File already exists.");
            }
            else 
            {
                _log.LogInformation($"Requesting video: {videoUrl}.");
                    
                WebClient client = new WebClient();
                client.DownloadFile(videoUrl, path);
            }

            _log.LogInformation($"File is here: {path}");
            return path;
        }
    }
}
