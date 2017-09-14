using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SlalomTracker
{
    [TestClass]
    public class TrackerTest
    {
        public static readonly Course COURSE = new Course() {
            CourseEntryCL = new GeoCoordinate() { Latitude = 0, Longitude = 0 },
            CourseExitCL = new GeoCoordinate() { Latitude = 259, Longitude = 23 }
        };

        [TestMethod]
        public void TestTrack()
        {
            Rope rope = new Rope(16);
            Tracker tracker = new Tracker(COURSE, rope);

        }
    }
}
