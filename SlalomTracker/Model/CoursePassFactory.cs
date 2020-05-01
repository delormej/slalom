using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using GeoCoordinatePortable;
using Newtonsoft.Json;
using System.Linq;
using SlalomTracker.Cloud;
using Logger = jasondel.Tools.Logger;

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
        private string _json;

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

        public Course Course 
        { 
            get { return m_course; } 
            set { m_course = value; }
        }
        
        private Course m_course;
        private Rope m_rope;

        public CoursePassFactory() 
        {
        }

        public CoursePass FromSkiVideo(SkiVideoEntity video)
        {
            CenterLineDegreeOffset = video.CenterLineDegreeOffset;
            RopeLengthOff = video.RopeLengthM;
            
            KnownCourses courses = new KnownCourses();
            Course = courses.ByName(video.CourseName);

            return FromUrl(video.JsonUrl);
        }

        /// <summary>
        /// Loads an object of List<Measurment> from a JSON file.
        /// </summary>
        /// <param name="path"></param>
        public CoursePass FromFile(string path)
        {
            using (var sr = File.OpenText(path))
                _json = sr.ReadToEnd();
            if (_json == "")
                throw new ApplicationException("Json file was empty: " + path);

            return FromJson(_json);
        }

        public CoursePass FromUrl(string url)
        {
            WebClient client = new WebClient();
            _json = client.DownloadString(url);
            if (string.IsNullOrEmpty(_json))
                throw new ApplicationException("No JSON file at url: " + url);
            
            return FromJson(_json);
        }

        /// <summary>
        /// Loads a List<Measurment> collection serialized as JSON in the string.
        /// </summary>
        /// <param name="json"></param>
        public CoursePass FromJson(string json)
        { 
            _json = json;
            return CreatePass();
        }

        /// <summary>
        /// Returns another coures pass if one exists after the exit measurment in 
        /// this collection of measurements of pass.  Returns null if there isn't another
        /// course pass found.
        /// </summary>
        public CoursePass GetNextPass(Measurement exit)
        {
            m_course = null; // Clear out existing course.
            CoursePass nextPass = null;
            
            var measurements = DeserializeMeasurements(); // Should I really need to do this each time???
            List<Measurement> nextMeasurements = GetNextPassMeasurements(exit, measurements);
            if (nextMeasurements != null && nextMeasurements.Count > 0)
            {
                nextPass = CreatePass(nextMeasurements);
            }
            return nextPass;
        }

        private List<Measurement> DeserializeMeasurements()
        {
            var measurements = (List<Measurement>)
                JsonConvert.DeserializeObject(_json, typeof(List<Measurement>));
            return measurements;
        }

        private List<Measurement> GetNextPassMeasurements(
            Measurement exit, List<Measurement> measurements)
        {
            const double NextCourseOffsetSeconds = 15;
            DateTime offsetStart = exit.Timestamp.AddSeconds(NextCourseOffsetSeconds);  
            
            IEnumerable<Measurement> nextMeasurements = measurements.Where(
                m => m.Timestamp > offsetStart
            );

            if (nextMeasurements != null && nextMeasurements.Count() > 0)
                return nextMeasurements.ToList();
            else
                return null;
        }

        /// <summary>
        /// Does a linear regression to fit the best centerline offset based on entry/exit gates.
        /// </summary>
        public double FitPass(string jsonUrl)
        {
            CoursePass bestPass = FromUrl(jsonUrl);
            return FitPass(bestPass);
        }

        public double FitPass(CoursePass pass)
        {
            const int MAX = 45, MIN = -45;
            double bestPrecision = double.MaxValue;
            CoursePass bestPass = pass;

            for (int i = MIN; i <= MAX; i++)
            {
                CenterLineDegreeOffset = i;
                CoursePass nextPass = CreatePass(pass.Measurements);
                double precision = nextPass.GetGatePrecision();
                
                if (precision < bestPrecision)
                {
                    bestPrecision = precision;
                    bestPass = nextPass;
                }
            }
            Logger.Log($"Best Precision: {bestPrecision} = {bestPass.CenterLineDegreeOffset}");
            return bestPass.CenterLineDegreeOffset;
        }

        private void FindCourse(List<Measurement> measurements)
        {
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
        }

        private CoursePass CreatePass()
        {
            if (_json == null)
                throw new ApplicationException("Must load json from File, Url or String.");
            var measurements = DeserializeMeasurements();
            return CreatePass(measurements);
        }

        private CoursePass CreatePass(List<Measurement> measurements)
        {
            if (this.m_course == null)
                FindCourse(measurements);
            
            if (this.m_course == null)
            {
                Logger.Log("Unable to find a course for this ski run.");       
                return null;
            }

            if (m_rope == null)
                m_rope = Rope.Default;

            CoursePass pass = new CoursePass(m_course, m_rope, CenterLineDegreeOffset);
            for (int i = 0; i < measurements.Count; i++)
            {
                Measurement current = measurements[i];
                current.BoatPosition = pass.CoursePositionFromGeo(current.BoatGeoCoordinate);               
                if (current.BoatPosition == CoursePosition.Empty)
                    continue;               
                
                CalculateInCourse(pass, measurements, i);
                CalculateCurrent(measurements, i);
                pass.Track(current);

                // If the handle has passed the 55s, we're done here.
                if (current.HandlePosition.Y > Course.LengthM)
                    break;
            }
            
            CalculateCoursePassSpeed(pass);
            return pass;
        }

        private void CalculateInCourse(CoursePass pass, List<Measurement> measurements, int i)
        {
            Measurement current = measurements[i];
            if (i == 0)
            {
                current.InCourse = false;
                return;
            }
            
            Measurement previous = measurements[i-1];
            if (current.BoatPosition.Y >= Course.Gates[0].Y && 
                current.BoatPosition.Y <= Course.Gates[3].Y )
            {
                current.InCourse = true;
                if (previous.InCourse == false)
                    pass.Entry = current;
            }
            else if (previous.InCourse && current.BoatPosition.Y >= Course.Gates[3].Y)
            {
                current.InCourse = false;
                pass.Exit = current;
            }
            else
            {
                current.InCourse = false;
            }
        }

        private void CalculateCurrent(List<Measurement> measurements, int index)
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

        private void CalculateCoursePassSpeed(CoursePass pass)
        {
            if (pass.Exit == null || pass.Entry == null)
            {
                Logger.Log("Skiping course pass speed calculation since Entry or Exit are null.");
                return;
            }

            TimeSpan duration = pass.Exit.Timestamp.Subtract(pass.Entry.Timestamp);
            double distance = pass.Exit.BoatGeoCoordinate.GetDistanceTo(
                pass.Entry.BoatGeoCoordinate);
            
            if (duration == null || duration.Seconds <= 0 || distance <= 0)
            {
                throw new ApplicationException("Could not calculate time and distance for course entry/exit.");
            }

            double speedMps = distance / duration.TotalSeconds;
            pass.AverageBoatSpeed = Math.Round(speedMps * CoursePass.MPS_TO_MPH, 1);
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
