using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SlalomTracker
{
    public class CoursePassFactory
    {
        enum Column { Seconds = 0, Lat, Lon, Speed, Z };

        /// <summary>
        /// Loads an object of List<Measurment> from a JSON file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static CoursePass FromFile(string path)
        {
            return FromFile(path, 0, Rope.Off(22));
        }

        /// <summary>
        /// Loads an object of List<Measurment> from a JSON file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="centerLineDegreeOffset"></param>
        /// <param name="rope"></param>
        /// <returns></returns>
        public static CoursePass FromFile(string path, double centerLineDegreeOffset, Rope rope)
        {
            string json = "";
            using (var sr = File.OpenText(path))
                json = sr.ReadToEnd();
            if (json == "")
                throw new ApplicationException("Json file was empty: " + path);

            return FromJson(json, centerLineDegreeOffset, rope);
        }

        /// <summary>
        /// Loads a List<Measurment> collection serialized as JSON in the string.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="centerLineDegreeOffset"></param>
        /// <param name="rope"></param>
        /// <returns></returns>
        public static CoursePass FromJson(string json, double centerLineDegreeOffset, Rope rope)
        { 
            var measurements = (List<Measurement>)JsonConvert.DeserializeObject(json, typeof(List<Measurement>));
            Course course = Course.FindCourse(measurements);
            if (course == null)
            {
                throw new ApplicationException("Unable to find a course for this ski run.");
            }
            return CreatePass(measurements, course, centerLineDegreeOffset, rope);
        }

        public static CoursePass FromJson(string json, double ropeOffLength = 15)
        {
            CoursePass pass = FromJson(json, 0, Rope.Off(ropeOffLength));
            CoursePass betterPass = FitPass(pass.Measurements, pass.Course, pass.Rope);
            return betterPass;
        }

        public static CoursePass FromUrl(string url, double centerLineDegreeOffset = 0, double ropeOffLength = 15)
        {
            WebClient client = new WebClient();
            string json = client.DownloadString(url);
            if (string.IsNullOrEmpty(json))
                throw new ApplicationException("No JSON file at url: " + url);
            
            return FromJson(json, centerLineDegreeOffset, Rope.Off(ropeOffLength));
        }

        /// <summary>
        /// Does a linear regression to fit the best centerline offset based on entry/exit gates.
        /// </summary>
        /// <param name="measurements"></param>
        /// <param name="course"></param>
        /// <returns></returns>
        public static CoursePass FitPass(List<Measurement> measurements, Course course, Rope rope)
        {
            const int MAX = 45;
            const int MIN = -45;
            CoursePass bestPass = null;
            double bestPrecision = 0;

            for (int i = MIN; i <= MAX; i++)
            {
                CoursePass pass = CreatePass(measurements, course, i, rope);
                if (bestPass == null)
                {
                    bestPass = pass;
                    bestPrecision = pass.GetGatePrecision();
                }
                else
                {
                    double p = pass.GetGatePrecision();
                    if (p < bestPrecision)
                    {
                        bestPass = pass;
                        bestPrecision = p;
                    }
                }
            }
            return bestPass;
        }

        private static CoursePass CreatePass(List<Measurement> measurements, Course course, double centerLineDegreeOffset, Rope rope)
        {
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
