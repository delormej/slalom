using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using SlalomTracker;

namespace MetadataExtractor
{
    public class SkiVideoEntity : TableEntity
    {
        public SkiVideoEntity(string path)
        {
            SetKeys(path);
        }

        public string Skier { get; set; }

        public double RopeLengthM { get; set; }

        public double BoatSpeedMph { get; set; }

        public bool HasCrash { get; set; }

        public bool All6Balls { get; set; }

        private void SetKeys(string path)
        {
            if (path.Contains('\\'))
                path = path.Replace('\\', '/');

            if (!path.Contains('/'))
                throw new ApplicationException("path must contain <date>/Filename.");

            int index = path.LastIndexOf(Path.AltDirectorySeparatorChar);
            this.PartitionKey = path.Substring(0, index);
            this.RowKey = path.Substring(index + 1, path.Length - index - 1);
        }
    }
}
