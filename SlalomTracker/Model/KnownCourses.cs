using System;
using System.Linq;
using GeoCoordinatePortable;
using System.Collections.Generic;
using SlalomTracker.Cloud;
using Logger = jasondel.Tools.Logger;

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

        /// <summary>
        /// Searches through all measurements to find the first course entry.
        /// </summary>
        public Course FindCourse(List<Measurement> measurements, out Measurement entry55)
        {
            List<KeyValuePair<Course, Measurement>> results = new List<KeyValuePair<Course, Measurement>>();
            Course found = null;
            entry55 = null;

            foreach (Course course in _knownCourses)
            {
                Measurement course55Entry = course.FindEntry55(measurements);
                if (course55Entry != null)
                    results.Add(new KeyValuePair<Course, Measurement>(course, course55Entry));
            }

            if (results.Count() > 1)
            {
                var first = results.OrderBy(kvp => kvp.Value.Timestamp).First();
                found = first.Key;
                entry55 = first.Value;
            }
            else if (results.Count() == 1)
            {
                found = results[0].Key;
                entry55 = results[0].Value;
            }

            return found;
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