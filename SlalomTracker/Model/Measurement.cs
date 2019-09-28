using System;
using System.Collections.Generic;
using GeoCoordinatePortable;
using Newtonsoft.Json;

namespace SlalomTracker
{
    /// <summary>
    /// Object to store measurements calculated from a rope rotation event.
    /// </summary>
    public class Measurement
    {
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Boat's position in x/y coordinates relative to the course.
        /// </summary>
        public CoursePosition BoatPosition { get; set; }

        /// <summary>
        /// Lat/Lon, heading and speed of boat.
        /// </summary>
        public GeoCoordinate BoatGeoCoordinate { get; set; }

        /// <summary>
        /// Indicates whether boat is in the course or not.
        /// </summary>
        public bool InCourse { get; set; }

        /// <summary>
        /// Rope swing speed in radians/second.
        /// </summary>
        public double RopeSwingSpeedRadS { get; set; }

        /// <summary>
        /// Current rope angle as it rotates on Y axis aound the ski pilon.
        /// </summary>
        public double RopeAngleDegrees { get; set; }

        /// <summary>
        /// How fast the skiier is moving.
        /// </summary>
        public double HandleSpeedMps { get; set; }

        /// <summary>
        /// Current boat speed in meters per second.
        /// </summary>
        public double BoatSpeedMps { get; set; }

        /// <summary>
        /// Position of the handle relative to the course in X,Y coordinates.
        /// </summary>
        public CoursePosition HandlePosition { get; set; }

        public static string ToJson(List<Measurement> measurements)
        {
            string json = JsonConvert.SerializeObject(measurements);
            return json;
        }
    }
}
