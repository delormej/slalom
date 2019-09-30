using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using GeoCoordinatePortable;
using Newtonsoft.Json;

namespace SlalomTracker
{
    public struct CourseCoordinates 
    {
        public double EntryLat;
        public double EntryLon;
        public double ExitLat;
        public double ExitLon;

        public static CourseCoordinates Default = new CourseCoordinates();
    }

    public class CoursePassFactory
    {
        public double CenterLineDegreeOffset { get; set; } = 0;
        public double RopeLengthOff { 
            get 
            {
                if (m_rope != null) 
                    return m_rope.FtOff;
                else 
                    return 0;
            }
            set 
            {
                m_rope = new Rope(value);
            }
        }
        public CourseCoordinates Course55Coordinates { get; set; }
        
        private Course m_course;
        private Rope m_rope;

        public CoursePassFactory() 
        {
        }

        /// <summary>
        /// Loads an object of List<Measurment> from a JSON file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="centerLineDegreeOffset"></param>
        /// <param name="rope"></param>
        /// <returns></returns>
        public CoursePass FromFile(string path)
        {
            string json = "";
            using (var sr = File.OpenText(path))
                json = sr.ReadToEnd();
            if (json == "")
                throw new ApplicationException("Json file was empty: " + path);

            return FromJson(json);
        }

        /// <summary>
        /// Loads a List<Measurment> collection serialized as JSON in the string.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="centerLineDegreeOffset"></param>
        /// <param name="rope"></param>
        /// <returns></returns>
        public CoursePass FromJson(string json)
        { 
            var measurements = (List<Measurement>)JsonConvert.DeserializeObject(json, typeof(List<Measurement>));
            
            if (Course55Coordinates.EntryLat != default(double)) 
            {
                this.m_course = new Course(
                    new GeoCoordinate(Course55Coordinates.EntryLat, Course55Coordinates.EntryLon),
                    new GeoCoordinate(Course55Coordinates.ExitLat, Course55Coordinates.ExitLon)
                );
            }
            else 
            {
                this.m_course = Course.FindCourse(measurements);
            }

            if (this.m_course == null)
                throw new ApplicationException("Unable to find a course for this ski run.");

            return CreatePass(measurements);
        }

        public CoursePass FromUrl(string url)
        {
            WebClient client = new WebClient();
            string json = client.DownloadString(url);
            if (string.IsNullOrEmpty(json))
                throw new ApplicationException("No JSON file at url: " + url);
            
            return FromJson(json);
        }

        /// <summary>
        /// Does a linear regression to fit the best centerline offset based on entry/exit gates.
        /// </summary>
        /// <param name="measurements"></param>
        /// <param name="course"></param>
        /// <returns></returns>
        public CoursePass FitPass(List<Measurement> measurements)
        {
            const int MAX = 45;
            const int MIN = -45;
            CoursePass bestPass = null;
            double bestPrecision = 0;

            for (int i = MIN; i <= MAX; i++)
            {
                CenterLineDegreeOffset = i;
                CoursePass pass = CreatePass(measurements);
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

        private CoursePass CreatePass(List<Measurement> measurements)
        {
            CoursePass pass = new CoursePass(m_course, m_rope, CenterLineDegreeOffset);
            foreach (var r in measurements)
                pass.Track(r);

            return pass;
        }
    }
}
