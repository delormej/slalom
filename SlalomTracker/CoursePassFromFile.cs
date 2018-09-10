using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SlalomTracker
{
    public class CoursePassFactory
    {
        enum Column { Seconds = 0, Lat, Lon, Speed, Z };

        public static CoursePass FromFile(string path)
        {
            return FromFile(path, 0, Rope.Off(22));
        }

        public static CoursePass FromFile(string path, double centerLineDegreeOffset, Rope rope)
        {
            string json = "";
            using (var sr = File.OpenText(path))
                json = sr.ReadToEnd();
            if (json == "")
                throw new ApplicationException("Json file was empty: " + path);

            return FromJson(json, centerLineDegreeOffset, rope);
        }

        public static CoursePass FromJson(string json, double centerLineDegreeOffset, Rope rope)
        { 
            var measurements = (List<Measurement>)JsonConvert.DeserializeObject(json, typeof(List<Measurement>));
            Course course = Course.FindCourse(measurements);
            CoursePass pass = new CoursePass(course, rope, centerLineDegreeOffset);
            foreach (var r in measurements)
            {
                pass.Track(r);
            }
            
            return pass;
        }

        private static void GetColumn(string[] row, Column column, out double result)
        {
            result = 0;
            double.TryParse(row[(int)column], out result);
        }
    }
}
