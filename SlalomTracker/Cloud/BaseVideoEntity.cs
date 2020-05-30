using System;
using System.IO;
using Microsoft.WindowsAzure.Storage.Table;

namespace SlalomTracker.Cloud
{
    public abstract class BaseVideoEntity : TableEntity
    {
        
        public string Url { get; set; }
        public string SlalomTrackerVersion { get; set; }

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