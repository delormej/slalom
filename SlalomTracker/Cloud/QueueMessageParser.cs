using System;
using Newtonsoft.Json;

namespace SlalomTracker.Cloud
{
    public class QueueMessageParser
    {
        public static string GetUrl(string message)
        {
            string videoUrl = null;
            dynamic msg = JsonConvert.DeserializeObject(message);
            if (msg.Url != null)
                videoUrl = msg.Url;
            else if (msg.eventType == "Microsoft.Storage.BlobCreated")
                videoUrl = GetUrlFromAzureStorageEvent(msg);
            else if (msg.message != null)
                videoUrl = GetUrlFromGoogleStorageEvent(msg);

            if (videoUrl == null || !(videoUrl.ToUpper().EndsWith("MP4")))
            {
                Console.WriteLine($"WARNING: Valid video url not found: {message}");
            }
            return videoUrl;
        }

        private static string GetUrlFromAzureStorageEvent(dynamic msg)
        {
            if (msg.data != null && msg.data.url != null)
                return msg.data.url.ToString();
            else 
                return null;
        }

        private static string GetUrlFromGoogleStorageEvent(dynamic objectEvent)
        {
            string eventType = objectEvent.message.attributes.eventType;

            if (eventType != "OBJECT_FINALIZE")
            {
                Console.Error.WriteLine("Invalid event type: " + eventType);
                return null;
            }

            string bucketId = objectEvent.message.attributes.bucketId;
            string objectId = objectEvent.message.attributes.objectId;
            string data = objectEvent.message.data;

            if (!objectId.ToUpper().EndsWith("MP4"))
            {
                Console.Error.WriteLine("Object was not an MP4 file: " + objectId);
                return null;
            }

            string url = string.Format("https://storage.googleapis.com/{0}/{1}", bucketId, objectId);
            return url;
        }        
    }
}

/*  EXAMPLE payload from gcloud pubsub.
{ 
    "message": 
    {
        "attributes": {
            "bucketId":"skivideos",
            "eventTime":"2019-05-30T00:30:28.286785Z",
            "eventType":"OBJECT_FINALIZE",
            "notificationConfig":"projects/_/buckets/skivideos/notificationConfigs/7",
            "objectGeneration":"1559176228286891",
            "objectId":"Program.cs",
            "payloadFormat":"JSON_API_V1"
        },
        "data":"ewogICJraW5kIjogInN0b3JhZ2Ujb2JqZWN0IiwKICAiaWQiOiAic2tpdmlkZW9zL1Byb2dyYW0uY3MvMTU1OTE3NjIyODI4Njg5MSIsCiAgInNlbGZMaW5rIjogImh0dHBzOi8vd3d3Lmdvb2dsZWFwaXMuY29tL3N0b3JhZ2UvdjEvYi9za2l2aWRlb3Mvby9Qcm9ncmFtLmNzIiwKICAibmFtZSI6ICJQcm9ncmFtLmNzIiwKICAiYnVja2V0IjogInNraXZpZGVvcyIsCiAgImdlbmVyYXRpb24iOiAiMTU1OTE3NjIyODI4Njg5MSIsCiAgIm1ldGFnZW5lcmF0aW9uIjogIjEiLAogICJjb250ZW50VHlwZSI6ICJ0ZXh0L3BsYWluIiwKICAidGltZUNyZWF0ZWQiOiAiMjAxOS0wNS0zMFQwMDozMDoyOC4yODZaIiwKICAidXBkYXRlZCI6ICIyMDE5LTA1LTMwVDAwOjMwOjI4LjI4NloiLAogICJzdG9yYWdlQ2xhc3MiOiAiTkVBUkxJTkUiLAogICJ0aW1lU3RvcmFnZUNsYXNzVXBkYXRlZCI6ICIyMDE5LTA1LTMwVDAwOjMwOjI4LjI4NloiLAogICJzaXplIjogIjczNTciLAogICJtZDVIYXNoIjogIkI1N3FLbkNOa0M0NnpiZldPQ2w5SkE9PSIsCiAgIm1lZGlhTGluayI6ICJodHRwczovL3d3dy5nb29nbGVhcGlzLmNvbS9kb3dubG9hZC9zdG9yYWdlL3YxL2Ivc2tpdmlkZW9zL28vUHJvZ3JhbS5jcz9nZW5lcmF0aW9uPTE1NTkxNzYyMjgyODY4OTEmYWx0PW1lZGlhIiwKICAiY3JjMzJjIjogIjY4cm5aZz09IiwKICAiZXRhZyI6ICJDS3ZqNm9tQXd1SUNFQUU9Igp9Cg==",
        "messageId":"566505797466534",
        "message_id":"566505797466534",
        "publishTime":"2019-05-30T00:30:28.699Z",
        "publish_time":"2019-05-30T00:30:28.699Z"
    },
    "subscription":"projects/halogen-premise-241213/subscriptions/onVideoPush"
}

    EXAMPLE of Azure Storage Event
{
    "topic": "/subscriptions/40a293b5-bd26-47ef-acc3-c001a5bfce82/resourceGroups/ski/providers/Microsoft.Storage/storageAccounts/skivideostorage",
    "subject": "/blobServices/default/containers/ski-ingest/blobs/GOPR0194.MP4",
    "eventType": "Microsoft.Storage.BlobCreated",
    "eventTime": "2019-06-05T22:01:30.7944655Z",
    "id": "953f1d64-f01e-0084-7cea-1b3a090654e7",
    "data": {
        "api": "PutBlockList",
        "clientRequestId": "b5a65386-df2b-400c-5909-274d55ef5439",
        "requestId": "953f1d64-f01e-0084-7cea-1b3a09000000",
        "eTag": "0x8D6EA015DBB6BB7",
        "contentType": "video/mp4",
        "contentLength": 380135973,
        "blobType": "BlockBlob",
        "url": "https://skivideostorage.blob.core.windows.net/ski-ingest/2019-05-17/007.MP4",
        "sequencer": "00000000000000000000000000000FAB000000000016ec22",
        "storageDiagnostics": {
            "batchId": "0157b3b6-c6d3-498d-b8b7-e9a0c2cad7b8"
        }
    },
    "dataVersion": "",
    "metadataVersion": "1"
}
*/
