using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SlalomTracker
{
    [TestClass]
    public class CoursePassTest
    {
        [TestMethod]
        public void TestTrack()
        {
            Rope rope = new Rope(16);
            CoursePass pass = new CoursePass(CourseTest.CreateTestCourse(), rope);

            pass.Track(DateTime.Now.Subtract(TimeSpan.FromSeconds(13)), 0.5, CourseTest.latitude, 
                CourseTest.AddDistance(CourseTest.latitude, CourseTest.longitude, 10));

            //foreach(var m in pass.Measurements)
            //{
            //    Console.WriteLine(m.HandlePosition);
            //}
        }
    }
}
