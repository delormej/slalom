using System;
using System.Linq;
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
                // Skip if accuracy is not under 500.
                if (m.GpsAccuracy > 500.0) 
                    continue;

                foreach (Course course in _knownCourses)
                {
                    if (course.IsBoatInEntry(m.BoatGeoCoordinate))
                    {
                        const int skipCount = 20;
                        // Ensure that the direction of travel matches Entry -> Exit.
                        int current = measurements.IndexOf(m);
                        if (measurements.Count > current + skipCount)
                        {
                            Measurement nextM = measurements[current + skipCount];
                            double boatHeading = Util.GetHeading(m.BoatGeoCoordinate, nextM.BoatGeoCoordinate);
                            double courseHeading = course.GetCourseHeadingDeg();

                            // within some tolerance
                            const double tolerance = 15.0;
                            if (boatHeading - tolerance <= courseHeading &&
                                boatHeading + tolerance >= courseHeading)
                            {
                                return course;
                            }
                        }
                    }
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

        public GeoCoordinate[] GetNew55Coordinates(string courseName,
            double meters, double heading)
        {
            Course course = ByName(courseName);
            if (course == null)
                throw new ApplicationException($"Unable to find course named {courseName}");
            GeoCoordinate newEntry = Util.MoveTo(course.Course55EntryCL, meters, heading);
            GeoCoordinate newExit = Util.MoveTo(course.Course55ExitCL, meters, heading);
            return new GeoCoordinate[] { newEntry, newExit };
        }
    }
}