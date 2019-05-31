using System;
using Newtonsoft.Json;

public class MessageParser
{
    public static string GetUrl(string message)
    {
        dynamic msg = JsonConvert.DeserializeObject(message);
        if (msg.message != null)
            return GetUrlFromGCloudEvent(message);
        else if (msg.eventType == "Microsoft.Storage.BlobCreated")
            return GetUrlFromAzureStorageEvent(message);
        else
            return null;
    }

    private static string GetUrlFromGCloudEvent(string message)
    {
        if (message == null) {
            Console.Error.WriteLine("Cannot get Url from Event, message empty.");
            return null;
        }

        dynamic objectEvent = JsonConvert.DeserializeObject(message);
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

    private static string GetUrlFromAzureStorageEvent(string message)
    {
        dynamic msg = JsonConvert.DeserializeObject(message);
        if (msg.data != null && msg.data.url != null)
            return msg.data.url.ToString();
        else 
            return null;
    }
}