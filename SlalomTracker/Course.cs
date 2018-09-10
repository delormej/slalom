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

        private static List<Course> _knownCourses;

        private List<GeoCoordinate> _polygon;

        /// <summary>
        /// Lat/Long of the pilon as you enter & exit the course.
        /// </summary>
        public GeoCoordinate CourseEntryCL { get; set; }
        public GeoCoordinate CourseExitCL { get; set; }

        static Course()
        {
            _knownCourses = new List<Course>();
            Course cove = new Course(
                    new GeoCoordinate(42.28908285, -71.35913251),
                    new GeoCoordinate(42.28668895, -71.35943147)
                );
            cove.Name = "cove";
            _knownCourses.Add(cove);

            Course outside = // oustide
                new Course(
                    new GeoCoordinate(42.28564438, -71.36237765),
                    new GeoCoordinate(42.28689601, -71.36498477)
                );
            outside.Name = "outside";
            _knownCourses.Add(outside);
        }

        public static Course ByName(string name)
        {
            foreach (Course c in _knownCourses)
                if (c.Name == name)
                    return c;

            throw new ApplicationException("Course name not found.");
        }

        public Course() : this(new GeoCoordinate(), new GeoCoordinate())
        {
        }

        public Course(GeoCoordinate entry, GeoCoordinate exit)
        {
            CourseEntryCL = entry;
            CourseExitCL = exit;

            GenerateCourseFeatures();
            _polygon = GetPolygon();
        }

        /// <summary>
        /// Generates Balls, BoatMarkers, Gates once Course Entry & Exit coordinates are available.
        /// </summary>
        private void GenerateCourseFeatures()
        {
            PreGates = new CoursePosition[4];
            Gates = new CoursePosition[4];

            // Pre Gates (55m)
            PreGates[0] = new CoursePosition((WidthM / 2.0) - 1.25, 0);
            PreGates[1] = new CoursePosition((WidthM / 2.0) + 1.25, 0);
            PreGates[2] = new CoursePosition((WidthM / 2.0) - 1.25, LengthM);
            PreGates[3] = new CoursePosition((WidthM / 2.0) + 1.25, LengthM);

            // Entry Gates
            Gates[0] = new CoursePosition((WidthM / 2.0) - 1.25, 55);
            Gates[1] = new CoursePosition((WidthM / 2.0) + 1.25, 55);

            // Exit Gates
            Gates[2] = new CoursePosition((WidthM / 2.0) - 1.25, LengthM - 55);
            Gates[3] = new CoursePosition((WidthM / 2.0) + 1.25, LengthM - 55);

            BoatMarkers = new CoursePosition[12];
            Balls = new CoursePosition[6];
            Balls[0] = new CoursePosition(0, 27 + 55); // Ball 1
            for (int i = 1; i<6; i++)
            {
                Balls[i] = new CoursePosition(Balls[i-1].X == 0 ? 23 : 0,
                    Balls[i - 1].Y + 41);
            }

            for (int i = 0; i < Balls.Length; i++)
            {
                BoatMarkers[i * 2] = new CoursePosition(11.5 - 1.15, Balls[i].Y);
                BoatMarkers[i * 2 + 1] = new CoursePosition(11.5 + 1.15, Balls[i].Y);
            }
        }

        /// <summary>
        /// Calculates the heading straight through the course from Entry to Exit Center Line.
        /// </summary>
        /// <returns></returns>
        public double GetCourseHeadingDeg()
        {
            return Util.GetHeading(CourseEntryCL, CourseExitCL);
        }

        /// <summary>
        /// Create a list of points that represent the corners of a rectangle inclusive of the pre-gates.
        /// </summary>
        /// <returns></returns>
        public List<GeoCoordinate> GetPolygon()
        {
            double left, right, heading = this.GetCourseHeadingDeg();
            right = (heading + 90 + 360) % 360;
            left = (right + 180) % 360;

            // From the 55's.
            List<GeoCoordinate> poly = new List<GeoCoordinate>(4);
            poly.Add(Util.CalculateDerivedPosition(
                Util.CalculateDerivedPosition(this.CourseEntryCL, 5.0, left), 
                -55, heading));
            poly.Add(Util.CalculateDerivedPosition(
                Util.CalculateDerivedPosition(this.CourseEntryCL, 5.0, right),
                -55, heading));

            poly.Add(Util.CalculateDerivedPosition(
                Util.CalculateDerivedPosition(this.CourseExitCL, 5.0, left),
                55, heading));
            poly.Add(Util.CalculateDerivedPosition(
                Util.CalculateDerivedPosition(this.CourseExitCL, 5.0, right),
                55, heading));
            return poly;
        }

        public void SetCourseEntry(double latitude, double longitude)
        {
            CourseEntryCL.Latitude = latitude;
            CourseEntryCL.Longitude = longitude;
        }

        public void SetCourseExit(double latitude, double longitude)
        {
            CourseExitCL.Latitude = latitude;
            CourseExitCL.Longitude = longitude;

            // TODO validate that it is 259 meters!
            double length = CourseEntryCL.GetDistanceTo(CourseExitCL);

        }

        public static Course FindCourse(List<Measurement> measurements)
        {
            List<Course> courses = new List<Course>();
            courses.Add(Course.ByName("cove"));
            courses.Add(Course.ByName("outside"));

            foreach (var m in measurements)
            {
                foreach (Course course in courses)
                {
                    if (course.IsBoatInCourse(m.BoatGeoCoordinate))
                        return course;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines if the boat is within the course geofenced area, inclusive of pre-gates.
        /// </summary>
        /// <param name="boatPosition"></param>
        /// <returns></returns>
        public bool IsBoatInCourse(GeoCoordinate point)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = _polygon.Count - 1; i < _polygon.Count; j = i++)
            {
                if ((((_polygon[i].Latitude <= point.Latitude) && (point.Latitude < _polygon[j].Latitude))
                        || ((_polygon[j].Latitude <= point.Latitude) && (point.Latitude < _polygon[i].Latitude)))
                        && (point.Longitude < (_polygon[j].Longitude - _polygon[i].Longitude) * (point.Latitude - _polygon[i].Latitude)
                            / (_polygon[j].Latitude - _polygon[i].Latitude) + _polygon[i].Longitude))

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
