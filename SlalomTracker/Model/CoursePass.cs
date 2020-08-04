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
        private VideoTime m_time;
        private Measurement m_courseEntry;
        private Measurement m_courseExit;
        private double _offsetX;
        private double _offsetY;

        /// <summary>
        /// First measurement at the 55 entry to the course, NOT actual course entry gates.
        /// </summary>
        public Measurement Entry 
        { 
            get { return m_courseEntry; } 
            internal set { m_courseEntry = value;} 
        }

        /// <summary>
        /// First measurement at the 55 exit of the course, NOT actual course exit gates.
        /// </summary>
        public Measurement Exit 
        { 
            get { return m_courseExit; } 
            internal set { m_courseExit = value; }    
        }

        public List<Measurement> Measurements;

        public Course Course { get; internal set; }

        /// <summary>
        /// Rope length used for the pass.
        /// </summary>
        public Rope Rope { get; internal set; }

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

        public double GpsAccuracy()
        {
            return this.Measurements.Average(m => m.GpsAccuracy);
        }

        /// <summary>
        /// Use CoursePassFactory to create CoursePass object.
        /// </summary>
        internal CoursePass()
        {
            Measurements = new List<Measurement>();
        }

        public void SetOffsets(CoursePosition offset, double centerLineOffset)
        {
            _offsetX = offset.X;
            _offsetY = offset.Y;
            this.CenterLineDegreeOffset = centerLineOffset;
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
        /// Returns the relative boat position X/Y with any potential offsets from GPS coords.
        /// </summary>
        public CoursePosition GetBoatPosition(Measurement current)
        {
            CoursePosition position = this.Course.CoursePositionFromGeo(current.BoatGeoCoordinate);    
            position.X += _offsetX;
            position.Y += _offsetY;  

            return position;
        }

        /// <summary>
        /// Returns a struct that represents the time since the begining of video in fractional seconds
        /// when the boat first goes through the 55s and how long until it goes through exit 55s.
        /// </summary>
        public VideoTime GetVideoTime()
        {
            if (m_time != null)
                return m_time;
            
            m_time = new VideoTime();

            const double DEFAULT_DURATION = 30.0;
            const double GATE_OFFSET_SECONDS = 1.0;

            if (Entry == null)
            {
                Logger.Log("Unable to find boat at 55s, returning start time as 0 seconds.");
                m_time.Start = 0;
            }
            else
            {
                m_time.Start = Entry.Timestamp.TimeOfDay.TotalSeconds >= GATE_OFFSET_SECONDS ? 
                    Entry.Timestamp.TimeOfDay.TotalSeconds - GATE_OFFSET_SECONDS : 0;
            }

            if (Exit == null)
            {
                Logger.Log("Unable to find exit, will use last measurement or default.");
                m_time.Duration = GetSecondsFromEntry(Measurements.Last());
            }
            else
            {
                double exitSeconds = Exit.Timestamp.TimeOfDay.TotalSeconds + GATE_OFFSET_SECONDS;
                if (exitSeconds > Measurements.Last().Timestamp.TimeOfDay.TotalSeconds)
                    exitSeconds = Measurements.Last().Timestamp.TimeOfDay.TotalSeconds;

                m_time.Duration = exitSeconds - m_time.Start;
            }

            if (m_time.Duration > DEFAULT_DURATION)
                m_time.Duration = DEFAULT_DURATION;

            return m_time;
        }

        public double GetSecondsAtEntry55()
        {
            if (m_time == null)
                GetVideoTime();

            return m_time.Start;
        }

        private double GetSecondsFromEntry(Measurement measurement)
        {
            double seconds = 0.0d;
            TimeSpan fromStart = measurement.Timestamp.Subtract(
                Entry.Timestamp);
            if (fromStart != null)
                seconds = fromStart.TotalSeconds;
            return seconds;
        }        
    }
}
