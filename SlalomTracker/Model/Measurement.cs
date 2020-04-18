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
        [JsonConverter(typeof(GeoCoordinateConverter))]
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
            var settings = new JsonSerializerSettings() {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MaxDepth = 2
            };
            string json = JsonConvert.SerializeObject(measurements, settings);
            return json;
        }
    }

    public class GeoCoordinateConverter : JsonConverter<GeoCoordinate>
    {
        public override void WriteJson(JsonWriter writer, GeoCoordinate value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override GeoCoordinate ReadJson(JsonReader reader, Type objectType, GeoCoordinate existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string value = (string)reader.Value;
                return FromLatLon(value);
            }
            else
            {
                GeoCoordinate obj = new GeoCoordinate();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if ((string)reader.Value == "Latitude")
                        {
                            obj.Latitude = (double)reader.ReadAsDouble();
                        }
                        else if ((string)reader.Value == "Longitude")
                        {
                            obj.Longitude = (double)reader.ReadAsDouble();
                        }
                    }
                    else if (reader.TokenType == JsonToken.EndObject)
                        break;
                }
                return obj;
            }
        }

        public static GeoCoordinate FromLatLon(string latLon)
        {
            try
            {
                string[] values = latLon.Split(",", 2);
                return new GeoCoordinate(double.Parse(values[0]), double.Parse(values[1]));
            }
            catch
            {
                return GeoCoordinate.Unknown;
            }
        }        
    }
}
