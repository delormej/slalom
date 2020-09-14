using System;
using System.IO;
using System.Net;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker.Cloud
{
    public class StorageHelper
    {
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
                Logger.Log("File already exists.");
            }
            else 
            {
                Logger.Log("Requesting video: " + videoUrl + " ...");
                    
                WebClient client = new WebClient();
                client.DownloadFile(videoUrl, path);
            }

            Logger.Log("File is here: " + path);
            return path;
        }
    }
}
