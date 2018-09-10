using System;

namespace SlalomTracker
{
    /// <summary>
    /// X,Y coorinates in relative meters to the rectangle that represents the ski course.
    /// https://www.thinkwaterski.com/dox/slalom_tolerances.pdf
    /// Lower left of course is 0,0 meters.  Upper right is 23,369.
    /// Matrix is inclusive of the pregates (55's).
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
        /// Overrides the + operator between two CoursePositions and returns a new one 
        /// representing the sum of both X & Y.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static CoursePosition operator +(CoursePosition a, CoursePosition b)
        {
            return new CoursePosition(a.X + b.X, a.Y + b.Y);
        }

        public static CoursePosition Empty
        {
            get { return new CoursePosition(0, 0); }
        }

        public override string ToString()
        {
            return string.Format("X:{0},Y:{1}", this.X, this.Y);
        }
    }

    //public struct GeoCoordinate
    //{
    //    public double Latitude, Longitude;
    //}
}
