using System;
using System.IO;
using Google.Cloud.Firestore;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlalomTracker.Cloud
{
    public abstract class BaseVideoEntity : TableEntity
    {
        private DateTimeOffset? _timestamp;

        [FirestoreProperty]        
        public string Url { get; set; }
        
        [FirestoreProperty]        
        public string SlalomTrackerVersion { get; set; }

        [JsonIgnore] // This avoids an error with JSON Serialization Name Collision.  This should be removed when TableEntity base class is removed.
        [FirestoreDocumentUpdateTimestamp]
        public new DateTimeOffset? Timestamp 
        { 
            get { return _timestamp; } 
            set { _timestamp = value; } 
        }

        public BaseVideoEntity()
        {}

        public BaseVideoEntity(string videoUrl, DateTime creationTime)
        {
            this.Url = videoUrl;
            this.PartitionKey = creationTime.ToString("yyyy-MM-dd");
            this.RowKey = GetFilenameFromUrl(videoUrl); 
            this.SlalomTrackerVersion = GetVersion();
        }

        private string GetFilenameFromUrl(string videoUrl)
        {
            Uri uri = new Uri(videoUrl);
            string filename = System.IO.Path.GetFileName(uri.LocalPath);
            return filename;
        }

        private string GetVersion()
        {
            var extractor = typeof(MetadataExtractor.Extract).Assembly.GetName();
            var assembly = System.Reflection.Assembly.GetEntryAssembly().GetName();
            string version = $"{assembly.Name}:v{assembly.Version.ToString()}, " +
                $"{extractor.Name}:v{extractor.Version.ToString()}";

            return version;
        }        
    }
}