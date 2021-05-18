using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using System.Linq;
using System.Collections.Generic;
using Google.Cloud.Firestore;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker.Cloud
{
    public class GoogleStorage : IStorage
    {
        const string CollectionName = "videos";
        private string _bucketName;
        private string _projectId;
        public string BaseUrl { get; private set; }

        StorageClient _storage;

        public GoogleStorage() :
            this(Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID"), 
                Environment.GetEnvironmentVariable("GOOGLE_STORAGE_BUCKET") )
        {}

        public GoogleStorage(string dbProjectId, string bucketName = "skivideo")
        {
            _projectId = dbProjectId ?? Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
            _bucketName = bucketName;

            if (_projectId == null || _bucketName == null)
                throw new ApplicationException("GOOGLE_PROJECT_ID and GOOGLE_STORAGE_BUCKET env variables missing.");

            BaseUrl = $"https://storage.googleapis.com/{_bucketName}/";
            _storage = StorageClient.Create();
        }

        public string BlobStorageUri 
        {
            get { return BaseUrl; }
        }

        public void AddToQueue(string blobName, string videoUrl)
        {
            throw new NotImplementedException();
        }

        public async Task<string> UploadVideoAsync(string localFile, DateTime creationTime)
        {
            string contentType = localFile.ToUpper().EndsWith("MP4") ? "video/mp4" : null;
            
            string url = null;
            using (var f = File.OpenRead(localFile))
                url = await UploadAsync(localFile, creationTime, contentType, f);
            
            return url;
        }

        public string DownloadVideo(string videoUrl)
        {
            return StorageHelper.DownloadVideo(videoUrl);
        }
        
        public string UploadVideo(string localFile, DateTime creationTime) 
        {
            var task = UploadVideoAsync(localFile, creationTime);
            task.Wait();
            
            return task.Result;
        }
        
        public string UploadThumbnail(string localFile, DateTime creationTime)
        {
            return UploadVideo(localFile, creationTime);
        }

        public void DeleteIngestedBlob(string url)
        {
            try
            {
                DeleteAsync(url).Wait();
            }
            catch (Exception e)
            {
                Logger.Log("WARNING: Unable to delete ingested blob.", e);
            }
        }

        // VideoMetadataStorage
        public void AddMetadata(SkiVideoEntity entity, string json)
        {
            entity.JsonUrl = UploadMetadata(entity, json);
            AddTableEntityAsync(entity).Wait();
            Logger.Log("Uploaded metadata for video:" + entity.Url);            
        }
        
        public void UpdateMetadata(SkiVideoEntity entity)
        {
            AddTableEntityAsync(entity).Wait();
        }
        
        public async Task AddTableEntityAsync(BaseVideoEntity entity, string tableName = null)
        {
            await AddTableEntityAsync(entity as SkiVideoEntity);
        }

        public async Task AddTableEntityAsync(SkiVideoEntity entity)
        {
            try
            {
                FirestoreDb db = FirestoreDb.Create(_projectId);

                DocumentReference doc = db.Collection(CollectionName)
                    .Document(entity.PartitionKey);
                await doc.Collection(CollectionName).Document(entity.RowKey)
                    .SetAsync(entity);

                Logger.Log($"Added Firestore metadata for {entity.Url}");
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to add Firestore metadata for {entity.Url}", e);
            }            
        }
        
        public SkiVideoEntity GetSkiVideoEntity(string recordedDate, string mp4Filename)
        {
            SkiVideoEntity entity = null;
            try 
            {
                FirestoreDb db = FirestoreDb.Create(_projectId);
                var videoRef = db.Collection(CollectionName).Document(recordedDate)
                    .Collection(CollectionName).Document(mp4Filename);

                var snapshotTask = videoRef.GetSnapshotAsync();
                snapshotTask.Wait();
                var snapshot = snapshotTask.Result;

                if (snapshot != null)
                    entity = snapshot.ConvertTo<SkiVideoEntity>();
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to get video {recordedDate}/{mp4Filename}.", e);
            }

            return entity;
        }

        public async Task<IEnumerable<SkiVideoEntity>> GetAllMetdataAsync()
        {
            FirestoreDb db = FirestoreDb.Create(_projectId);
            Query query = db.CollectionGroup(CollectionName); // .WhereEqualTo("MarkedForDelete", false); // <-- This requires an index which doesn't exist.
            QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

            IEnumerable<SkiVideoEntity> videos = querySnapshot.Documents
                .Select(d => d.ConvertTo<SkiVideoEntity>());

            Logger.Log($"Found {videos.Count()} videos.");

            return videos.Where(v => v.MarkedForDelete == false);
        }

        /// <summary>
        /// Returns urls of videos sitting in the uploaded (staging) video bucket.
        /// </summary>
        public IEnumerable<string> ListUploaded(string uploadBucket = null)
        {
            uploadBucket ??= _bucketName;
            var list = _storage.ListObjects(uploadBucket);

            foreach (var item in list)
                yield return item.MediaLink;
        }

        // CourseMetadataStorage
        public List<Course> GetCourses()
        {
            throw new NotImplementedException();
        }
        
        public void UpdateCourse(Course course)
        {
            throw new NotImplementedException();
        }

        public Task<float> GetBucketSizeAsync()
        {
            return Task<float>.Run( () => 
            {
                var list = _storage.ListObjects(_bucketName);
                float size = list.Sum(o => (float?)o.Size ?? default(float));
                return size / (1024F*1024F); // Convert to MB
            });
        }        

        public IEnumerable<GoogleStorageObject> GetLargest()
        {
            var list = _storage.ListObjects(_bucketName);
            var objects = list.OrderByDescending(o => o.Size)
                .Select(o => 
                    new GoogleStorageObject() { Name = o.Name, Size = o.Size }
                );
            return objects;
        }                

        public Task DeleteAsync(string videoUrl)
        {
            return Task.Run(() => 
                _storage.DeleteObject(_bucketName, GetObjectName(videoUrl))
            );
        }

        private string GetObjectName(string videoUrl)
        {
            return videoUrl.Replace(BaseUrl, "");
        }

        private string UploadMetadata(SkiVideoEntity entity, string json)
        {
            string blobName = StorageHelper.GetBlobName(entity.Url, entity.RecordedTime);
            if (!blobName.ToUpper().EndsWith(".MP4"))
                throw new ApplicationException($"Path to video must end with .MP4, provided {entity.Url}");

            string fileName = blobName.Replace(".MP4", ".json", true, null);

            byte[] byteArray = System.Text.Encoding.ASCII.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);
            var task = UploadAsync(fileName, entity.RecordedTime, "application/json", stream);
            task.Wait();
            
            return task.Result;
        }

        private async Task<string> UploadAsync(string fileName, DateTime creationTime, string contentType, Stream stream)
        {
            string objectName = StorageHelper.GetBlobName(fileName, creationTime);
            
            Google.Apis.Storage.v1.Data.Object storageObject = null;
            storageObject = await _storage.UploadObjectAsync(_bucketName, objectName, contentType, stream);

            if (storageObject == null)
                throw new ApplicationException($"Unable to upload Video {objectName} to Google storage.");

            return $"{BaseUrl}{objectName}";      
        }        

        public class GoogleStorageObject
        {
            public string Name;
            public ulong? Size;
        }
    }
}