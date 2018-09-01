using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

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

        public static CoursePass Load(string path)
        {
            return Load(path, 0, Rope.Off(22));
        }

        public static CoursePass Load(string path, double centerLineDegreeOffset, Rope rope)
        {
            Course course = new Course();
            course.SetCourseEntry(42.289087, -71.359124);
            course.SetCourseExit(42.287023, -71.359394);
            CoursePass pass = new CoursePass(course, rope, centerLineDegreeOffset);

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

        public static CoursePass LoadFromJson(string path, double centerLineDegreeOffset, Rope rope)
        {
            string json = "";
            using (var sr = File.OpenText(path))
                json = sr.ReadToEnd();
            if (json == "")
                throw new ApplicationException("Json file was empty: " + path);

            Course course = new Course();
            course.SetCourseEntry(42.289087, -71.359124);
            course.SetCourseExit(42.287023, -71.359394);
            CoursePass pass = new CoursePass(course, rope, centerLineDegreeOffset);

            var result = (List<Measurement>)JsonConvert.DeserializeObject(json, typeof(List<Measurement>));
            foreach (var r in result)
            {
                pass.Track(r);
            }
            
            return pass;
        }
    }
}
