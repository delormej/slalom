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
            string path = "";
            // Get second to last directory seperator.
            int dirMarker = videoUrl.LastIndexOf('/');
            if (dirMarker > 0)
                dirMarker = videoUrl.LastIndexOf('/', dirMarker-1, dirMarker-1);
            if (dirMarker < 0)
            {
                path = Path.GetFileName(videoUrl);
            }
            else
            {
                path = videoUrl.Substring(dirMarker + 1, videoUrl.Length - dirMarker - 1);
            }
            return path;
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

                string directory = Path.GetDirectoryName(path);
                if (directory != String.Empty && !Directory.Exists(directory))
                {
                    // Edge case! Ensure a file doesn't also exist already with that name.
                    if (File.Exists(directory))
                    {
                        const string prefix = "_tmp_";
                        directory = prefix + directory;
                        path = prefix + path;
                    }
                    Directory.CreateDirectory(directory);
                }
                    
                WebClient client = new WebClient();
                client.DownloadFile(videoUrl, path);
            }

            Logger.Log("File is here: " + path);
            return path;
        }
    }
}
