using System;
using GeoCoordinatePortable;
using System.Collections.Generic;
using System.Text;

namespace SlalomTracker
{
    internal class Util
    {
        const double EarthRadius = 6378137.0;

        /// <summary>
        /// Helper methods that coverts radians to degrees since Trig functions return radians.
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static double RadiansToDegrees(double radians)
        {
            return Math.Round(radians * (180.0D / Math.PI), 6);
        }

        /// <summary>
        /// Helper method to convert degrees to radians.
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI/180.0D);
        }

        /// <summary> 
        /// Calculates the new-point from a given source at a given range (meters) and bearing (degrees). . 
        /// </summary> 
        /// <param name="source">Orginal Point</param> 
        /// <param name="range">Range in meters</param> 
        /// <param name="bearing">Bearing in degrees</param> 
        /// <returns>End-point from the source given the desired range and bearing.</returns> 
        public static GeoCoordinate CalculateDerivedPosition(GeoCoordinate source, double range, double bearing)
        {
            double latA = DegreesToRadians(source.Latitude);
            double lonA = DegreesToRadians(source.Longitude);
            double angularDistance = range / EarthRadius;
            double trueCourse = DegreesToRadians(bearing);

            double lat = Math.Asin(Math.Sin(latA) * Math.Cos(angularDistance) + Math.Cos(latA) * Math.Sin(angularDistance) * Math.Cos(trueCourse));

            double dlon = Math.Atan2(Math.Sin(trueCourse) * Math.Sin(angularDistance) * Math.Cos(latA), Math.Cos(angularDistance) - Math.Sin(latA) * Math.Sin(lat));
            double lon = ((lonA + dlon + Math.PI) % (Math.PI * 2)) - Math.PI;

            return new GeoCoordinate(RadiansToDegrees(lat), RadiansToDegrees(lon));
        }

        public static GeoCoordinate Calc(GeoCoordinate source, double distance, double degrees)
        {
            /*
 lat=asin(sin(lat1)*cos(d)+cos(lat1)*sin(d)*cos(tc))
 IF (cos(lat)=0)
    lon=lon1      // endpoint a pole
 ELSE
    lon=mod(lon1-asin(sin(tc)*sin(d)/cos(lat))+pi,2*pi)-pi
 ENDIF                  
             */
            double lat1 = DegreesToRadians(source.Latitude);
            double lon1 = DegreesToRadians(source.Longitude);
            double d = distance;
            double tc = DegreesToRadians(degrees);
            double lon = 0;
            
            double lat=Math.Asin(Math.Sin(lat1)*Math.Cos(d)+Math.Cos(lat1)*Math.Sin(d)*Math.Cos(tc));
            if (Math.Cos(lat)==0)
                lon=lon1;
            else
                lon=(lon1-Math.Asin(Math.Sin(tc)*Math.Sin(d)/Math.Cos(lat))+Math.PI) % ((2*Math.PI)-Math.PI);

            GeoCoordinate destination = new GeoCoordinate(RadiansToDegrees(lat), RadiansToDegrees(lon));
            Console.WriteLine($"CALC source:{source}, destination:{destination}");
            return destination;
        }

        public static GeoCoordinate MoveTo(GeoCoordinate from, double distanceM, double heading)
        {
            double dLat = distanceM * Math.Cos(DegreesToRadians(heading)) / EarthRadius;
            double dLon = distanceM * Math.Sin(DegreesToRadians(heading)) / 
                            (EarthRadius * Math.Cos(DegreesToRadians(from.Latitude)));
            return new GeoCoordinate(from.Latitude + RadiansToDegrees(dLat), 
                from.Longitude + RadiansToDegrees(dLon));
        }

        public static CoursePosition CoursePositionFromGeo(GeoCoordinate boatPosition, Course course)
        {
            double distance = boatPosition.GetDistanceTo(course.Course55EntryCL);
            double boatHeading = Util.GetHeading(course.Course55EntryCL, boatPosition);
            double courseHeading = course.GetCourseHeadingDeg();
            double radiansOffCenter = Util.DegreesToRadians(courseHeading - boatHeading);
            
            // calculate 3 angles
            double deltaRadians = Util.DegreesToRadians(90) - radiansOffCenter;

            // x axis
            double x = distance * (Math.Sin(radiansOffCenter) / Math.Sin(Util.DegreesToRadians(90)));

            // y axis (how far down the course)
            double y = distance * (Math.Sin(deltaRadians) / Math.Sin(Util.DegreesToRadians(90)));

            //Console.WriteLine($"CoursePosition: {boatHeading}, {courseHeading}, {distance}, {x}, {y}");

            return new CoursePosition(x, y);            
        }

        public static double GetHeading(GeoCoordinate start, GeoCoordinate end)
        {
            double dLongitude = Util.DegreesToRadians(end.Longitude -
                start.Longitude);

            double dPhi = Math.Log(
                      Math.Tan(Util.DegreesToRadians(end.Latitude) / 2 + Math.PI / 4) /
                        Math.Tan(Util.DegreesToRadians(start.Latitude) / 2 + Math.PI / 4));

            if (Math.Abs(dLongitude) > Math.PI)
                dLongitude = dLongitude > 0 ? -(2 * Math.PI - dLongitude) : (2 * Math.PI + dLongitude);

            double heading = Util.RadiansToDegrees(Math.Atan2(dLongitude, dPhi));
            if (heading < 0)
                heading = (heading + 360) % 360;

            return heading;
        }
    }
}
