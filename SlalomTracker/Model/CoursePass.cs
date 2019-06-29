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
        bool m_entered55s; 

        private Measurement m_courseEntry;
        private Measurement m_courseExit;

        public Measurement Entry { get { return m_courseEntry; } }
        public Measurement Exit { get { return m_courseExit; } }

        public List<Measurement> Measurements;

        public Course Course { get; private set; }

        /// <summary>
        /// Rope length used for the pass.
        /// </summary>
        public Rope Rope { get; private set; }

        /// <summary>
        /// Average boat speed (i.e. 30.4,32.3,34.2,36 mph) for the course.
        /// </summary>
        public double AverageBoatSpeed { get; private set; }

        const double MPS_TO_MPH = 2.23694d;

        /// <summary>
        /// Calibration offset in degrees from which rope angle is calculated from.  
        /// This value represents the heading in degrees which should represent the center line.
        /// </summary>
        public double CenterLineDegreeOffset { get; set; }

        public CoursePass(Course course, Rope rope) : this(course, rope, 0)
        {
        }

        public CoursePass(Course course, Rope rope, double CenterLineDegreeOffset)
        {
            this.Course = course;
            this.Rope = rope;
            this.CenterLineDegreeOffset = CenterLineDegreeOffset;
            Measurements = new List<Measurement>();
        }

        /// <summary>
        /// Given the boat's position, calculate in the matrix (x,y) relative to the course. 
        /// </summary>
        /// <param name="boatPosition"></param>
        /// <returns></returns>
        public CoursePosition CoursePositionFromGeo(double latitude, double longitude)
        {
            return CoursePositionFromGeo(new GeoCoordinate(latitude, longitude));
        }

        /// <summary>
        /// Given the boat's position, calculate in the matrix (x,y) relative to the course. 
        /// Where 0,0 is center line of pre-gates.
        /// </summary>
        /// <param name="boatPosition"></param>
        /// <returns></returns>
        public CoursePosition CoursePositionFromGeo(GeoCoordinate boatPosition)
        {
            // TODO: with the exception of this entered check, everything else seems like logic that should live in
            // the Course class? Evaluate this.
            if (!m_entered55s)
                return CoursePosition.Empty;
            else
                return Util.CoursePositionFromGeo(boatPosition, Course);
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

        public void Track(DateTime timestamp, double ropeSwingRadS, double latitude, double longitude, double speed)
        {
            GeoCoordinate boatPosition = new GeoCoordinate(latitude, longitude);
            boatPosition.Speed = speed;
            Track(timestamp, ropeSwingRadS, boatPosition);
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
            Measurement current = new Measurement();
            current.BoatGeoCoordinate = boatPosition;
            current.RopeSwingSpeedRadS = ropeSwingRadS;
            current.Timestamp = timestamp;
            current.BoatSpeedMps = boatPosition.Speed; // TODO: this is redundant, because it's now in BoatGeoCoordinate.
            Track(current);
        }

        public void Track(Measurement current)
        { 
            current.InCourse = this.Course.IsBoatInCourse(current.BoatGeoCoordinate);

            // Block on this to enure only the first event entering the course gets recorded.
            lock (this)
            {
                if (!m_entered55s && current.InCourse)
                {
                    m_entered55s = true;
                }

                if (m_courseEntry == null && 
                        this.Course.IsBoatInEntry(current.BoatGeoCoordinate))
                {
                    m_courseEntry = current;
                }
                else if (m_courseExit == null &&
                        this.Course.IsBoatInExit(current.BoatGeoCoordinate))
                {
                    m_courseExit = current;
                    CalculateCoursePassSpeed();
                }
            }

            // Calculate measurements.
            Measurement previous = Measurements.Count > 0 ? Measurements[Measurements.Count - 1] : null;
            current.BoatPosition = CoursePositionFromGeo(current.BoatGeoCoordinate);
            if (current.BoatPosition == CoursePosition.Empty)
                return;

            double ropeArcLength = 0;

            // All subsequent calculations are based on movement since the last measurement.
            if (previous != null)
            {
                // Time since last event in partial seconds.
                double seconds = current.Timestamp.Subtract(previous.Timestamp).TotalSeconds;

                // Convert radians per second to degrees per second.  
                current.RopeAngleDegrees = previous.RopeAngleDegrees +
                    Util.RadiansToDegrees(current.RopeSwingSpeedRadS * seconds);
                ropeArcLength = GetRopeArcLength(current, previous);
            }
            else
            {
                current.RopeAngleDegrees = CenterLineDegreeOffset;
            }

            // Get handle position in x,y coordinates from the pilon.
            CoursePosition virtualHandlePos = Rope.GetHandlePosition(current.RopeAngleDegrees);

            // Actual handle position is calculated relative to the pilon/boat position, behind the boat.
            double y = current.BoatPosition.Y - virtualHandlePos.Y;
            double x = current.BoatPosition.X - virtualHandlePos.X;
            current.HandlePosition = new CoursePosition(x, y);
            Measurements.Add(current);
        }

        public double GetRopeArcLength(Measurement current, Measurement previous)
        {
            // Calculate linear distance from previous.X,Y (A) to current.X,Y (B)
            // In the center, draw a perpendicular line to a point Y away (C)
            // Create a triangle between A->C, C->B
            // Calculate the angle at C

            // This is incorrect.
            //double distance = current.BoatPosition.X - previous.BoatPosition.X;
            //double angleDelta = Math.Abs(current.RopeAngleDegrees - previous.RopeAngleDegrees);
            //return GetRopeArcLength(distance, Rope.LengthM, angleDelta);
            return 0;
        }

        public double GetRopeArcLength(double boatDistance, double ropeLengthM, double angleDelta)
        {
            double radius = ropeLengthM;
            double arcLength = (angleDelta / 360) * 2 * Math.PI * radius;
            double distance = arcLength + boatDistance;
            return distance;
        }

        public double GetGatePrecision()
        {
            var entryM = FindHandleAtY(Course.Gates[0].Y);
            var exitM = FindHandleAtY(Course.Gates[3].Y);
            if (entryM == null || exitM == null)
                return double.MaxValue;

            // Get the differene between
            double dEntry = Math.Pow(Math.Abs(entryM.HandlePosition.X - 11.5), 2);
            double dExit = Math.Pow(Math.Abs(exitM.HandlePosition.X - 11.5), 2);

            return Math.Sqrt(dEntry + dExit);
        }

        public double GetSecondsAtEntry()
        {
            double seconds = 0.0d;
            TimeSpan fromStart = this.Entry.Timestamp.Subtract(
                this.Measurements[0].Timestamp);
            if (fromStart != null)
                seconds = fromStart.TotalSeconds;
            return seconds;
        }

        public double GetDurationSeconds()
        {
            if (this.Exit == null)
                return 0.0d;
            
            double start = GetSecondsAtEntry();
            double duration = this.Exit.Timestamp.Subtract(
                this.Entry.Timestamp).TotalSeconds;

            return duration;
        }

        public double GetTotalSeconds()
        {
            int count = Measurements.Count-1;
            double duration = this.Measurements[count].Timestamp.Subtract(
                this.Measurements[0].Timestamp).TotalSeconds;
            return duration;
        }
    
        private Measurement FindHandleAtY(double y)
        {
            foreach (var m in this.Measurements)
                if ((int)m.HandlePosition.Y == (int)y)
                    return m;
            return null;
        }

        private void CalculateCoursePassSpeed()
        {
            TimeSpan duration = m_courseExit.Timestamp.Subtract(m_courseEntry.Timestamp);
            double distance = m_courseExit.BoatGeoCoordinate.GetDistanceTo(
                m_courseEntry.BoatGeoCoordinate);
            
            if (duration == null || duration.Seconds <= 0 || distance <= 0)
            {
                throw new ApplicationException("Could not calculate time and distance for course entry/exit.");
            }

            double speedMps = distance / duration.TotalSeconds;
            AverageBoatSpeed = Math.Round(speedMps * MPS_TO_MPH, 1);
        }
    }
}
