using System;
using Newtonsoft.Json;

namespace SlalomTracker.OnVideoQueued
{
    public class MessageParser
    {
        public static string GetUrl(string message)
        {
            dynamic msg = JsonConvert.DeserializeObject(message);
            if (msg.Url != null)
                return msg.Url;
            else if (msg.eventType == "Microsoft.Storage.BlobCreated")
                return GetUrlFromAzureStorageEvent(msg);
            else
                return null;
        }

        private static string GetUrlFromAzureStorageEvent(dynamic msg)
        {
            if (msg.data != null && msg.data.url != null)
                return msg.data.url.ToString();
            else 
                return null;
        }
    }
}
