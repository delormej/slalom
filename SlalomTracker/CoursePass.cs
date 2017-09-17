using GeoCoordinatePortable;
using System;
using System.Collections.Generic;

namespace SlalomTracker
{
    /// <summary>
    /// Represents a pass through the course, containing all measurements.
    /// </summary>
    public class CoursePass
    {
        // Flag if the boat pilon entered the course geofenced area.
        bool m_inCourse; 

        public List<Measurement> Measurements;

        public DateTime CourseEntryTimestamp { get; set; }
        public DateTime CourseExitTimestamp { get; set; }

        public Course Course { get; private set; }

        /// <summary>
        /// Rope length used for the pass.
        /// </summary>
        public Rope Rope { get; private set; }

        /// <summary>
        /// Average boat speed (i.e. 30.4,32.3,34.2,36 mph) for the course.
        /// </summary>
        public double AverageBoatSpeed { get; }

        /// <summary>
        /// Calibration offset in degrees from which rope angle is calculated from.  
        /// This value represents the heading in degrees which should represent the center line.
        /// </summary>
        public double CenterLineDegreeOffset { get; set; }

        public CoursePass(Course course, Rope rope)
        {
            this.Course = course;
            this.Rope = rope;

            CenterLineDegreeOffset = 0;
            Measurements = new List<Measurement>();
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

            CenterLineDegreeOffset = 0;
        }

        /// <summary>
        /// Calculates and records an event along the skiers course pass.
        /// </summary>
        public void Track(DateTime timestamp, double ropeSwingRadS, double latitude, double longitude)
        {
            GeoCoordinate boatPosition = new GeoCoordinate(latitude, longitude);
            Track(timestamp, ropeSwingRadS, boatPosition);
        }

        /// <summary>
        /// Calculates and records an event along the skiers course pass, using GeoCoordinate.
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
                        CourseEntryTimestamp = timestamp;
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
            Measurement previous = Measurements.Count > 0 ? Measurements[Measurements.Count - 1] : null;
            current.BoatPosition = Course.CoursePositionFromGeo(boatPosition);
            current.RopeSwingSpeedRadS = ropeSwingRadS;

            // All subsequent calculations are based on movement since the last measurement.
            if (previous != null)
            {
                // Time since last event in partial seconds.
                double time = current.Timestamp.Subtract(previous.Timestamp).Milliseconds * 1000;

                // Convert radians per second to degrees per second.  
                current.RopeAngleDegrees = previous.RopeAngleDegrees +
                    Util.RadToDeg(ropeSwingRadS) * time;

                // Only interested in the down course speed, if the boat waggled side to 
                // side, it technically could have been going faster.
                double boatDistanceM = current.BoatPosition.Y - previous.BoatPosition.Y;
                current.BoatSpeedMps = boatDistanceM / time;

                // Handle position is calculated relative to the pilon/boat position.
                current.HandlePosition = current.BoatPosition +
                    Rope.GetHandlePosition(current.RopeAngleDegrees);
            }
            else
            {
                current.RopeAngleDegrees = CenterLineDegreeOffset;
            }

            Measurements.Add(current);
        }
    }
}
