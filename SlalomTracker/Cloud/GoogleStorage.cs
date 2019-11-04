using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;

namespace SlalomTracker.Cloud
{
    public class GoogleStorage
    {
        public string BucketName;
        public string BaseUrl;

        public GoogleStorage()
        {
            BucketName = "skivideo";
            BaseUrl = $"https://storage.googleapis.com/{BucketName}/";
        }

        public async Task<string> UploadVideoAsync(string localFile, DateTime creationTime)
        {
            string directory = Storage.GetBlobDirectory(creationTime);
            string objectName = directory + System.IO.Path.GetFileName(localFile);
            
            Google.Apis.Storage.v1.Data.Object storageObject = null;
            
            using (var f = File.OpenRead(localFile))
            {
                StorageClient storage = StorageClient.Create();
                string contentType = localFile.ToUpper().EndsWith("MP4") ? "video/mp4" : null;
                storageObject = await storage.UploadObjectAsync(BucketName, objectName, contentType, f);
            }

            if (storageObject == null)
                throw new ApplicationException($"Unable to upload Video {localFile} to Google storage.");

            return $"{BaseUrl}{objectName}";      
        }

        private void ListBlobs(string bucketName)
        {
            // var list = storage.ListObjects(bucketName);
            // foreach (var o in list)
            // {
            //     System.Console.WriteLine(o.Id);
            // }            
        }        
    }
}