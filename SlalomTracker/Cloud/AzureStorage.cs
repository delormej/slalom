using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Text.RegularExpressions;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker.Cloud
{
    public class AzureStorage : IStorage
    {
        const string SKICONTAINER = "ski";
        const string INGEST_SKICONTAINER = "ski-ingest";
        const string SKITABLE = "skivideos";
        const string COURSETABLE = "courses";
        public const string ENV_SKIBLOBS = "SKIBLOBS";
        const string BLOB_QUEUE = "skiqueue";
        private CloudStorageAccount _account;
        private Queue _queue;

        public string BlobStorageUri 
        {
            get 
            {
                return _account.BlobStorageUri.PrimaryUri.AbsoluteUri;
            }
        }

        public AzureStorage()
        {
            Connect();
            ConnectToQueue();
        }

        public void AddMetadata(SkiVideoEntity entity, string json)
        {
            string blobName = StorageHelper.GetBlobName(entity.Url, entity.RecordedTime);
            string jsonUrl = UploadMeasurements(blobName, json);
            entity.JsonUrl = jsonUrl; 
            AddTableEntity(entity);
            Logger.Log("Uploaded metadata for video:" + entity.Url);            
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

        public async Task<IEnumerable<SkiVideoEntity>> GetAllMetdataAsync()
        {
            CloudTableClient client = _account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(SKITABLE);

            TableQuery<SkiVideoEntity> query = new TableQuery<SkiVideoEntity>(); //.Take(10);

            List<SkiVideoEntity> results = new List<SkiVideoEntity>();
            TableContinuationToken token = null;

            do
            {
                var seg = await table.ExecuteQuerySegmentedAsync(query, token);
                token = seg.ContinuationToken;
                results.AddRange(seg.Results);
            }
            while (token != null);

            return results;
        }

        public string UploadVideo(string localFile, DateTime creationTime)
        {
            string blobName = StorageHelper.GetBlobName(localFile, creationTime);
            CloudBlockBlob blob = GetBlobReference(blobName);
            Task<bool> existsTask = blob.ExistsAsync();
            existsTask.Wait();
            if (existsTask.Result)
            {
                Logger.Log($"Cloud Blob {blobName} already exists. Overwriting by default.");
            }

            Logger.Log($"Uploading file: {localFile}");
            var task = blob.UploadFromFileAsync(localFile);
            task.Wait();

            string uri = blob.SnapshotQualifiedUri.AbsoluteUri;
            Logger.Log($"Uploaded file to {uri}");
            return uri; // URL to the uploaded file.
        }

        public string UploadThumbnail(string localFile, DateTime creationTime)
        {
            // For now there is no difference between a video and any other file upload.
            return UploadVideo(localFile, creationTime);
        }

        public string DownloadVideo(string videoUrl)
        {
            return StorageHelper.DownloadVideo(videoUrl);
        }
        public void AddToQueue(string blobName, string videoUrl)
        {
            _queue.Add(blobName, videoUrl);
        }

        public void DeleteIngestedBlob(string url)
        {
            string blobName = "";
            try
            {
                Uri uri = new Uri(url);
                // Remove container name from path.
                blobName = uri.LocalPath.Replace(Path.DirectorySeparatorChar + INGEST_SKICONTAINER + Path.DirectorySeparatorChar, "");
                CloudBlockBlob blob = GetBlobReference(blobName, INGEST_SKICONTAINER);
                if (blob == null)
                    throw new ApplicationException($"Error deleting. {blobName} did not exist.");
                blob.DeleteAsync().Wait();
                Logger.Log($"Deleted {blobName}");
            }
            catch (Exception e)
            {
                // Warning, not an error.
                Logger.Log($"Unable to delete blob {blobName} from ingest container {INGEST_SKICONTAINER}.\n\tError: {e.Message}");
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
        }

        private void ConnectToQueue()
        {
            CloudQueueClient client = _account.CreateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(BLOB_QUEUE);
            Task task = queue.CreateIfNotExistsAsync();
            task.Wait();
            _queue = new Queue(queue);
        }

        private void AddTableEntity(SkiVideoEntity entity)
        {
            AddTableEntityAsync(entity, SKITABLE).Wait();
        }

        public async Task AddTableEntityAsync(SkiVideoEntity entity)
        {
            await AddTableEntityAsync(entity, SKITABLE);
        }

        public async Task AddTableEntityAsync(BaseVideoEntity entity, string tableName)
        {
            CloudTableClient client = _account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            TableOperation insert = TableOperation.InsertOrReplace(entity);
            await table.CreateIfNotExistsAsync();
            var result = await table.ExecuteAsync(insert);
            
            Logger.Log($"Inserted table entity {tableName} with status code: {result.HttpStatusCode}");
        }

        public SkiVideoEntity GetSkiVideoEntity(string recordedDate, string mp4Filename)
        {
            // ParitionKey format YYYY-MM-DD
            // RowKey format e.g. GOPR01444.MP4
            SkiVideoEntity entity = null;

            try
            {
                CloudTableClient client = _account.CreateCloudTableClient();
                CloudTable table = client.GetTableReference(SKITABLE);
                TableOperation retrieve = TableOperation.Retrieve<SkiVideoEntity>(recordedDate, mp4Filename);
                Task<TableResult> retrieveTask = table.ExecuteAsync(retrieve);
                retrieveTask.Wait();
                entity = (SkiVideoEntity)retrieveTask.Result.Result;
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to retrieve SkiVideoEntity; ParitionKey {recordedDate}, RowKey {mp4Filename}", e);
            }

            return entity;
        }

        public List<Course> GetCourses()
        {
            CloudTableClient client = _account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(COURSETABLE);
            TableQuery<CourseEntity> query = new TableQuery<CourseEntity>().Where("");
            var result = table.ExecuteQuerySegmentedAsync(query, null);
            result.Wait();
            
            List<Course> courses = new List<Course>();
            foreach (CourseEntity entity in result.Result)
                courses.Add(entity.ToCourse());

            return courses;
        }    

        public void UpdateCourse(Course course)
        {
            const string coursePartitionKey = "cochituate";
            if (course == null)
                throw new ApplicationException("You must pass a course object to update.");

            CloudTableClient client = _account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(COURSETABLE);

            // Get existing entity, if one exists.
            TableQuery<CourseEntity> query = new TableQuery<CourseEntity>()
                .Where($"PartitionKey eq '{coursePartitionKey}' and RowKey eq '{course.Name}'");
            var result = table.ExecuteQuerySegmentedAsync(query, null);
            result.Wait();
            CourseEntity entity = result.Result.FirstOrDefault();

            if (entity != null) 
            {
                entity.FriendlyName = course.FriendlyName;
                entity.Course55EntryCL = course.Course55EntryCL;
                entity.Course55ExitCL = course.Course55ExitCL;
            } 
            else 
            {
                entity = new CourseEntity();
                entity.FriendlyName = course.FriendlyName;
                entity.PartitionKey = coursePartitionKey;
                entity.RowKey = course.Name;
                entity.Course55EntryCL = course.Course55EntryCL;
                entity.Course55ExitCL = course.Course55ExitCL;                
            }

            TableOperation insert = TableOperation.InsertOrReplace(entity);
            Task createTask = table.CreateIfNotExistsAsync();
            createTask.Wait();
            Task insertTask = table.ExecuteAsync(insert);
            insertTask.Wait();
            
            Logger.Log($"Inserted course: {entity.RowKey}");
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
