using System;
using Google.Cloud.Firestore;

namespace SlalomTracker.Cloud
{
    [FirestoreData]
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
        [FirestoreProperty]        
        public string HotUrl { get; set; }

        [FirestoreProperty]        
        public string ThumbnailUrl { get; set; }
        
        [FirestoreProperty]  
        public string JsonUrl { get; set; }
        
        [FirestoreProperty]  
        public string Skier { get; set; }

        [FirestoreProperty]  
        public double CenterLineDegreeOffset { get; set; }

        /// <summary>
        /// Rope Length in canonical "Off" format; 15,22,28,32, etc...
        /// </summary>
        [FirestoreProperty]          
        public double RopeLengthM { get; set; }

        [FirestoreProperty]  
        public double BoatSpeedMph { get; set; }

        [FirestoreProperty]  
        public bool HasCrash { get; set; }

        [FirestoreProperty]  
        public bool All6Balls { get; set; }

        [FirestoreProperty]  
        public string CourseName { get; set; }

        [FirestoreProperty]  
        public double EntryTime { get; set; }

        [FirestoreProperty]  
        public string Notes { get; set; }

        [FirestoreProperty]  
        public DateTime RecordedTime { get; set; }

        [FirestoreProperty]  
        public bool MarkedForDelete { get; set; } = false;

        [FirestoreProperty]  
        public bool Starred { get; set; } = false;
    }
}
