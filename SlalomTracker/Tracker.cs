using System;
using System.Collections.Generic;
using System.Text;

namespace SlalomTracker
{
    public class Tracker
    {
        CoursePass m_pass;
        bool m_inCourse; // has the boat pilon entered the course geofenced area.

        /// <summary>
        /// Create a 
        /// </summary>
        public Tracker(Course course, Rope rope)
        {
            m_pass = new CoursePass(course, rope);
            m_pass.CenterLineDegreeOffset = 0; 
            m_pass.Measurements = new List<Measurement>();
        }

        /// <summary>
        /// Determines if the boat is within the course geofenced area.
        /// </summary>
        /// <param name="boatPosition"></param>
        /// <returns></returns>
        private bool IsInCourse(GeoCoordinate boatPosition)
        {
            // dummy value.
            return true;
        }

        /// <summary>
        /// From the rope's compass heading (degress) and the heading from the center line
        /// straight through the course, determines the starting angle of the rope for which
        /// all rope position calculations will be based on.
        /// </summary>
        /// <remarks>
        /// This is required because all rope swing events are reported in radians per second, 
        /// each event is an indication of the direction and speed the rope is swinging, but
        /// without calibration we don't know where centerline started. 
        /// 
        /// A headingDeg 0 inidicates a manual calibration with the rope directly behind the
        /// boat on the center line.
        /// </remarks>
        /// <param name="heading"></param>
        public void CalibrateRopeAngle(double headingDeg)
        {
            // Requires the caller to get the heading from the device.

            if (headingDeg != 0)
            {
                throw new NotImplementedException("CalibrateRopeAngle not yet implemented.");
            }

            m_pass.CenterLineDegreeOffset = 0;
        }

        /// <summary>
        /// Calculates and records an event along the skiers course pass.
        /// </summary>
        public void Track(DateTime timestamp, double ropeSwingRadS, GeoCoordinate boatPosition)
        {
            // Block on this to enure only the first event entering the course gets recorded.
            lock (this)
            {
                if (IsInCourse(boatPosition))
                {
                    if (!m_inCourse)
                    {
                        // Boat pilon is now in the course.
                        m_inCourse = true;
                        m_pass.CourseEntryTimestamp = timestamp;
                    }
                }
                else
                {
                    // record exit time.
                    m_inCourse = false;
                }
            }

            // Calculate measurements.
            Measurement current = new Measurement();
            Measurement previous = m_pass.Measurements.Count > 0 ? m_pass.Measurements[m_pass.Measurements.Count - 1] : null;
            current.BoatPosition = CoursePosition.CoursePositionFromGeo(boatPosition);
            current.RopeSwingSpeedRadS = ropeSwingRadS;

            // All subsequent calculations are based on movement since the last measurement.
            if (previous != null)
            {
                // Time since last event in partial seconds.
                double time = current.Timestamp.Subtract(previous.Timestamp).Milliseconds * 1000;

                // Convert radians per second to degrees per second.  
                current.RopeAngleDegrees = previous.RopeAngleDegrees + 
                    Rope.RadToDeg(ropeSwingRadS) * time;

                // Only interested in the down course speed, if the boat waggled side to 
                // side, it technically could have been going faster.
                double boatDistanceM = current.BoatPosition.Y - previous.BoatPosition.Y;
                current.BoatSpeedMps = boatDistanceM / time;

                // Handle position is calculated relative to the pilon/boat position.
                current.HandlePosition = CoursePosition.Add(current.BoatPosition,
                    m_pass.Rope.GetHandlePosition(current.RopeAngleDegrees));
            }
            else
            {
                current.RopeAngleDegrees = m_pass.CenterLineDegreeOffset;
            }

            m_pass.Measurements.Add(current);
        }
    }
}
