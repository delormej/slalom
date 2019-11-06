using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using System.Linq;

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

        public Task<float> GetBucketSizeAsync(string bucketName = null)
        {
            return Task<float>.Run( () => 
            {
                var list = _storage.ListObjects(bucketName ??= BucketName);
                float size = list.Sum(o => (float?)o.Size ?? default(float));
                return size / (1024F*1024F); // Convert to MB
            });
        }        
    }
}