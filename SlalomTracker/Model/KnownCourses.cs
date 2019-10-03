using System;
using GeoCoordinatePortable;
using System.Collections.Generic;
using SlalomTracker.Cloud;

namespace SlalomTracker
{
    public class KnownCourses
    {       
        private static List<Course> _knownCourses;
        private Storage _storage;

        public KnownCourses() 
        {
            // Initialize storage object to be used for loading / saving courses.
            _storage = new Storage();
            if (_knownCourses == null)
                LoadKnownCourses();
        }

        public List<Course> List { get { return _knownCourses; } }

        public Course FindCourse(List<Measurement> measurements)
        {
            foreach (var m in measurements)
            {
                foreach (Course course in _knownCourses)
                {
                    if (course.IsBoatInEntry(m.BoatGeoCoordinate))
                        return course;
                }
            }

            return null;
        }

        public Course ByName(string name)
        {
            foreach (Course c in _knownCourses)
                if (c.Name == name)
                    return c;

            throw new ApplicationException("Course name not found.");
        }

        private void LoadKnownCourses()
        {
            lock(this)
            {
                _knownCourses = _storage.GetCourses();
                // Add reverse for each of the courses.
                List<Course> reverseCourses = new List<Course>();
                foreach (Course c in _knownCourses) 
                    reverseCourses.Add(ReverseCourse(c));
                
                _knownCourses.AddRange(reverseCourses);
            }
        }

        private Course ReverseCourse(Course course)
        {
            Course reverse = new Course(course.Course55ExitCL, course.Course55EntryCL);
            reverse.Name = course.Name + "_reverse";
            return reverse;
        }


        //
        // These functions below should not be needed if we can load from Azure Storage table.
        //

        public void AddKnownCourses()
        {
            _storage.UpdateCourse(GetCoveCourse());
            _storage.UpdateCourse(GetOutisdeCourse());
        }        

        private Course GetCoveCourse()
        {
            Course cove = new Course(
                    new GeoCoordinate(Math.Round(42.28958014, 6), Math.Round(-71.35911924, 6)),
                    new GeoCoordinate(Math.Round(42.28622924, 6), Math.Round(-71.35950488, 6))
                );
            cove.Name = "cove";
            cove.FriendlyName = "Cove";
            
            return cove;
        }

        private Course GetOutisdeCourse()
        {
            var entry = new GeoCoordinate(42.286974, -71.36495);
            var exit = new GeoCoordinate(42.285677, -71.362336);
            double heading = Util.GetHeading(entry, exit);
            double reverse = (heading + 180) % 360;
            var entry55 = Util.MoveTo(entry, 55.0, reverse);
            var exit55 = Util.MoveTo(exit, 55.0, heading);
            Course outside = new Course(entry55, exit55);

            // var Course55EntryCL = new GeoCoordinate(-71.35911924, 42.28958014);
            // var Course55ExitCL = new GeoCoordinate(-71.35950488, 42.28622924);
            // Course outside = new Course(Course55EntryCL, Course55ExitCL);
            outside.Name = "outside";
            outside.FriendlyName = "Outside";

            return outside;
        }

        public GeoCoordinate GetNewCoordinates(string courseName,
            double meters, double heading)
        {
            Course course = ByName(courseName);
            if (course == null)
                throw new ApplicationException($"Unable to find course named {courseName}");
            GeoCoordinate newCoord = Util.MoveTo(course.Course55EntryCL, meters, heading);
            return newCoord;
        }
    }
}