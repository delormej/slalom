using System;
using GeoCoordinatePortable;
using Newtonsoft.Json;

namespace SlalomTracker
{
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