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

        public List<Measurement> Measurements { get; internal set; }

        public Course Course { get; internal set; }

        /// <summary>
        /// Rope length used for the pass.
        /// </summary>
        public Rope Rope { get; internal set; }

        public VideoTime VideoTime { get; set; }

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

        public double GetSecondsAtEntry55()
        {
            return VideoTime?.Start ?? 0;
        }        
    }
}
