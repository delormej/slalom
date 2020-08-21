using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using System.Linq;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace SlalomTracker.Cloud
{
    public class GoogleStorage
    {
        public string BucketName;
        public string BaseUrl;

        StorageClient _storage;

        public GoogleStorage()
        {
            BucketName = "skivideo";
            BaseUrl = $"https://storage.googleapis.com/{BucketName}/";
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
                storageObject = await _storage.UploadObjectAsync(BucketName, objectName, contentType, f);
            }

            if (storageObject == null)
                throw new ApplicationException($"Unable to upload Video {localFile} to Google storage.");

            return $"{BaseUrl}{objectName}";      
        }

        public async Task AddSkiVideoEntityAsync(SkiVideoEntity entity)
        {
            try
            {
                string projectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
                FirestoreDb db = FirestoreDb.Create(projectId);

                DocumentReference doc = db.Collection("videos").
                    Document("2020-08-20");
                await doc.Collection("videos").Document("GOPR1111.MP4").CreateAsync(entity);

            // Alternatively, collection.Document("los-angeles").Create(city);
            // DocumentReference document = await collection.AddAsync(entity);            
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR!\n" + e.Message);
            }
        }

        public Task<float> GetBucketSizeAsync()
        {
            return Task<float>.Run( () => 
            {
                var list = _storage.ListObjects(BucketName);
                float size = list.Sum(o => (float?)o.Size ?? default(float));
                return size / (1024F*1024F); // Convert to MB
            });
        }        

        public IEnumerable<GoogleStorageObject> GetLargest()
        {
            var list = _storage.ListObjects(BucketName);
            var objects = list.OrderByDescending(o => o.Size)
                .Select(o => 
                    new GoogleStorageObject() { Name = o.Name, Size = o.Size }
                );
            return objects;
        }                

        public Task DeleteAsync(string videoUrl)
        {
            return Task.Run(() => 
                _storage.DeleteObject(BucketName, GetObjectName(videoUrl))
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