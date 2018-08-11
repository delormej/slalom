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
        internal static CoursePass CreateTestCoursePass()
        {
            return CoursePassFromCSV.Load("..\\..\\..\\GOPR0403.csv", 0.0);
        }

        [TestMethod]
        public void TestGetCourseHeadingDeg()
        {

        }

        [TestMethod]
        public void TestCoursePositionFromGeo()
        {
            //Course course = CreateTestCourse();
            //course.SetCourseEntry(42.286670, -71.358994);
            //course.SetCourseExit(42.289249, -71.359091);

            //CoursePosition position = course.CoursePositionFromGeo(42.286770, -71.35900);
        }

        [TestMethod]
        public void TestGenerateCourseFeatures()
        {

        }
    }
}
