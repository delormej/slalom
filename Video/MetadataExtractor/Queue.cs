using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace MetadataExtractor
{
    class Queue
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
            string format = @"{ 'Name:' {0}, 'Url': {1} }";
            string value = string.Format(format, blobName, fullUri);

            CloudQueueMessage message = new CloudQueueMessage(value);
            Task task = _queue.AddMessageAsync(message);
            task.Wait();
        }

        public CloudQueueMessage Get()
        {
            Task<CloudQueueMessage> task = _queue.GetMessageAsync();
            task.Wait();
            return task.Result;
        }

        public void Finish(CloudQueueMessage message)
        {
            Task task = _queue.DeleteMessageAsync(message);
            task.Wait();
        }
    }
}
