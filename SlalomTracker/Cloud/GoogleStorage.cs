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
        private string _bucketName;
        public string BaseUrl { get; private set; }

        StorageClient _storage;

        public GoogleStorage()
        {
            _bucketName = Environment.GetEnvironmentVariable("GOOGLE_STORAGE_BUCKET") 
                ?? "skivideo";
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

        public async Task AddSkiVideoEntityAsync(SkiVideoEntity entity)
        {
            try
            {
                string projectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
                FirestoreDb db = FirestoreDb.Create(projectId);

                DocumentReference doc = db.Collection("videos").
                    Document(entity.PartitionKey);
                await doc.Collection("videos").Document(entity.RowKey).SetAsync(entity);


                DocumentReference docRef = db.Collection("videos").Document("2020-05-20")
                    .Collection("videos").Document("GOPR2453_ts.MP4");
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                var skiEntity = snapshot.ConvertTo<SkiVideoEntity>();
                Console.WriteLine($"My Timestamp: {skiEntity.Timestamp}");

                if (snapshot.Exists)
                {
                    Console.WriteLine("Document data for {0} document:", snapshot.Id);
                    Dictionary<string, object> city = snapshot.ToDictionary();
                    foreach (KeyValuePair<string, object> pair in city)
                    {
                        Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
                    }
                }
                else
                {
                    Console.WriteLine("Document {0} does not exist!", snapshot.Id);
                }

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