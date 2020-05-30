using System;
using System.IO;
using Microsoft.WindowsAzure.Storage.Table;

namespace SlalomTracker.Cloud
{
    public class SkiVideoEntity : BaseVideoEntity
    {
        public SkiVideoEntity()
        {
            
        }

        public SkiVideoEntity(string videoUrl, DateTime creationTime) : 
            base(videoUrl, creationTime)
        {
            this.RecordedTime = creationTime;
        }
       
        /// <summary>
        /// Url for hot storage (currently Google storage).  Only most recent 
        /// videos stored here.
        /// </summary>
        public string HotUrl { get; set; }

        public string ThumbnailUrl { get; set; }
        
        public string JsonUrl { get; set; }
        
        public string Skier { get; set; }

        public double CenterLineDegreeOffset { get; set; }

        /// <summary>
        /// Rope Length in canonical "Off" format; 15,22,28,32, etc...
        /// </summary>
        public double RopeLengthM { get; set; }

        public double BoatSpeedMph { get; set; }

        public bool HasCrash { get; set; }

        public bool All6Balls { get; set; }

        public string CourseName { get; set; }

        public double EntryTime { get; set; }

        public string Notes { get; set; }

        public DateTime RecordedTime { get; set; }

        public bool MarkedForDelete { get; set; } = false;

        public bool Starred { get; set; } = false;
    }
}
