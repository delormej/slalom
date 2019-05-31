using System;
using Newtonsoft.Json;

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

//{"Name":"2019-05-30/002.MP4","Url":"https://storage.googleapis.com/skivideos/2019-05-30/002.MP4"}
//{"Name":"2019-05-21/005.MP4","Url":"https://storage.googleapis.com/skivideos/2019-05-21/005.MP4"}
/*
{"topic":"/subscriptions/40a293b5-bd26-47ef-acc3-c001a5bfce82/resourceGroups/ski/providers/Microsoft.Storage/storageAccounts/skivideostorage","subject":"/blobServices/default/containers/ski/blobs/2019-05-30/002.MP4","eventType":"Microsoft.Storage.BlobCreated","eventTime":"2019-05-31T11:10:24.7662457Z","id":"3ebcf7e8-d01e-0054-0da1-1786ab0645a4","data":{"api":"PutBlob","clientRequestId":"cd7c29e0-846e-4658-80bb-e62bd23fcb4a","requestId":"3ebcf7e8-d01e-0054-0da1-1786ab000000","eTag":"0x8D6E5B8947FD465","contentType":"application/octet-stream","contentLength":1018731,"blobType":"BlockBlob","url":"https://skivideostorage.blob.core.windows.net/ski/2019-05-30/002.json","sequencer":"00000000000000000000000000000FA40000000005c3e9f9","storageDiagnostics":{"batchId":"103b0961-bd62-417a-a0c4-562fdb69877c"}},"dataVersion":"","metadataVersion":"1"}

 */