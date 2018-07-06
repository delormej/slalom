using System;
using GeoCoordinatePortable;
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

        const double EarthRadius = 6378137.0;
        const double DegreesToRadians = 0.0174532925;
        const double RadiansToDegrees = 57.2957795;
        /// <summary> 
        /// Calculates the new-point from a given source at a given range (meters) and bearing (degrees). . 
        /// </summary> 
        /// <param name="source">Orginal Point</param> 
        /// <param name="range">Range in meters</param> 
        /// <param name="bearing">Bearing in degrees</param> 
        /// <returns>End-point from the source given the desired range and bearing.</returns> 
        public static GeoCoordinate CalculateDerivedPosition(GeoCoordinate source, double range, double bearing)
        {
            double latA = source.Latitude * DegreesToRadians;
            double lonA = source.Longitude * DegreesToRadians;
            double angularDistance = range / EarthRadius;
            double trueCourse = bearing * DegreesToRadians;

            double lat = Math.Asin(Math.Sin(latA) * Math.Cos(angularDistance) + Math.Cos(latA) * Math.Sin(angularDistance) * Math.Cos(trueCourse));

            double dlon = Math.Atan2(Math.Sin(trueCourse) * Math.Sin(angularDistance) * Math.Cos(latA), Math.Cos(angularDistance) - Math.Sin(latA) * Math.Sin(lat));
            double lon = ((lonA + dlon + Math.PI) % (Math.PI * 2)) - Math.PI;

            return new GeoCoordinate(lat * RadiansToDegrees, lon * RadiansToDegrees);
        }

        public static double GetHeading(GeoCoordinate start, GeoCoordinate end)
        {
            double dLongitude = Util.DegToRad(end.Longitude -
                start.Longitude);

            double dPhi = Math.Log(
                      Math.Tan(Util.DegToRad(end.Latitude) / 2 + Math.PI / 4) /
                        Math.Tan(Util.DegToRad(start.Latitude) / 2 + Math.PI / 4));

            if (Math.Abs(dLongitude) > Math.PI)
                dLongitude = dLongitude > 0 ? -(2 * Math.PI - dLongitude) : (2 * Math.PI + dLongitude);

            double heading = Util.RadToDeg(Math.Atan2(dLongitude, dPhi));

            return heading;
        }
    }
}
