using System;
using System.IO;
using Microsoft.WindowsAzure.Storage.Table;

namespace SlalomTracker.Cloud
{
    public class SkiVideoEntity : TableEntity
    {
        public SkiVideoEntity()
        {
            
        }
        public SkiVideoEntity(string videoUrl, CoursePass pass)
        {
            this.Url = videoUrl;
            SetKeys(videoUrl);
            BoatSpeedMph = pass.AverageBoatSpeed;
            CourseName = pass.Course.Name;
            EntryTime = pass.GetSecondsAtEntry();
        }

        public string Url { get; set; }
        
        public string ThumbnailUrl { get; set; }
        
        public string JsonUrl { get; set; }
        
        public string Skier { get; set; }

        public double RopeLengthM { get; set; }

        public double BoatSpeedMph { get; set; }

        public bool HasCrash { get; set; }

        public bool All6Balls { get; set; }

        public string CourseName { get; set; }

        public double EntryTime { get; set; }

        public string SlalomTrackerVersion { 
            get { return GetVersion(); }
        }

        private void SetKeys(string videoUrl)
        {
            string path = Storage.GetBlobName(videoUrl);

            if (path.Contains(@"\"))
                path = path.Replace('\\', '/');

            if (!path.Contains(@"/"))
                throw new ApplicationException("path must contain <date>/Filename.");

            int index = path.LastIndexOf(Path.AltDirectorySeparatorChar);
            this.PartitionKey = path.Substring(0, index);
            this.RowKey = path.Substring(index + 1, path.Length - index - 1);
        }

        private string GetVersion()
        {
            var extractor = typeof(MetadataExtractor.Extract).Assembly.GetName();
            var assembly = System.Reflection.Assembly.GetEntryAssembly().GetName();
            string version = $"{extractor.Name}:v{extractor.Version.ToString()}\n" +
                $"{assembly.Name}:v{assembly.Version.ToString()}";

            return version;
        }
    }
}
