using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using GeoCoordinatePortable;
using Newtonsoft.Json;
using System.Linq;
using SlalomTracker.Cloud;
using SlalomTracker.Video;
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

            return FromJsonUrl(video.JsonUrl);
        }

        /// <summary>
        /// Loads an object of List<Measurment> from a JSON file.
        /// </summary>
        /// <param name="path"></param>
        public CoursePass FromLocalJsonFile(string path)
        {
            using (var sr = File.OpenText(path))
                _json = sr.ReadToEnd();
            if (_json == "")
                throw new ApplicationException("Json file was empty: " + path);

            return FromJson(_json);
        }

        /// <summary>
        /// Downloads json from the provided url to creates a CoursePass.
        /// </summary>
        public CoursePass FromJsonUrl(string url)
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
            
            var measurements = Measurement.DeserializeMeasurements(_json);
            List<Measurement> nextMeasurements = GetNextPassMeasurements(exit, measurements);

            if (nextMeasurements?.Count() > 0)
                return CreatePass(nextMeasurements);
            else
                return null;
        }

        private List<Measurement> GetNextPassMeasurements(
            Measurement exit, List<Measurement> measurements)
        {
            const double NextCourseOffsetSeconds = 15;
            DateTime offsetStart = exit.Timestamp.AddSeconds(NextCourseOffsetSeconds);  
            
            IEnumerable<Measurement> nextMeasurements = measurements.Where(
                m => m.Timestamp > offsetStart
            );

            return nextMeasurements?.ToList();
        }

        private Course GetCourse(List<Measurement> measurements, out Measurement entry55)
        {
            Course course;
            entry55 = null;

            if (Course55Coordinates.EntryLat != default(double)) 
            {
                course = new Course(
                    new GeoCoordinate(Course55Coordinates.EntryLat, Course55Coordinates.EntryLon),
                    new GeoCoordinate(Course55Coordinates.ExitLat, Course55Coordinates.ExitLon)
                );
                entry55 = course.FindEntry55(measurements);
            }
            else 
            {
                KnownCourses knownCourses = new KnownCourses();
                course = knownCourses.FindCourse(measurements, out entry55);
            }    

            return course;
        }

        private CoursePass CreatePass()
        {
            if (_json == null)
                throw new ApplicationException("Must load json from File, Url or String.");
            var measurements = Measurement.DeserializeMeasurements(_json);
            return CreatePass(measurements);
        }

        private CoursePass CreatePass(List<Measurement> measurements)
        {
            const double MAX_PASS_SECONDS = 30.0;

            if (measurements == null || measurements.Count() <= 1)
                throw new ApplicationException("Unable to create a pass, no measurements passed.");

            CoursePass pass = new CoursePass();
            pass.CenterLineDegreeOffset = CenterLineDegreeOffset;
            
            if (m_rope == null)
                m_rope = Rope.Default;
            pass.Rope = m_rope;

            Measurement entry55 = null;

            if (m_course == null)
            {
                pass.Course = GetCourse(measurements, out entry55);
            }
            else
            {
                pass.Course = m_course;
                entry55 = pass.Course.FindEntry55(measurements);               
            }

            if (pass.Course == null || entry55 == null)
            {
                int gpsInaccuracyCount = measurements.Count(m => m.GpsAccuracy > 500.0);
                string error = $"No course found.  Had {gpsInaccuracyCount} inaccurate of total {measurements.Count()} measurements.";
                throw new ApplicationException(error);
            }
            
            pass.Entry = entry55;
            pass.Exit = pass.Course.FindExit55(measurements) ?? measurements.Last();

            if (pass.Exit.Timestamp.Subtract(pass.Entry.Timestamp).TotalSeconds > MAX_PASS_SECONDS)
            {
                pass.Exit = measurements.FindHandleAtSeconds(
                    pass.Entry.Timestamp.AddSeconds(MAX_PASS_SECONDS).TimeOfDay.TotalSeconds
                );
            }

            pass.VideoTime = GetVideoTime(measurements, pass.Entry, pass.Exit);

            int lastIndex = measurements.IndexOf(pass.Exit);
            int firstIndex = measurements.IndexOf(pass.Entry);
            if (firstIndex == 0)
                firstIndex++;           
            
            measurements[firstIndex-1].RopeAngleDegrees = CenterLineDegreeOffset;
            
            pass.Measurements = new List<Measurement>();
            for (int i = firstIndex; i < lastIndex; i++)
            {
                Measurement current = measurements[i];
                current.BoatPosition = pass.Course.CoursePositionFromGeo(current.BoatGeoCoordinate);

                if (current.BoatPosition != CoursePosition.Empty)
                {                    
                    current.InCourse = (current.BoatPosition.Y >= Course.Gates[0].Y && 
                        current.BoatPosition.Y <= Course.Gates[3].Y);                       

                    HandleCalculations(current, measurements, i);               
                    pass.Measurements.Add(current);

                    if (current.Timestamp >= pass.Exit.Timestamp)
                        break;
                }
            }
            
            pass.AverageBoatSpeed = CalculateCoursePassSpeed(pass);

            return pass;
        }
 
        /// <summary>
        /// This is the main method to do all calculations for the handle position / speed.
        /// </summary>
        private void HandleCalculations(Measurement current, List<Measurement> measurements, int index)
        {
            Measurement previous = measurements[index - 1];

            // Convert radians per second to degrees per second.  
            double seconds = current.Timestamp.Subtract(previous.Timestamp).TotalSeconds;
            double averageRopeSwingSpeedRadS = AverageRopeSwingSpeedRadS(measurements, index);
            current.RopeAngleDegrees = previous.RopeAngleDegrees +
                Util.RadiansToDegrees(averageRopeSwingSpeedRadS * seconds);

            current.HandlePosition = CalculateRopeHandlePosition(current);
            current.HandleSpeedMps = CalculateHandleSpeed(previous, current, seconds);
        }

        private double CalculateCoursePassSpeed(CoursePass pass)
        {
            Measurement entryGate = pass.Measurements.FindBoatAtY(Course.Gates[0].Y);
            Measurement exitGate = pass.Measurements.FindBoatAtY(Course.Gates[3].Y);

            if (exitGate == null)
            {
                // Try to calculate speed at 1 ball.
                exitGate = pass.Measurements.FindBoatAtY(Course.Balls[0].Y);
                Logger.Log("Course exit null, trying to caluculate from ball 1: " + exitGate?.BoatPosition.Y);
            }

            if (entryGate == null || exitGate == null)
            {
                Logger.Log("Skiping course pass speed calculation since gates are null.");
                return 0;                
            }

            if (exitGate.Timestamp.Subtract(entryGate.Timestamp).TotalSeconds > 30)
            {
                Logger.Log("Skiping course pass speed calculation, exit gate is out of range.");
                return 0;
            }

            TimeSpan duration = exitGate.Timestamp.Subtract(entryGate.Timestamp);
            double distance = exitGate.BoatGeoCoordinate.GetDistanceTo(
                entryGate.BoatGeoCoordinate);
            
            if (duration == null || duration.Seconds <= 0 || distance <= 0)
            {
                throw new ApplicationException("Could not calculate time and distance for course entry/exit.");
            }

            double speedMps = distance / duration.TotalSeconds;
            return Math.Round(speedMps * CoursePass.MPS_TO_MPH, 1);
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
            if (previous?.HandlePosition == null || time == 0)
                return 0.0d;

            // Calculate 1 side of right angle triangle
            double dX = current.HandlePosition.X - previous.HandlePosition.X;
            double dY = current.HandlePosition.Y - previous.HandlePosition.Y;
            // a^2 + b^2 = c^2
            double distance = Math.Sqrt((dY * dY) + (dX * dX));
            
            return distance / time;
        }    

        /// <summary>
        /// Returns a struct that represents the time since the begining of video in fractional seconds
        /// when the boat first goes through the 55s and how long until it goes through exit 55s.
        /// </summary>
        private VideoTime GetVideoTime(List<Measurement> measurements, Measurement entry, Measurement exit)
        {
            VideoTime time = new VideoTime();

            const double DEFAULT_DURATION = 30.0; // max # of seconds for the video unless otherwise specified.
            const double GATE_OFFSET_SECONDS = 1.0; // amount of time before 55s to trim with.

            if (entry == null)
            {
                Logger.Log("Unable to find boat at 55s, returning start time as 0 seconds.");
                time.Start = 0;
            }
            else
            {
                time.Start = GetSecondsFrom(measurements[0], entry);

                // if video has the recording, backup the start before entry.
                if (time.Start >= GATE_OFFSET_SECONDS)
                    time.Start -= GATE_OFFSET_SECONDS;
            }

            if (exit == null)
            {
                Logger.Log("Unable to find exit, will use last measurement or default.");
                time.Duration = GetSecondsFrom(entry, measurements.Last());
            }
            else
            {
                double exitSeconds = exit.Timestamp.TimeOfDay.TotalSeconds + GATE_OFFSET_SECONDS;
                if (exitSeconds > measurements.Last().Timestamp.TimeOfDay.TotalSeconds)
                    exitSeconds = measurements.Last().Timestamp.TimeOfDay.TotalSeconds;

                time.Duration = exitSeconds - time.Start;
            }

            if (time.Duration > DEFAULT_DURATION)
                time.Duration = DEFAULT_DURATION;

            return time;
        }

        private double GetSecondsFrom(Measurement start, Measurement end)
        {
            double seconds = 0.0d;
            TimeSpan fromStart = end.Timestamp.Subtract(
                start.Timestamp);
            if (fromStart != null)
                seconds = fromStart.TotalSeconds;
            return seconds;
        }                
    }
}
