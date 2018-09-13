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
        /// Lat/Long of the pilon as you enter & exit the 55s (pregates) of the course.
        /// </summary>
        public GeoCoordinate Course55EntryCL { get; set; }
        public GeoCoordinate Course55ExitCL { get; set; }
        public List<GeoCoordinate> Polygon { get { return _polygon; } }

        static Course()
        {
            _knownCourses = new List<Course>();
            Course cove = new Course(
                    new GeoCoordinate(42.28958014, -71.35911924),
                    new GeoCoordinate(42.28622924, -71.35950488)
                );
            cove.Name = "cove";
            _knownCourses.Add(cove);

            Course outside = // oustide
                new Course(
                    new GeoCoordinate(42.28530973, -71.36180685),
                    new GeoCoordinate(42.28721409, -71.36553574)
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
            Course55EntryCL = entry;
            Course55ExitCL = exit;
            double length = Course55EntryCL.GetDistanceTo(Course55ExitCL);

            // 3 meter tolerance?
            //if (length > (LengthM + 3) ||
            //        length < (LengthM - 3))
            //    throw new ApplicationException(string.Format(@"Course length: {0} is not within tolerance.", length));

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
            return Util.GetHeading(Course55EntryCL, Course55ExitCL);
        }

        /// <summary>
        /// Create a list of points that represent the corners of a rectangle inclusive of the pre-gates.
        /// </summary>
        /// <returns></returns>
        private List<GeoCoordinate> GetPolygon()
        {
            double left, right, heading = this.GetCourseHeadingDeg();
            right = (heading + 90 + 360) % 360;
            left = (right + 180) % 360;

            // From the 55's.
            List<GeoCoordinate> poly = new List<GeoCoordinate>(4);
            poly.Add(Util.CalculateDerivedPosition(this.Course55EntryCL, 5.0, left));
            poly.Add(Util.CalculateDerivedPosition(this.Course55EntryCL, 5.0, right));
            poly.Add(Util.CalculateDerivedPosition(this.Course55ExitCL, 5.0, left));
            poly.Add(Util.CalculateDerivedPosition(this.Course55ExitCL, 5.0, right));
            return poly;
        }


        public static Course FindCourse(List<Measurement> measurements)
        {
            foreach (var m in measurements)
            {
                foreach (Course course in _knownCourses)
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
