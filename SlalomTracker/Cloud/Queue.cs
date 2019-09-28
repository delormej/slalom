using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace SlalomTracker.Cloud
{
    public class Queue
    {
        CloudQueue _queue;

        internal Queue(CloudQueue queue)
        {
            _queue = queue;
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
