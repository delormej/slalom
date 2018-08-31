using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace MetadataExtractor
{
    public class Queue
    {
        const string BLOB_QUEUE = "skiqueue";
        CloudQueue _queue;

        public Queue(CloudStorageAccount account)
        {
            CloudQueueClient client = account.CreateCloudQueueClient();
            _queue = client.GetQueueReference(BLOB_QUEUE);
            Task task = _queue.CreateIfNotExistsAsync();
            task.Wait();
        }

        public void Add(string blobName, string fullUri)
        {
            var obj = new { Name = blobName, Url = fullUri };
            string json = JsonConvert.SerializeObject(obj);
            CloudQueueMessage message = new CloudQueueMessage(json);
            Task task = _queue.AddMessageAsync(message);
            task.Wait();
        }

        public CloudQueueMessage Get()
        {
            Task<CloudQueueMessage> task = _queue.GetMessageAsync();
            task.Wait();
            return task.Result;
        }

        public void Delete(CloudQueueMessage message)
        {
            Task task = _queue.DeleteMessageAsync(message);
            task.Wait();
        }
    }
}
