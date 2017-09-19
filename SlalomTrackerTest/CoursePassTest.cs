using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

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
            pass.CourseEntryTimestamp = DateTime.Now.Subtract(TimeSpan.FromSeconds(15));

            // Travel down the course is 259m @ 14m/sec
            // 2 events per second
            // 
            const int eventsPerSecond = 4;
            const int metersPerSecond = 14;
            double courseLengthM = 259 + rope.LengthM;
            int events = ((int)courseLengthM / metersPerSecond) * eventsPerSecond;
            const double ropeRadPerSecond = 0.67;
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
                    if (handlePosX > 23 || handlePosX < 0)
                    {
                        ropeDirection = ropeDirection * -1;
                    }

                    // Exponentially increment speed towards center line.
                    double ropeSpeedFactor = ((Math.Pow(handlePosX - centerLineX, 2) / 100) * ropeRadPerSecond);
                    if (ropeSpeedFactor > (ropeRadPerSecond*0.75)) ropeSpeedFactor = ropeRadPerSecond * 0.75;
                    ropeSpeed = ropeRadPerSecond - ropeSpeedFactor;
                }

                // increment 1 second & 14 meters.
                double longitude = CourseTest.AddDistance(CourseTest.latitude, CourseTest.longitude,
                    (metersPerSecond / eventsPerSecond) * i);
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
        }
    }
}
