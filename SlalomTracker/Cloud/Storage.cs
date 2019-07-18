using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Text.RegularExpressions;

namespace SlalomTracker.Cloud
{
    public class Storage
    {
        const string SKICONTAINER = "ski";
        const string INGEST_SKICONTAINER = "ski-ingest";
        const string SKITABLE = "skivideos";
        public const string ENV_SKIBLOBS = "SKIBLOBS";
        const string BLOB_QUEUE = "skiqueue";

        public string BlobStorageUri 
        {
            get 
            {
                return _account.BlobStorageUri.PrimaryUri.AbsoluteUri;
            }
        }

        CloudStorageAccount _account;
        Queue _queue;

        public Storage()
        {
            Connect();
            ConnectToQueue();
        }

        public CloudStorageAccount Account { get { return _account; } }
        public Queue Queue { get { return _queue; } }

        public SkiVideoEntity AddMetadata(string videoUrl, string json)
        {
            CoursePass pass = CoursePassFactory.FromJson(json);
            return AddMetadata(videoUrl, "", json, pass);
        }

        public SkiVideoEntity AddMetadata(string videoUrl, string thumbnailUrl, string json, CoursePass pass)
        {
            string blobName = GetBlobName(videoUrl);
            string jsonUrl = UploadMeasurements(blobName, json);
            SkiVideoEntity entity = new SkiVideoEntity(videoUrl, pass);
            entity.ThumbnailUrl = thumbnailUrl;
            entity.JsonUrl = jsonUrl;
            AddTableEntity(entity);
            Console.WriteLine("Uploaded metadata for video:" + videoUrl);
            return entity;
        }

        public void UpdateMetadata(SkiVideoEntity entity)
        {
            CloudTableClient client = _account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(SKITABLE);
            TableOperation update = TableOperation.Merge(entity);
            update.Entity.ETag = "*"; // Allow last-writer wins approach.
            Task updateTask = table.ExecuteAsync(update);
            updateTask.Wait();               
        }

        public async Task<List<SkiVideoEntity>> GetAllMetdata()
        {
            CloudTableClient client = _account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(SKITABLE);
            TableQuery<SkiVideoEntity> query = new TableQuery<SkiVideoEntity>().Where("");
            TableQuerySegment<SkiVideoEntity> result = await table.ExecuteQuerySegmentedAsync(query, null);
            return result.Results;
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
            Task<bool> existsTask = blob.ExistsAsync();
            existsTask.Wait();
            if (existsTask.Result)
            {
                Console.WriteLine($"Cloud Blob {blobName} already exists. Overwriting by default.");
            }

            Console.WriteLine($"Uploading file: {localFile}");
            var task = blob.UploadFromFileAsync(localFile);
            task.Wait();

            string uri = blob.SnapshotQualifiedUri.AbsoluteUri;
            Console.WriteLine($"Uploaded file to {uri}");
            return uri; // URL to the uploaded file.
        }

        public string UploadThumbnail(string localFile)
        {
            // For now there is no difference between a video and any other file upload.
            return UploadVideo(localFile);
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

        public List<String> GetAllBlobUris()
        {
            return BlobRestApi.GetBlobs(
                _account.Credentials.AccountName, 
                GetAccountKey(),
                SKICONTAINER);
        } 

        private static string GetAccountKey()
        {
            string connection = GetConnectionString();
            string pattern = @"AccountKey=([^;]+)";
            string accountKey = "";
            var match = Regex.Match(connection, pattern);
            if (match != null && match.Groups.Count > 0)
                accountKey = match.Groups[1].Value;

            return accountKey;
        }

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

        private void WalkDirectories(string path) 
        {
            WalkFiles(path);
            string[] dirs = Directory.GetDirectories(path);
            for (int i = 0; i < dirs.Length; i++) 
            {
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

        public static string GetBlobName(string localFile)
        {
            string dir = GetBlobDirectory(localFile);
            string blob = dir + Path.GetFileName(localFile);
            return blob;
        }

        /// <summary>
        /// Returns a date followed by '/', eg: "YYYY-MM-DD/"
        /// </summary>
        /// <param name="localFile"></param>
        /// <returns></returns>
        public static string GetBlobDirectory(string localFile)
        {
            // Remove HERO5 Black x directory.
            int heroMonikerStart = localFile.IndexOf("HERO");
            if (heroMonikerStart > 0)
            {
                localFile = localFile.Substring(0, heroMonikerStart);
            }

            string dir = "";
            int start = 0, end = localFile.LastIndexOf(Path.DirectorySeparatorChar);
            if (end >= 0)
            {
                start = localFile.LastIndexOf(Path.DirectorySeparatorChar, end - 1);
                dir = localFile.Substring(start + 1, (end - start) - 1);
            }
            dir += "/";
            return dir;
        }

        public void DeleteIngestedBlob(string url)
        {
            string blobName = "";
            try
            {
                blobName = GetBlobName(url);
                CloudBlockBlob blob = GetBlobReference(blobName, INGEST_SKICONTAINER);
                if (blob == null)
                    throw new ApplicationException($"Error deleting. {blobName} did not exist.");
                blob.DeleteAsync().Wait();
                Console.WriteLine($"Deleted {blobName}");
            }
            catch (Exception e)
            {
                throw new ApplicationException(
                    $"Unable to delete blob {blobName} at {url}", e);
            }
        }

        private CloudBlockBlob GetBlobReference(string blobName, string container = SKICONTAINER)
        {
            CloudBlobContainer blobContainer = GetBlobContainer(container);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blobName);
            return blob;
        }

        private CloudBlobContainer GetBlobContainer(string container)
        {
            CloudBlobClient blobClient = _account.CreateCloudBlobClient();
            return blobClient.GetContainerReference(container);
        }

        private void Connect()
        {
            string connection = GetConnectionString();
            if (!CloudStorageAccount.TryParse(connection, out _account))
            {
                // Otherwise, let the user know that they need to define the environment variable.
                string error =
                    "A connection string has not been defined in the system environment variables. " +
                    "Add a environment variable named '" + ENV_SKIBLOBS + "' with your storage " +
                    "connection string as a value.";
                throw new ApplicationException(error);
            }
        }

        private static string GetConnectionString()
        {
            return Environment.GetEnvironmentVariable(ENV_SKIBLOBS);
            //ENV SKIBLOBS = "DefaultEndpointsProtocol=https;AccountName=skivideostorage;AccountKey=74gV///fVtd/ZL+PzXZU6nsOVzIvt6XC59T9elFnY91vCVqmitlHxNA9QLbQsedTmnCzSR0BhtL0J8dwOVSWvA==;EndpointSuffix=core.windows.net"
        }

        private void ConnectToQueue()
        {
            CloudQueueClient client = _account.CreateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(BLOB_QUEUE);
            Task task = queue.CreateIfNotExistsAsync();
            task.Wait();
            _queue = new Queue(queue);
        }

        private void QueueNewVideo(string blobName, string url)
        {
            _queue.Add(blobName, url);
        }

        private void AddTableEntity(SkiVideoEntity entity)
        {
            CloudTableClient client = _account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(SKITABLE);
            TableOperation insert = TableOperation.InsertOrReplace(entity);
            Task createTask = table.CreateIfNotExistsAsync();
            createTask.Wait();
            Task insertTask = table.ExecuteAsync(insert);
            insertTask.Wait();
        }

        private string UploadMeasurements(string blobName, string json)
        {
            if (!blobName.EndsWith(".MP4"))
                throw new ApplicationException("Path to video must end with .MP4");

            string fileName = blobName.Replace(".MP4", ".json");
            CloudBlockBlob blob = GetBlobReference(fileName);
            Task t = blob.UploadTextAsync(json);
            t.Wait();
            string uri = blob.SnapshotQualifiedUri.AbsoluteUri;
            return uri;
        }
    }    
}
