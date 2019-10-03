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

        private List<GeoCoordinate> _polygon;
        private List<GeoCoordinate> _entryPolygon;
        private List<GeoCoordinate> _exitPolygon;

        /// <summary>
        /// Lat/Long of the pilon as you enter & exit the 55s (pregates) of the course.
        /// </summary>
        public GeoCoordinate Course55EntryCL { get; set; }
        public GeoCoordinate Course55ExitCL { get; set; }
        public string FriendlyName { get; set; }
        public List<GeoCoordinate> Polygon { get { return _polygon; } }
        public List<GeoCoordinate> EntryPolygon { get { return _entryPolygon; } }
        public List<GeoCoordinate> ExitPolygon { get { return _exitPolygon; } }

        public Course() : this(new GeoCoordinate(), new GeoCoordinate())
        {
        }

        public Course(GeoCoordinate entry, GeoCoordinate exit)
        {
            Course55EntryCL = entry;
            Course55ExitCL = exit;
            double length = Course55EntryCL.GetDistanceTo(Course55ExitCL);
            Console.WriteLine("Course length: " + length);
            // 3 meter tolerance?
            //if (length > (LengthM + 3) ||
            //        length < (LengthM - 3))
            //    throw new ApplicationException(string.Format(@"Course length: {0} is not within tolerance.", length));

            GenerateCourseFeatures();
            GeneratePolygons();
        }

        /// <summary>
        /// Generates Balls, BoatMarkers, Gates once Course Entry & Exit coordinates are available.
        /// </summary>
        private void GenerateCourseFeatures()
        {
            PreGates = new CoursePosition[4];
            Gates = new CoursePosition[4];

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

            BoatMarkers = new CoursePosition[12];
            Balls = new CoursePosition[6];
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

        /// <summary>
        /// Calculates the heading straight through the course from Entry to Exit Center Line.
        /// </summary>
        /// <returns></returns>
        public double GetCourseHeadingDeg()
        {
            return Util.GetHeading(Course55EntryCL, Course55ExitCL);
        }

        private void GeneratePolygons()
        {
            //double reverseHeading = (GetCourseHeadingDeg() + 180) % 360;
            _polygon = GetCoursePolygon();
            _entryPolygon = GetEntryGatePolygon(); //Get55mPolygon(Course55EntryCL, GetCourseHeadingDeg());
            _exitPolygon = GetExitGatePolygon();//Get55mPolygon(Course55ExitCL, reverseHeading);
        }

        /// <summary>
        /// Create a list of points that represent the corners of a rectangle inclusive of the pre-gates.
        /// </summary>
        /// <returns></returns>
        private List<GeoCoordinate> GetCoursePolygon()
        {
            double halfWidth = WidthM / 2.0d; // 5.0;
            double left, right, heading = this.GetCourseHeadingDeg();
            right = (heading + 90 + 360) % 360;
            left = (right + 180) % 360;

            // From the 55's.
            List<GeoCoordinate> poly = new List<GeoCoordinate>(4);
            poly.Add(Util.MoveTo(this.Course55EntryCL, halfWidth, left));
            poly.Add(Util.MoveTo(this.Course55EntryCL, halfWidth, right));
            poly.Add(Util.MoveTo(this.Course55ExitCL, halfWidth, right));
            poly.Add(Util.MoveTo(this.Course55ExitCL, halfWidth, left));
            Console.WriteLine($"CourseGeo, TopLeft:{poly[0]}, TopRight:{poly[1]}, BottomRight:{poly[2]}, BottomLeft:{poly[3]}");
            return poly;
        }

        private List<GeoCoordinate> GetEntryGatePolygon()
        {
            double halfWidth = WidthM / 2.0d; 
            double left, right, heading = this.GetCourseHeadingDeg();
            right = (heading + 90 + 360) % 360;
            left = (right + 180) % 360;
            
            GeoCoordinate GateCL = Util.MoveTo(this.Course55EntryCL, 55.0, heading);
            GeoCoordinate LeftTop = Util.MoveTo(GateCL, halfWidth, left);
            GeoCoordinate RightTop = Util.MoveTo(GateCL, halfWidth, right);
            GeoCoordinate RightBottom = Util.MoveTo(
                Util.MoveTo(GateCL, 1.0, heading),
                halfWidth, right);
            GeoCoordinate LeftBottom = Util.MoveTo(
                Util.MoveTo(GateCL, 1.0, heading),
                halfWidth, left);

            List<GeoCoordinate> poly = new List<GeoCoordinate>(4);
            poly.Add(LeftTop);
            poly.Add(RightTop);
            poly.Add(RightBottom);
            poly.Add(LeftBottom);

            return poly;
        }

        private List<GeoCoordinate> GetExitGatePolygon()
        {
            double halfWidth = WidthM / 2.0d; 
            double left, right;
            double reverseHeading = (GetCourseHeadingDeg() + 180) % 360;
            right = (reverseHeading + 90 + 360) % 360;
            left = (right + 180) % 360;
            
            GeoCoordinate GateCL = Util.MoveTo(this.Course55ExitCL, 55.0, reverseHeading);
            GeoCoordinate LeftTop = Util.MoveTo(GateCL, halfWidth, left);
            GeoCoordinate RightTop = Util.MoveTo(GateCL, halfWidth, right);
            GeoCoordinate RightBottom = Util.MoveTo(
                Util.MoveTo(GateCL, 1.0, reverseHeading),
                halfWidth, right);
            GeoCoordinate LeftBottom = Util.MoveTo(
                Util.MoveTo(GateCL, 1.0, reverseHeading),
                halfWidth, left);

            List<GeoCoordinate> poly = new List<GeoCoordinate>(4);
            poly.Add(LeftTop);
            poly.Add(RightTop);
            poly.Add(RightBottom);
            poly.Add(LeftBottom);

            return poly;
        }

        /// <summary>
        /// Returns a polygon as wide as the course and 1m long, relative to the reference coordinate.
        /// </summary>
        private List<GeoCoordinate> Get55mPolygon(GeoCoordinate reference, double heading)
        {
            double left, right;
            right = (heading + 90 + 360) % 360;
            left = (right + 180) % 360;

            // From the 55's.
            List<GeoCoordinate> poly = new List<GeoCoordinate>(4);
            // Create a poly that is 1 meter long, 5 meters wide.
            
            // Course entry is 55m from the reference passed in.
            GeoCoordinate entryCL = Util.MoveTo(reference, 54.5, heading);
            
            poly.Add(Util.MoveTo(entryCL, 5.0, left));
            poly.Add(Util.MoveTo(entryCL, 5.0, right));
            poly.Add(Util.MoveTo(
                Util.MoveTo(entryCL, 1.0, heading), 5.0, right));
            poly.Add(Util.MoveTo(
                Util.MoveTo(entryCL,  1.0, heading), 5.0, left));

            return poly;
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

        public bool IsBoatInEntry(GeoCoordinate point)
        {
            return IsPointInPoly(point, _entryPolygon);
        }

        public bool IsBoatInExit(GeoCoordinate point)
        {
            return IsPointInPoly(point, _exitPolygon);
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

        public string Name { get; set; }

        public CoursePosition[] Balls { get; private set; }

        public CoursePosition[] BoatMarkers { get; private set; }

        public CoursePosition[] Gates { get; private set; }

        public CoursePosition[] PreGates { get; private set; }
    }
}
