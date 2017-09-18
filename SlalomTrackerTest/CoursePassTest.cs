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

            for (int i = 0; i < 30; i++)
            {
                int ropeDirection = 1; 
                if (i > 0)
                {
                    double ropeAngle = pass.Measurements[pass.Measurements.Count - 1].RopeAngleDegrees;

                    // Invert the direction of the rope when you hit the apex.
                    if (ropeAngle > rope.GetHandleApexDeg())
                    {
                        ropeDirection = -1;
                    }
                    else
                    {
                        ropeDirection = 1;
                    }
                }

                // increment 1 second & 14 meters.
                double distanceM = CourseTest.AddDistance(CourseTest.latitude, CourseTest.longitude, 14 * i);
                pass.Track(pass.CourseEntryTimestamp.AddSeconds(1), (0.5 * ropeDirection), 
                    CourseTest.latitude, distanceM);
            }

            foreach (var m in pass.Measurements)
            {
                Trace.WriteLine(string.Format("X: {0}, Y: {1}", m.HandlePosition.X, m.HandlePosition.Y));
            }
        }
    }
}
