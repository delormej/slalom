using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using GeoCoordinatePortable;
using Newtonsoft.Json;
using System.Linq;

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
                m_rope = Rope.Off(value);
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
                KnownCourses knownCourses = new KnownCourses();
                this.m_course = knownCourses.FindCourse(measurements);
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

        // private CoursePass CreatePass(List<Measurement> measurements)
        // {
        //     CoursePass pass = new CoursePass(m_course, m_rope, CenterLineDegreeOffset);
        //     foreach (var r in measurements)
        //         pass.Track(r);

        //     return pass;
        // }

        private CoursePass CreatePass(List<Measurement> measurements)
        {
            CoursePass pass = new CoursePass(m_course, m_rope, CenterLineDegreeOffset);
            for (int i = 0; i < measurements.Count; i++)
            {
                Measurement current = measurements[i];
                current.BoatPosition = pass.CoursePositionFromGeo(current.BoatGeoCoordinate);               
                if (current.BoatPosition == CoursePosition.Empty)
                    continue;               
                Calculate(measurements, i);
                pass.Track(current);
            }

            return pass;
        }

        private void Calculate(List<Measurement> measurements, int index)
        {
            Measurement current = measurements[index];
            Measurement previous = null;
            double seconds = 0.0d;

            if (index == 0)
            {
                measurements[index].RopeAngleDegrees = CenterLineDegreeOffset;
            }
            else 
            {
                previous = measurements[index - 1];
                seconds = current.Timestamp.Subtract(previous.Timestamp).TotalSeconds;
                
                // Convert radians per second to degrees per second.  
                double averageRopeSwingSpeedRadS = AverageRopeSwingSpeedRadS(measurements, index);
                current.RopeAngleDegrees = previous.RopeAngleDegrees +
                    Util.RadiansToDegrees(averageRopeSwingSpeedRadS * seconds);
            }

            current.HandlePosition = CalculateRopeHandlePosition(current);
            current.HandleSpeedMps = CalculateHandleSpeed(previous, current, seconds);
        }

        private double AverageRopeSwingSpeedRadS(List<Measurement> measurements, int index)
        {
            const int HalfWindowSize = 4;
            double average = 0.0d;

            if (index < HalfWindowSize || (index + HalfWindowSize) >= measurements.Count)
                average = measurements[index].RopeSwingSpeedRadS;
            else 
                average = measurements.GetRange(index-HalfWindowSize, 2*HalfWindowSize)
                    .Select(v => v.RopeSwingSpeedRadS)
                    .Average();
            return average;
        }

        ///
        /// <summary> Get handle position in x,y coordinates from the pilon. </summary>
        ///
        private CoursePosition CalculateRopeHandlePosition(Measurement current) 
        {
            CoursePosition virtualHandlePos = m_rope.GetHandlePosition(current.RopeAngleDegrees);

            // Actual handle position is calculated relative to the pilon/boat position, behind the boat.
            double y = current.BoatPosition.Y - virtualHandlePos.Y;
            double x = current.BoatPosition.X - virtualHandlePos.X;
            return new CoursePosition(x, y);            
        }

        private double CalculateHandleSpeed(Measurement previous, Measurement current, double time) 
        {
            if (previous == null || time == 0)
                return 0.0d;

            // Calculate 1 side of right angle triangle
            double dX = current.HandlePosition.X - previous.HandlePosition.X;
            double dY = current.HandlePosition.Y - previous.HandlePosition.Y;
            // a^2 + b^2 = c^2
            double distance = Math.Sqrt((dY * dY) + (dX * dX));
            
            return distance / time;
        }        
    }
}
