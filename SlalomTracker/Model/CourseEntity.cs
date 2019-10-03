using System;
using GeoCoordinatePortable;
using Microsoft.WindowsAzure.Storage.Table;

namespace SlalomTracker.Cloud
{
    /// <summary>
    /// Wraps a Course object in an entity that is serializable to table storage.
    /// </summary>
    internal class CourseEntity : TableEntity
    {
        public CourseEntity()
        {
        }
        public double EntryLat {get;set;}
        public double EntryLon {get;set;}
        public double ExitLat {get;set;}
        public double ExitLon {get;set;}

        public GeoCoordinate Course55EntryCL 
        { 
            get 
            {
                if (EntryLat == default(double))
                    return null;
                else
                    return new GeoCoordinate(this.EntryLat, this.EntryLon);
            } 
            set 
            {
                this.EntryLat = value.Latitude;
                this.EntryLon = value.Longitude;
            }
        }
        public GeoCoordinate Course55ExitCL 
        {
            get 
            {
                if (ExitLat == default(double))
                    return null;               
                else 
                    return new GeoCoordinate(this.ExitLat, this.ExitLon);
            } 
            set 
            {
                this.ExitLat = value.Latitude;
                this.ExitLon = value.Longitude;
            }        
        }

        public string FriendlyName { get; set; }

        public Course ToCourse() 
        {
            Course course = new Course();
            if (this.Course55EntryCL !=null && this.Course55ExitCL != null)
            {
                course.Course55EntryCL = this.Course55EntryCL;
                course.Course55ExitCL = this.Course55ExitCL;
            };
            course.FriendlyName = this.FriendlyName;
            course.Name = this.RowKey;

            return course;
        }
    }
}