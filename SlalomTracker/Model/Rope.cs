using System;
using System.Collections.Generic;

namespace SlalomTracker
{
    public class Rope
    {
        /// <summary>
        /// Rope length in meters.
        /// </summary>
        private double m_lengthM;
        
        /// <summary>
        /// Arc length of the rope in meters.
        /// </summary>
        private double m_ropeArcLengthM = double.NaN;

        /// <summary>
        /// Length in meters to the HandleApexRadius. 
        /// </summary>
        private static readonly double HandleApexRadiusM = 11;  

        private double getArcLengthM()
        {
            // We could use arc length (distance) to calculate speed? or just use the radians/sec to calculate this.

            m_ropeArcLengthM = 0.1; // Dummy value.
            return m_ropeArcLengthM;
        }

        public static Rope Off(double ft)
        {
            return new Rope((75 - ft) * 0.3048);
        }

        public static List<Rope> GetStandardLengths()
        {
            List<Rope> ropes = new List<Rope>();
            ropes.Add(Rope.Off(0));
            ropes.Add(Rope.Off(15));
            ropes.Add(Rope.Off(22));
            ropes.Add(Rope.Off(28));
            ropes.Add(Rope.Off(32));
            ropes.Add(Rope.Off(35));
            return ropes;
        }

        public Rope(double lengthM)
        {
            m_lengthM = lengthM;
            getArcLengthM();
        }

        /// <summary>
        /// Returns the rope length in meters.
        /// </summary>
        public double LengthM
        {
            get { return m_lengthM; }
        }

        /// <summary>
        /// Returns the conventional definition of the number of feet off the full 75' rope.
        /// </summary>
        public double FtOff
        {
            get { return 75 - (m_lengthM * 3.28084);  }
        }

        /// <summary>
        /// Readonly rope arc.  
        /// In theory this could be statically stored based on well-known rope lengths.
        /// </summary>
        public double ArcLength
        {
            get { return m_ropeArcLengthM; }
        }

        /// <summary>
        /// Calculates the angle of the rope where the handle will have reached the apex
        /// at the buoy line.
        /// </summary>
        /// <returns></returns>
        public double GetHandleApexDeg()
        {
            // TOOD: this is only valid for 1 buoy line side, we must specify if you want 1,3,5 or 2,4,6 side.

            // Ref: https://www.mathsisfun.com/algebra/trig-finding-angle-right-triangle.html
            // Longer side needs to be the divisor.
            double soa = HandleApexRadiusM > m_lengthM ? m_lengthM / HandleApexRadiusM :
                HandleApexRadiusM / m_lengthM;
            double degrees = Util.RadiansToDegrees(Math.Asin(soa));
            //double degrees = radians * 180 / Math.PI;

            return degrees;
        }


        /// <summary>
        /// Returns the x,y course position of the rope assuming no slack.
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        public CoursePosition GetHandlePosition(double ropeAngleDeg)
        {
            /*
             * Ref: https://www.mathsisfun.com/algebra/trig-solving-asa-triangles.html
             * You know two angles and one side, solve for the other two sides.
             * 
             * a = Y distance from the pilon
             * b = rope length
             * c = X distance from the pilon
             * A = angle at handle (in radians)
             * B = RIGHT angle (in radians) 
             * C = angle at pilon (in radians)
             */
            const double B = 90;
            double C = Math.Abs(ropeAngleDeg);  // Remove any sign.
            double A = B - C;  // A+B+C=180 degrees
            double b = m_lengthM;

            double a = Math.Sin(Util.DegreesToRadians(A)) * b;
            double c = Math.Sin(Util.DegreesToRadians(C)) * b;

            // Determine which buoy line (0 degrees == @buoy246line, 180 degreees == @buoy135line)
            //
            // Determine the side of the boat we're swinging on.
            // Buoy line 2,4,6 is on the left, 1,3,5 is on the right (facing the back of the boat)
            //
            if (ropeAngleDeg < 0)
            {
                c *= -1;
            }

            return new CoursePosition(c, a);
        }

        public override string ToString()
        {
            return string.Format("{0}' off", Math.Round(this.FtOff,0));
        }
    }
}
