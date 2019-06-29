using System;

namespace SlalomTracker
{
    /// <summary>
    /// X,Y coorinates in relative meters to the rectangle that represents the ski course.
    /// Upper left of course is -23,0 meters.  Lower right is 23,369.
    /// 0,0 represents the center line of the pregate (55's).
    /// https://www.thinkwaterski.com/dox/slalom_tolerances.pdf
    /// </summary>
    public class CoursePosition
    {
        public double X, Y;

        public CoursePosition(double x, double y)
        {
            // if (x < -23 || x > 23 || y < 0 || y > 369)
            // {
            //     Console.WriteLine($"CoursePosition x:{x}, y:{y} are invalid.  Range must be (-23,0) to (23,369).  " +
            //         "Upper left of course is -23,0 meters.  Lower right is 23,369.  " +
            //         "Where 0,0 represents the center line of the pregate (55's).");
            // }
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
}
