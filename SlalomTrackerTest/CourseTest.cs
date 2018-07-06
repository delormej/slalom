using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SlalomTracker
{
    [TestClass]
    public class CourseTest
    {
        internal static double r_earth = 6378; // km
        internal static double latitude = 42.289087, longitude = -71.359124; // original lat/long position

        /// <summary>
        /// Test hepler method that adds longitude distance to the start of the course.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="distanceM"></param>
        /// <returns></returns>
        internal static double AddDistance(double latitude, double longitude, double distanceM)
        {
            //double new_latitude = latitude + (dy / r_earth) * (180 / Math.PI);
            double new_longitude = longitude + ((distanceM / 1000) / r_earth) * (180 / Math.PI) / Math.Cos(latitude * Math.PI / 180);

            return new_longitude;
        }

        /// <summary>
        /// Dummy course for testing.
        /// </summary>
        /// <returns></returns>
        internal static Course CreateTestCourse()
        {
            Course course = new Course();

            course.SetCourseEntry(latitude, longitude);  // TODO need to get lat/long of start and finish.
            course.SetCourseExit(latitude, AddDistance(latitude, longitude, 259));

            return course;
        }

        [TestMethod]
        public void TestGetCourseHeadingDeg()
        {

        }

    }
}
