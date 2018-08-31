using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using SlalomTracker;
using Newtonsoft.Json;

namespace MetadataExtractor
{
    public class SkiVideoEntity : TableEntity
    {
        public SkiVideoEntity(string path, List<Measurement> measurements)
        {
            SetKeys(path);
            this.Measurements = JsonConvert.SerializeObject(measurements);
        }

        public string Measurements { get; set; }

        public string Skier { get; set; }

        public double RopeLengthM { get; set; }

        public double BoatSpeedMph { get; set; }

        private void SetKeys(string path)
        {
            if (!path.Contains(Path.AltDirectorySeparatorChar))
                throw new ApplicationException("path must contain <date>/Filename.");

            int index = path.LastIndexOf(Path.AltDirectorySeparatorChar);
            this.PartitionKey = path.Substring(0, index);
            this.RowKey = path.Substring(index + 1, path.Length - index - 1);
        }
    }
}
