using System;
using System.Collections.Generic;
using GeoCoordinatePortable;
using Newtonsoft.Json;

namespace SlalomTracker
{
    /// <summary>
    /// Object to store measurements calculated from a rope rotation event.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
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
        [JsonConverter(typeof(GeoCoordinateConverter))]
        public GeoCoordinate BoatGeoCoordinate { get; set; }

        /// <summary>
        /// GPS Precision, under 500 is good.
        /// </summary>
        /// https://github.com/gopro/gpmf-parser
        public double GpsAccuracy { get; set; }

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

        public override string ToString()
        {
            string json = JsonConvert.SerializeObject(this);
            return json;            
        }

        public static string ToJson(List<Measurement> measurements)
        {
            var settings = new JsonSerializerSettings() {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MaxDepth = 2
            };
            string json = JsonConvert.SerializeObject(measurements, settings);
            return json;
        }

        public static List<Measurement> DeserializeMeasurements(string json)
        {
            var measurements = (List<Measurement>)
                JsonConvert.DeserializeObject(json, typeof(List<Measurement>));
            return measurements;
        }
    }
}
