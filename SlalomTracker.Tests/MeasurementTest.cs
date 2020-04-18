using System;
using SlalomTracker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace SlalomTracker
{
    [TestClass]
    public class MeasurementTest
    {
        [TestMethod]
        public void TestSerialization()
        {
            string[] jsons = new string[] {
                @"{
                    ""Timestamp"": ""0001-01-01T00:00:53.109"",
                    ""BoatGeoCoordinate"": {
                        ""VerticalAccuracy"": 1.33,
                        ""Latitude"": 42.285241,
                        ""Longitude"": -71.3614531
                    },
                    ""RopeSwingSpeedRadS"": 0.04694347045454546,
                    ""BoatSpeedMps"": 14.553
                }", 
                @"{
                    ""Timestamp"": ""0001-01-01T00:00:53.109"",
                    ""BoatGeoCoordinate"": ""42.285241,-71.3614531"",
                    ""RopeSwingSpeedRadS"": 0.04694347045454546,
                    ""BoatSpeedMps"": 14.553
                }",
                @"{""Timestamp"":""0001-01-01T00:00:02.002"",""BoatPosition"":null,""BoatGeoCoordinate"":{""Latitude"":42.285232,""Longitude"":-71.359619,""HorizontalAccuracy"":""NaN"",""VerticalAccuracy"":""NaN"",""Speed"":""NaN"",""Course"":""NaN"",""IsUnknown"":false,""Altitude"":""NaN""},""InCourse"":false,""RopeSwingSpeedRadS"":0.0,""RopeAngleDegrees"":0.0,""HandleSpeedMps"":0.0,""BoatSpeedMps"":0.125,""HandlePosition"":null}"
            };
            int i = 0;
            foreach (string json in jsons)
            {
                Measurement m = (Measurement)
                     Newtonsoft.Json.JsonConvert.DeserializeObject(json, typeof(Measurement));               
                m.BoatGeoCoordinate.VerticalAccuracy = 99.1349;
                System.Console.WriteLine($"Deserialized[{i++}]: {m.BoatGeoCoordinate.Longitude}, {m.RopeSwingSpeedRadS}");

                List<Measurement> list = new List<Measurement>();
                list.Add(m);
                string outJson = Measurement.ToJson(list);
                System.Console.WriteLine($"json: {Measurement.ToJson(list)}");

                var measurements = (List<Measurement>)
                    Newtonsoft.Json.JsonConvert.DeserializeObject(outJson, typeof(List<Measurement>));

                foreach (var d in measurements)
                {
                    Assert.IsTrue(Math.Round(d.BoatGeoCoordinate.Latitude,0) == 42);
                    System.Console.WriteLine($"[{d.Timestamp}] @ coords: {d.BoatGeoCoordinate.Latitude}, {d.BoatGeoCoordinate.Longitude}");
                }

            }
        }
    }
}