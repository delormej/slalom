using System;
using GeoCoordinatePortable;
using System.Collections.Generic;

namespace SlalomTracker
{
    // struct CourseDimensions { double Width = 23; Height = 259; }
    
    /// <summary>
    /// Represents the entry & exit gates for a course and the rectangle that surrounds.
    /// </summary>
    public class Course
    {
        // The default course width and length, there is a possibility that these 
        // are slightly off.
        public static readonly double WidthM = 23;
        public static readonly double LengthM = 259 + (55 * 2); // Course + pregates
        public static readonly CoursePosition[] Balls;
        public static readonly CoursePosition[] BoatMarkers;
        public static readonly CoursePosition[] Gates;
        public static readonly CoursePosition[] PreGates;
        
        public string Name { get; set; }
        private List<GeoCoordinate> _polygon;
        private List<GeoCoordinate> _entry55Polygon;
        private List<GeoCoordinate> _exit55Polygon;

        /// <summary>
        /// Lat/Long of the pilon as you enter & exit the 55s (pregates) of the course.
        /// </summary>
        public GeoCoordinate Course55EntryCL { get; set; }
        public GeoCoordinate Course55ExitCL { get; set; }
        public string FriendlyName { get; set; }
        public List<GeoCoordinate> Polygon { get { return _polygon; } }
        public List<GeoCoordinate> Entry55Polygon { get { return _entry55Polygon; } }
        public List<GeoCoordinate> Exit55Polygon { get { return _exit55Polygon; } }        
        /// <summary>
        /// Heading straight through the course from Entry to Exit Center Line.
        /// </summary>
        public double CourseHeading { get; private set; }

        static Course()
        {
            PreGates = new CoursePosition[4];
            Gates = new CoursePosition[4];
            BoatMarkers = new CoursePosition[12];
            Balls = new CoursePosition[6];            

            GenerateCourseFeatures();
        }

        public Course() : this(new GeoCoordinate(), new GeoCoordinate())
        {
        }

        public Course(GeoCoordinate entry55, GeoCoordinate exit55)
        {
            Course55EntryCL = entry55;
            Course55ExitCL = exit55;
            CourseHeading = Util.GetHeading(Course55EntryCL, Course55ExitCL);
            GeneratePolygons();
        }

        private delegate bool InPoly(GeoCoordinate geo);

        public Measurement FindEntry55(List<Measurement> measurements)
        {
            return FindMeasurement(IsBoatInEntry55, measurements);
        }

        public Measurement FindExit55(List<Measurement> measurements)
        {
            return FindMeasurement(IsBoatInExit55, measurements);
        }

        private Measurement FindMeasurement(InPoly isBoatIn, List<Measurement> measurements)
        {
            Measurement found = null;

            foreach (Measurement m in measurements)
            {
                if (isBoatIn(m.BoatGeoCoordinate))
                {
                    const int skipCount = 20;
                    // Ensure that the direction of travel matches Entry -> Exit.
                    int current = measurements.IndexOf(m);
                    if (measurements.Count > current + skipCount)
                    {
                        Measurement nextM = measurements[current + skipCount];
                        double boatHeading = Util.GetHeading(m.BoatGeoCoordinate, nextM.BoatGeoCoordinate);

                        // within some tolerance
                        const double tolerance = 15.0;
                        if (boatHeading - tolerance <= CourseHeading &&
                            boatHeading + tolerance >= CourseHeading)
                        {
                            found = m;
                            break;
                        }
                    }
                }
            }

            return found;
        }        

        /// <summary>
        /// Generates Balls, BoatMarkers, Gates once Course Entry & Exit coordinates are available.
        /// </summary>
        private static void GenerateCourseFeatures()
        {
            // Pre Gates (55m)
            PreGates[0] = new CoursePosition(-1.25, 0);
            PreGates[1] = new CoursePosition(1.25, 0);
            PreGates[2] = new CoursePosition(-1.25, LengthM);
            PreGates[3] = new CoursePosition(1.25, LengthM);

            // Entry Gates
            Gates[0] = new CoursePosition(-1.25, 55);
            Gates[1] = new CoursePosition(1.25, 55);

            // Exit Gates
            Gates[2] = new CoursePosition(-1.25, LengthM - 55);
            Gates[3] = new CoursePosition(1.25, LengthM - 55);

            Balls[0] = new CoursePosition(-11.5, 27 + 55); // Ball 1
            for (int i = 1; i<6; i++)
            {
                // Alternate X position (-11.5, 11.5)
                Balls[i] = new CoursePosition(Balls[i-1].X *-1d,
                    Balls[i - 1].Y + 41);
            }

            for (int i = 0; i < Balls.Length; i++)
            {
                BoatMarkers[i * 2] = new CoursePosition(-1.15, Balls[i].Y);
                BoatMarkers[i * 2 + 1] = new CoursePosition(1.15, Balls[i].Y);
            }
        }

        private void GeneratePolygons()
        {
            _polygon = GetCoursePolygon();
            _entry55Polygon = GetGatePolygon(Course55EntryCL);
            _exit55Polygon = GetGatePolygon(Course55ExitCL);
        }

        /// <summary>
        /// Create a list of points that represent the corners of a rectangle inclusive of the pre-gates.
        /// </summary>
        /// <returns></returns>
        private List<GeoCoordinate> GetCoursePolygon()
        {
            double halfWidth = WidthM / 2.0d; // 5.0;
            double left, right;
            right = (CourseHeading + 90 + 360) % 360;
            left = (right + 180) % 360;

            // From the 55's.
            List<GeoCoordinate> poly = new List<GeoCoordinate>(4);
            poly.Add(Util.MoveTo(this.Course55EntryCL, halfWidth, left));
            poly.Add(Util.MoveTo(this.Course55EntryCL, halfWidth, right));
            poly.Add(Util.MoveTo(this.Course55ExitCL, halfWidth, right));
            poly.Add(Util.MoveTo(this.Course55ExitCL, halfWidth, left));

            return poly;
        }

        /// <summary>
        /// Returns a polygon with reference point in the center of start, 1 meter
        /// wider on each side and 1 meter past.
        /// </summary>
        private List<GeoCoordinate> GetGatePolygon(GeoCoordinate reference)
        {
            double halfWidth = WidthM / 2.0d; 
            double left, right;
            right = (CourseHeading + 90 + 360) % 360;
            left = (right + 180) % 360;
            
            GeoCoordinate GateCL = reference;
            GeoCoordinate LeftTop = Util.MoveTo(GateCL, halfWidth, left);
            GeoCoordinate RightTop = Util.MoveTo(GateCL, halfWidth, right);
            GeoCoordinate RightBottom = Util.MoveTo(
                Util.MoveTo(GateCL, 1.0, CourseHeading),
                halfWidth, right);
            GeoCoordinate LeftBottom = Util.MoveTo(
                Util.MoveTo(GateCL, 1.0, CourseHeading),
                halfWidth, left);

            List<GeoCoordinate> poly = new List<GeoCoordinate>(4);
            poly.Add(LeftTop);
            poly.Add(RightTop);
            poly.Add(RightBottom);
            poly.Add(LeftBottom);

            return poly;
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
            double distance = boatPosition.GetDistanceTo(Course55EntryCL);
            double boatHeading = Util.GetHeading(Course55EntryCL, boatPosition);
            double radiansOffCenter = Util.DegreesToRadians(CourseHeading - boatHeading);
            
            // calculate 3 angles
            double deltaRadians = Util.DegreesToRadians(90) - radiansOffCenter;

            // x axis
            double x = distance * (Math.Sin(radiansOffCenter) / Math.Sin(Util.DegreesToRadians(90)));

            // y axis (how far down the course)
            double y = distance * (Math.Sin(deltaRadians) / Math.Sin(Util.DegreesToRadians(90)));

            //Console.WriteLine($"CoursePosition: {boatHeading}, {courseHeading}, {distance}, {x}, {y}");

            return new CoursePosition(x, y);            
        }

        /// <summary>
        /// Determines if the boat is within the course geofenced area, inclusive of pre-gates (55s).
        /// </summary>
        /// <param name="boatPosition"></param>
        /// <returns></returns>
        public bool IsBoatInCourse(GeoCoordinate point)
        {
            return IsPointInPoly(point, _polygon);
        }

        public bool IsBoatInEntry55(GeoCoordinate point)
        {
            return IsPointInPoly(point, _entry55Polygon);
        }

        public bool IsBoatInExit55(GeoCoordinate point)
        {
            return IsPointInPoly(point, _exit55Polygon);
        }

        private bool IsPointInPoly(GeoCoordinate point, List<GeoCoordinate> polygon)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if ((((polygon[i].Latitude <= point.Latitude) && (point.Latitude < polygon[j].Latitude))
                        || ((polygon[j].Latitude <= point.Latitude) && (point.Latitude < polygon[i].Latitude)))
                        && (point.Longitude < (polygon[j].Longitude - polygon[i].Longitude) * (point.Latitude - polygon[i].Latitude)
                            / (polygon[j].Latitude - polygon[i].Latitude) + polygon[i].Longitude))

                    c = !c;
            }

            return c;
        }

        //
        // Maintain a virtual course map in x,y coordinates for all course elements (entry/exit gates, balls[1-6], boat markers)
        // 
        // Entry Gate Center Line Position (lat/long)
        // Exit Gate Center Line Position (lat/long)
        // EntryGates[2] (x,y coordinates) 
        // ExitGates[2] (x,y coordiantes)
        // Balls[5] (x,y coordinates)
        // BoatMarkers[10] (x,y coordiantes)
        // BoatGuides[4] -- the course entry / exit guides, what are these called? Green balls..
    }
}
