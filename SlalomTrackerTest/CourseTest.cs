using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SlalomTracker
{
    [TestClass]
    public class CourseTest
    {
        internal static double r_earth = 6378; // km
        internal static double latitude = 42.289087, longitude = -71.359124; // original lat/long position
        private Course _course;

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

        internal static Course CreateTestCourse()
        {
            return CreateTestCoursePass().Course;
        }

        [TestInitialize]
        public void Setup()
        {
            if (_course == null)
                _course = CreateTestCourse();
        }

        [TestMethod]
        public void TestGetCourseHeadingDeg()
        {
            double heading = _course.GetCourseHeadingDeg();
            Assert.IsTrue((int)heading == 185, "heading is not equal");
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
            _course.GenerateCourseFeatures();
            // Quick spot check.
            //Ball[5] = { X: 23,Y: 287}
            //PreGate[3] = { X: 12.75,Y: 369}
            Assert.IsTrue(_course.Balls[5].X == 23 && _course.Balls[5].Y == 287, "Balls are in wrong CoursePosition.");
            Assert.IsTrue(_course.PreGates[3].X == 12.75 && _course.PreGates[3].Y == 369, "PreGates are in wrong CoursePosition.");
        }
    }
}
