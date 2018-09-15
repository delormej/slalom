using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;

namespace SlalomTracker
{
    [TestClass]
    public class CoursePassTest
    {
        CoursePass _pass;

        public static CoursePass TestTrack(double ropeM, double swingSpeedRadS, double boatSpeedMps)
        {
            Rope rope = new Rope(ropeM);
            CoursePass pass = new CoursePass(CourseTest.CreateTestCoursePass().Course, rope);
            pass.CourseEntryTimestamp = DateTime.Now.Subtract(TimeSpan.FromSeconds(15));

            // Travel down the course is 259m @ 14m/sec
            // 2 events per second
            // 
            const int eventsPerSecond = 16;
            double metersPerSecond = boatSpeedMps;
            double courseLengthM = 259 + rope.LengthM + (55*2);
            int events = (int)(courseLengthM / metersPerSecond) * eventsPerSecond;
            double ropeRadPerSecond = swingSpeedRadS;
            int ropeDirection = 1;
            const double centerLineX = 11.5;
            double ropeSpeed = ropeRadPerSecond;

            for (int i = 0; i < events; i++)
            {    
                if (i > 0)
                {
                    //double ropeAngle = pass.Measurements[pass.Measurements.Count - 1].RopeAngleDegrees;
                    double handlePosX = pass.Measurements[pass.Measurements.Count - 1].HandlePosition.X;

                    // Invert the direction of the rope when you hit the apex.
                    //if (ropeAngle > rope.GetHandleApexDeg() || ropeAngle < (rope.GetHandleApexDeg() * -1))
                    if (handlePosX > 22.5 || handlePosX < 0.5)
                    {
                        ropeDirection = ropeDirection * -1;
                    }

                    // Exponentially increment speed towards center line.
                    double ropeSpeedFactor = ((Math.Pow(handlePosX - centerLineX, 2) / 100) * ropeRadPerSecond);
                    if (ropeSpeedFactor > (ropeRadPerSecond*0.80)) ropeSpeedFactor = ropeRadPerSecond * 0.80;
                    ropeSpeed = ropeRadPerSecond - ropeSpeedFactor;
                }

                // increment 1 second & 14 meters.
                double longitude = CourseTest.AddDistance(CourseTest.latitude, CourseTest.longitude,
                    ((metersPerSecond / eventsPerSecond) * i) + ropeM);
                DateTime time = pass.CourseEntryTimestamp.AddSeconds((double)(1.0 / eventsPerSecond) * i);

                pass.Track(time, 
                    (ropeSpeed * ropeDirection), 
                    CourseTest.latitude, longitude);
            }

            Trace.WriteLine(string.Format("X: Apex:{0}", rope.GetHandleApexDeg()));
            foreach (var m in pass.Measurements)
            {
                Trace.WriteLine(m.HandlePosition.X);
            }

            Trace.WriteLine("Y:");
            foreach (var m in pass.Measurements)
            {
                Trace.WriteLine(m.HandlePosition.Y);
            }

            return pass;
        }

        [TestInitialize]
        public void Setup()
        {
            _pass = CourseTest.CreateTestCoursePass();
        }

        [TestMethod]
        public void CoursePositionFromGeoTest()
        {
            // Grab a boatposition and verify where in the X,Y course plane it should fit.
            // 7.45, 42.289983, -71.358973, 13.68, 0.11289 <-- before the course
            // 21.63, 42.288066, -71.359257, 14.87, 0.50935 <-- just prior to ball 1, after gate crossing
            // 40.71, 42.285529, -71.359519, 14.27, 0.59728 <-- between exit gates and the 55's 
            // 45.32, 42.285165, -71.359369, 5.60, -0.10074 <-- way past the course, looping around

            // 15.02, 42.288937, -71.359136, 14.33, 0.77112 <-- boat is passing through the 55s.

            //42.2867806,"Longitude":-71.3594418 == Chet @ 15 seconds into the GOPR0565.mp4
            // .\slalom\SlalomTracker\Video\MetadataExtractor\GOPR0565.json
            CoursePass pass = CoursePassFactory.FromFile(@"..\..\..\..\Video\MetadataExtractor\GOPR0565.json");
            CoursePosition position = pass.CoursePositionFromGeo(42.2867806, -71.3594418);

            Assert.IsTrue(position.X == 11.5, "Incorrect course position.");
            Assert.IsTrue((int)position.Y == 61, "Incorrect course position.");
        }

        [TestMethod]
        public void GetRopeArcLengthTest()
        {
            //current.RopeAngleDegrees = ;
            double angleDelta = 42.6; // 74.23; // 22off == 42.6, 41off == 74.23
            double boatDistance = 11.5;
            // 34mph = 15.2778 meters per second
            double len = _pass.GetRopeArcLength(boatDistance, Rope.Off(22).LengthM, angleDelta); // 41off == 24.9, 22off == 23.5
        }

        [TestMethod]
        public void GetBestFitTest()
        {
            CoursePass pass = CoursePassFactory.FromFile(@"..\..\..\..\Video\MetadataExtractor\GOPR0565.json");
            CoursePass best = CoursePassFactory.FitPass(pass.Measurements, pass.Course, pass.Rope);
            double precision = best.GetGatePrecision();

            //Assert.IsTrue(precision == 1.0F);
        }
    }
}
