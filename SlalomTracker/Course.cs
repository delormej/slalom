using System;
using System.Collections.Generic;
using System.Text;

namespace SlalomTracker
{
    // struct CourseDimensions { double Width = 23; Height = 259; }

    /// <summary>
    /// X,Y coorinates in relative meters to the rectangle that represents the ski course.
    /// https://www.thinkwaterski.com/dox/slalom_tolerances.pdf
    /// Lower left of course is 0,0 meters.  Upper right is 23,259.
    /// </summary>
    public class CoursePosition
    {
        public double X, Y;

        public CoursePosition(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Given the boat's position, calculate in the matrix (x,y) relative to the course. 
        /// Where 0,0 represents Center Line at course entry.
        /// </summary>
        /// <param name="boatPosition"></param>
        /// <returns></returns>
        public static CoursePosition CoursePositionFromGeo(GeoCoordinate boatPosition)
        {
            throw new NotImplementedException();
        }

        // TODO: there is a way to override the default + operator, look this up.
        public static CoursePosition Add(CoursePosition a, CoursePosition b)
        {
            return new CoursePosition(a.X + b.X, a.Y + b.Y);
        }
    }

    public struct GeoCoordinate
    {
        public double Latitude, Longitude;
    }

    /// <summary>
    /// Represents the entry & exit gates for a course and the rectangle that surrounds.
    /// </summary>
    public class Course
    {
        // The default course width and length, there is a possibility that these 
        // are slightly off.
        public static readonly double WidthM = 23;
        public static readonly double LengthM = 259;

        /// <summary>
        /// Lat/Long of the pilon as you enter & exit the course.
        /// </summary>
        public GeoCoordinate CourseEntryCL { get; set; }
        public GeoCoordinate CourseExitCL { get; set; }
    }
    
    /// <summary>
    /// Represents a pass through the course, containing all measurements.
    /// </summary>
    public class CoursePass
    {
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
        }
    }


    /// <summary>
    /// Object to store measurements calculated from a rope rotation event.
    /// </summary>
    public class Measurement
    {
        public DateTime Timestamp { get; set; }

        public CoursePosition BoatPosition { get; set; }

        /// <summary>
        /// Rope swing speed in radians/second.
        /// </summary>
        public double RopeSwingSpeedRadS { get; set; }

        /// <summary>
        /// Current rope angle as it rotates on Y axis aound the ski pilon.
        /// </summary>
        public double RopeAngleDegrees { get; set; }

        public double BoatSpeedMps { get; set; }

        public CoursePosition HandlePosition { get; set; }



        // 1. GPS map entry and exit gates for course, store bearing (degrees) to be used to offset phone/rope bearing at launch.
        // 2. Calculate CL bearing based on gate entry, current bearing
        // 2. Calculate rope swing speed
        // 3. Calculate rope apex
        // 4. Calculate handle position based on degrees and rope length.
        // 5. Calculate the arc of the rope for a given length
        // 6. Calculate x,y position of handle in space.

        //
        // Maintain a virtual course map in x,y (unit==centimeters) coordinates for all course elements (entry/exit gates, balls[1-6], boat markers)
        // 
        // Entry Gate Center Line Position (lat/long)
        // Exit Gate Center Line Position (lat/long)
        // EntryGates[2] (x,y coordinates) 
        // ExitGates[2] (x,y coordiantes)
        // Balls[5] (x,y coordinates)
        // BoatMarkers[10] (x,y coordiantes)
        // BoatGuides[4] -- the course entry / exit guides, what are these called? Green balls..

        // Measurement:
        // Time Stamp
        // Boat Position (Lat/Long), but store x,y position?
        // Boat Speed (this-last position / this-last time)
        // Rope Swing Speed (rad/second)
        // Rope Angle (degrees) -- calculated based on swing speed and last degrees
        // Handle Position (x,y relative to course)


        // We're registering for rotation events, which will come with timestamp & rad/sec.
        // We need to then get the pilon position (lat/long) at that very instant
        // From all of this, we can create a measurement.

        // GPS

    }
}
