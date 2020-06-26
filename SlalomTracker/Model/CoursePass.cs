using GeoCoordinatePortable;
using System;
using System.Collections.Generic;
using System.Linq;
using SlalomTracker.Video;
using Logger = jasondel.Tools.Logger;

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

        public Measurement Entry 
        { 
            get { return m_courseEntry; } 
            internal set { m_courseEntry = value;} 
        }
        
        public Measurement Exit 
        { 
            get { return m_courseExit; } 
            internal set { m_courseExit = value; }    
        }

        public List<Measurement> Measurements;

        public Course Course { get; private set; }

        /// <summary>
        /// Rope length used for the pass.
        /// </summary>
        public Rope Rope { get; private set; }

        /// <summary>
        /// Average boat speed (i.e. 30.4,32.3,34.2,36 mph) for the course.
        /// </summary>
        public double AverageBoatSpeed { get; internal set; }

        public const double MPS_TO_MPH = 2.23694d;

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

        public void Track(Measurement current)
        { 
            if (!m_entered55s && this.Course.IsBoatInCourse(current.BoatGeoCoordinate))
                m_entered55s = true;
                
            Measurements.Add(current);
        }

        public double GetRopeArcLength(Measurement current, Measurement previous)
        {
            // Calculate linear distance from previous.X,Y (A) to current.X,Y (B)
            // In the center, draw a perpendicular line to a point Y away (C)
            // Create a triangle between A->C, C->B
            // Calculate the angle at C

            double distance = current.BoatPosition.X - previous.BoatPosition.X;
            double angleDelta = Math.Abs(current.RopeAngleDegrees - previous.RopeAngleDegrees);
            return GetRopeArcLength(distance, Rope.LengthM, angleDelta);
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
            var entryM = this.Measurements.FindHandleAtY(Course.Gates[0].Y);
            var exitM = this.Measurements.FindHandleAtY(Course.Gates[3].Y);
            if (entryM == null || exitM == null)
                return double.MaxValue;

            // Get the differene between
            double dEntry = Math.Pow(Math.Abs(entryM.HandlePosition.X), 2);
            double dExit = Math.Pow(Math.Abs(exitM.HandlePosition.X), 2);

            return Math.Sqrt(dEntry + dExit);
        }

        /// <summary>
        /// Returns a struct that represents the time since the begining of video in fractional seconds
        /// when the boat first goes through the 55s and how long until it goes through exit 55s.
        /// </summary>
        public VideoTime GetVideoTime()
        {
            VideoTime time = new VideoTime();

            const double MAGIC_JUST_PAST_55s = 0.1;   // Has to be > 0
            const double DEFAULT_DURATION = 30.0;

            Measurement start = Measurements.FindBoatAtY(MAGIC_JUST_PAST_55s); 
            Measurement exit = Measurements.FindHandleAtY(Course.PreGates[3].Y);

            if (start == null)
            {
                Logger.Log("Unable to find boat at 55s, returning start time as 0 seconds.");
                time.Start = 0;
            }
            else
            {
                // Hack to get the total seconds.
                time.Start = new TimeSpan(start.Timestamp.Ticks).TotalSeconds;
            }

            if (exit == null)
            {
                Logger.Log("Unable to find exit, will use last measurement or default.");
                time.Duration = GetSecondsFromStart(Measurements.Last());
            }
            else
            {
                time.Duration = exit.Timestamp.Subtract(start.Timestamp).TotalSeconds;
            }

            if (time.Duration > DEFAULT_DURATION)
                time.Duration = DEFAULT_DURATION;

            return time;
        }

        public double GetSecondsAtEntry()
        {
            return GetSecondsFromStart(this.Entry);
        }

        private double GetSecondsFromStart(Measurement measurement)
        {
            double seconds = 0.0d;
            TimeSpan fromStart = measurement.Timestamp.Subtract(
                this.Measurements[0].Timestamp);
            if (fromStart != null)
                seconds = fromStart.TotalSeconds;
            return seconds;
        }        
    }
}
