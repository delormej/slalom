using System;
using System.Collections.Generic;
using System.Text;

namespace SlalomTracker
{
    internal class Util
    {
        /// <summary>
        /// Helper methods that coverts radians to degrees since Trig functions return radians.
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        internal static double RadToDeg(double rad)
        {
            return rad * 180 / Math.PI;
        }

        /// <summary>
        /// Helper method to convert degrees to radians.
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        internal static double DegToRad(double deg)
        {
            return deg / 180 * Math.PI;
        }
    }
}
