using System;
using System.Collections.Generic;
using SlalomTracker;
using Microsoft.WindowsAzure.Storage;

namespace MetadataExtractor
{
    public class Extract
    {
        public static string ExtractMetadata(string path)
        {
            GpmfParser parser = new GpmfParser();
            List<Measurement> measurements = parser.LoadFromMp4(path);
            string json = Measurement.ToJson(measurements);
            return json;
        }
    }
}
