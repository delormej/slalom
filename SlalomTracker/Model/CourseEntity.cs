using System;
using GeoCoordinatePortable;
using Microsoft.WindowsAzure.Storage.Table;

namespace SlalomTracker.Cloud
{
    public class CourseEntity : TableEntity
    {
        public CourseEntity()
        {
        }

        public GeoCoordinate Course55EntryCL { get; set; }
        public GeoCoordinate Course55ExitCL { get; set; }
        public string FriendlyName { get; set; }
    }
}