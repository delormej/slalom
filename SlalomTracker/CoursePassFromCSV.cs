using System;
using System.IO;

namespace SlalomTracker
{
    public class CoursePassFromCSV
    {
        enum Column { Seconds = 0, Lat, Lon, Speed, Z };

        private static void GetColumn(string[] row, Column column, out double result)
        {
            result = 0;
            double.TryParse(row[(int)column], out result);
        }

        public static CoursePass Load(string path, double centerLineDegreeOffset)
        {
            Course course = new Course();
            course.SetCourseEntry(42.289087, -71.359124);
            course.SetCourseExit(42.287023, -71.359394);
            CoursePass pass = new CoursePass(course, Rope.Off(22), centerLineDegreeOffset);

            using (StreamReader sr = new StreamReader(path))
            {
                string line = sr.ReadLine(); // Advance 1st line header.
                double seconds, lat, lon, z, speed;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] row = line.Split(',');
                    GetColumn(row, Column.Seconds, out seconds);
                    GetColumn(row, Column.Lat, out lat);
                    GetColumn(row, Column.Lon, out lon);
                    GetColumn(row, Column.Speed, out speed);
                    GetColumn(row, Column.Z, out z);
                    DateTime timestamp = DateTime.Now.AddDays(-1).AddSeconds(seconds);
                    pass.Track(timestamp, z, lat, lon, speed);
                }
            }

            return pass;
        }
    }
}
