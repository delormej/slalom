using System;
using System.Collections.Generic;
using SlalomTracker;
using Microsoft.WindowsAzure.Storage;

namespace MetadataExtractor
{
    public class Extract
    {
        public static string ExtractMetadata(string mp4)
        {
            GpmfParser parser = new GpmfParser();
            List<Measurement> measurements = parser.LoadFromMp4(mp4);
            string json = Measurement.ToJson(measurements);
            Console.WriteLine("Created JSON measurements for: " + mp4);
            return json;
        }
    }
}
