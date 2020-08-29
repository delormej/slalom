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
        private string _bucketName;
        private string _projectId;
        public string BaseUrl { get; private set; }

        StorageClient _storage;

        public GoogleStorage()
        {
            _projectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
            _bucketName = Environment.GetEnvironmentVariable("GOOGLE_STORAGE_BUCKET") 
                ?? "skivideo";
            
            if (_projectId == null || _bucketName == null)
                throw new ApplicationException("GOOGLE_PROJECT_ID and GOOGLE_STORAGE_BUCKET env variables missing.");

            BaseUrl = $"https://storage.googleapis.com/{_bucketName}/";
            _storage = StorageClient.Create();
        }

        public async Task<string> UploadVideoAsync(string localFile, DateTime creationTime)
        {
            string directory = Storage.GetBlobDirectory(creationTime);
            string objectName = directory + System.IO.Path.GetFileName(localFile);
            
            Google.Apis.Storage.v1.Data.Object storageObject = null;
            
            using (var f = File.OpenRead(localFile))
            {
                string contentType = localFile.ToUpper().EndsWith("MP4") ? "video/mp4" : null;
                storageObject = await _storage.UploadObjectAsync(_bucketName, objectName, contentType, f);
            }

            if (storageObject == null)
                throw new ApplicationException($"Unable to upload Video {localFile} to Google storage.");

            return $"{BaseUrl}{objectName}";      
        }

        public string DownloadVideo(string videoUrl)
        {
            return null;
        }
        
        public string UploadVideo(string localFile, DateTime creationTime) 
        {
            return null;
        }
        
        public string UploadThumbnail(string localFile, DateTime creationTime)
        {
            return null;
        }

        public void DeleteIngestedBlob(string url)
        {}

        // VideoMetadataStorage
        public void AddMetadata(SkiVideoEntity entity, string json)
        {}
        
        public void UpdateMetadata(SkiVideoEntity entity)
        {}
        
        public async Task AddTableEntityAsync(BaseVideoEntity entity, string tableName = null)
        {
            await AddTableEntityAsync(entity as SkiVideoEntity);
        }

        public async Task AddTableEntityAsync(SkiVideoEntity entity)
        {
            const string tableName = "videos";
            try
            {
                FirestoreDb db = FirestoreDb.Create(_projectId);

                DocumentReference doc = db.Collection(tableName)
                    .Document(entity.PartitionKey);
                await doc.Collection(tableName).Document(entity.RowKey)
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
            return null;
        }

        public async Task<IEnumerable<SkiVideoEntity>> GetAllMetdataAsync()
        {
            FirestoreDb db = FirestoreDb.Create(_projectId);
            Query query = db.CollectionGroup("videos"); // .WhereEqualTo("MarkedForDelete", false); // <-- This requires an index which doesn't exist.
            QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

            IEnumerable<SkiVideoEntity> videos = querySnapshot.Documents
                .Select(d => d.ConvertTo<SkiVideoEntity>());

            return videos.Where(v => v.MarkedForDelete == false);
        }

        // CourseMetadataStorage
        public List<Course> GetCourses()
        {
            return null;
        }
        
        public void UpdateCourse(Course course)
        {}
 

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

        public class GoogleStorageObject
        {
            public string Name;
            public ulong? Size;
        }
    }
}