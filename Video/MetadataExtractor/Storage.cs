using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using SlalomTracker;
using Newtonsoft.Json;

namespace MetadataExtractor
{
    public class Storage
    {
        const string SKICONTAINER = "ski";
        const string SKITABLE = "skivideos";
        CloudStorageAccount _account;
        Queue _queue;

        public Storage(CloudStorageAccount account)
        {
            _account = account;
            _queue = new Queue(_account);
        }

        public void AddMetadata(string path)
        {
            SkiVideoEntity entity = new SkiVideoEntity(path);
            CloudTableClient client = _account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(SKITABLE);
            TableOperation insert = TableOperation.InsertOrReplace(entity);
            Task createTask = table.CreateIfNotExistsAsync();
            createTask.Wait();
            Task insertTask = table.ExecuteAsync(insert);
            insertTask.Wait();
        }

        public void UploadMeasurements(string path, List<Measurement> measurements)
        {
            if (!path.EndsWith(".MP4"))
                throw new ApplicationException("Path to video must end with .MP4");

            string fileName = path.Replace(".MP4", ".json");
            string json = JsonConvert.SerializeObject(measurements);
            CloudBlockBlob blob = GetBlobReference(fileName);
            Task t = blob.UploadTextAsync(json);
            t.Wait();
        }

        public void UploadVideos(string path)
        {
            if (IsFilePath(path))
            {
                string outputUrl = UploadVideo(path);
                Console.WriteLine("Wrote " + path + " to " + outputUrl);
            }
            else
            {
                WalkDirectories(path);
            }            
        }

        public string UploadVideo(string localFile)
        {
            string blobName = GetBlobName(localFile);
            CloudBlockBlob blob = GetBlobReference(blobName);
            var task = blob.UploadFromFileAsync(localFile);
            task.Wait();

            string uri = blob.SnapshotQualifiedUri.AbsoluteUri;
            QueueNewVideo(blobName, uri);
            return uri; // URL to the uploaded video.
        }

        public bool BlobNameExists(string blobName)
        {
            // NOTE: this is not the full URL, only the name, i.e. 2018-08-24/GOPRO123.MP4
            CloudBlockBlob blob = GetBlobReference(blobName);
            Task<bool> t = blob.ExistsAsync();
            t.Wait();
            return t.Result;
        }

        public static string DownloadVideo(string videoUrl)
        {
            string path = GetLocalPath(videoUrl);
            if (File.Exists(path)) 
            {
                Console.WriteLine("File already exists.");
            }
            else 
            {
                Console.Write("Requesting video: " + videoUrl + " ...");

                string directory = Path.GetDirectoryName(path);
                if (directory != String.Empty && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                WebClient client = new WebClient();
                client.DownloadFile(videoUrl, path);
            }

            Console.WriteLine("Files is here: " + path);
            return path;
        }        

        private static string GetLocalPath(string videoUrl)
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

        private void WalkDirectories(string path) 
        {
            string[] dirs = Directory.GetDirectories(path);
            for (int i = 0; i < dirs.Length; i++) 
            {
                WalkFiles(dirs[i]);
                WalkDirectories(dirs[i]);
            }         
        }

        private void WalkFiles(string path)
        {
            string[] files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; i++)
            {
                string outputUrl = UploadVideo(files[i]);
                Console.WriteLine("Wrote " + path + " to " + outputUrl);
            }
        }

        /* Checks to see if it's a valid File or Directory.  
            returns True if File, False if Directory, exception if neither.
        */
        private bool IsFilePath(string localFile)
        {
            if (!File.Exists(localFile))
            {
                if (!Directory.Exists(localFile))
                    throw new FileNotFoundException("Invalid file or directory: " + localFile);
                else
                    return false;
            }         
            else 
                return true;
        }

        private string GetBlobName(string localFile)
        {
            if (!File.Exists(localFile))
                throw new FileNotFoundException("Video file does not exist: " + localFile);

            string dir = GetBlobDirectory(localFile);
            string blob = dir + Path.GetFileName(localFile);
            return blob;
        }

        private string GetBlobDirectory(string localFile)
        {
            // Remove HERO5 Black x directory.
            int heroMonikerStart = localFile.IndexOf("HERO");
            if (heroMonikerStart > 0)
            {
                localFile = localFile.Substring(0, heroMonikerStart);
            }

            string dir = "";
            int end = 0, start = localFile.LastIndexOf(Path.DirectorySeparatorChar);
            if (start >= 0)
            {
                for (int i = start - 1; i > 0; i--)
                {
                    if (localFile[i] == Path.DirectorySeparatorChar)
                    {
                        end = i;
                        break;
                    }
                }
                dir = localFile.Substring(end, (start - end));
            }
            if (dir != string.Empty)
                dir += "/";
            return dir;
        }

        private CloudBlockBlob GetBlobReference(string blobName)
        {
            CloudBlobClient blobClient = _account.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(SKICONTAINER);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blobName);
            return blob;
        }

        private void QueueNewVideo(string blobName, string url)
        {
            Queue queue = new Queue(_account);
            queue.Add(blobName, url);
        }
    }    
}