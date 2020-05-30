using System;
using System.IO;
using Microsoft.WindowsAzure.Storage.Table;

namespace SlalomTracker.Cloud
{
    public class ComparedVideoEntity : BaseVideoEntity
    {
        public string Video1Url { get; set; }
        public string Video2Url { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }

        public ComparedVideoEntity(string videoUrl, DateTime creationTime, string createdBy = null) : 
            base(videoUrl, creationTime)
        {
            this.Created = creationTime;
            this.CreatedBy = createdBy;
        }
    }
}
