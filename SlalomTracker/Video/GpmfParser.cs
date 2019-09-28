using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using SlalomTracker;
using GeoCoordinatePortable;
using Newtonsoft.Json;

namespace MetadataExtractor
{
    /// <summary>
    /// Extracts metadata from MP4 file.
    /// </summary>
    public class GpmfParser
    {
        const string GPMFEXE = "gpmfdemo";
        const string GYRO = "GYRO";
        const string GPS = "GPS5";
        const string TIME = "TIME";
        const double gpsHz = 18; // GPS5 18.169 Hz
        const double gyroHz = 399;  // GYRO 403.846 Hz
        const int gyrosPerGps = (int)(gyroHz / gpsHz);

        // Source: https://github.com/gopro/gpmf-parser
        enum Column {
            Label = 0,
            SecondsIn = 1,
            SecondsOut = 2,
            Z = 1,
            Lat = 1,
            Lon = 2,
            Speed = 4,
        };

        List<Measurement> measurements;
        DateTime initialTime = DateTime.MinValue;
        int currentGpsCount, currentGyroCount;
        double start, accumZ;

        public List<Measurement> LoadFromMp4(string mp4Path)
        {
            string csv = ParseMetadata(mp4Path);
            if (csv != string.Empty)
                return LoadFromCsv(csv);
            else
                throw new ApplicationException("No metadata found in: " + mp4Path);
        }

        public List<Measurement> LoadFromCsv(string csv)
        {
            measurements = new List<Measurement>();

            using (var sr = new StringReader(csv))
            {
                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    string[] row = line.Split(',');
                    string rowLabel = GetColumn(row, Column.Label);

                    if (rowLabel == TIME)
                    {
                        ProcessTime(row);
                    }
                    else if (rowLabel == GPS)
                    {
                        ProcessGps(row);
                    }
                    else if (rowLabel == GYRO)
                    {
                        ProcessGyro(row);
                    }
                }
            }

            return measurements;
        }

        private void ProcessTime(string[] row)
        {
            // Reset datapoint item count.
            currentGpsCount = 0;
            currentGyroCount = 0;
            accumZ = 0;
            start = double.Parse(GetColumn(row, Column.SecondsIn));
        }

        /// <summary>
        /// Add a measurement record for each GPS row.
        /// </summary>
        /// <param name="row"></param>
        private void ProcessGps(string[] row)
        {
            // Don't grab more than x GPS entries.
            if (currentGpsCount > gpsHz)
                return;

            // @ GPS Hz, get fractional seconds.
            DateTime startTime = initialTime.AddSeconds(
                start + (currentGpsCount / gpsHz));

            Measurement m = new Measurement();
            m.Timestamp = startTime;
            m.BoatGeoCoordinate = new GeoCoordinate(
                double.Parse(GetColumn(row, Column.Lat)),
                double.Parse(GetColumn(row, Column.Lon)));
            m.BoatSpeedMps = double.Parse(GetColumn(row, Column.Speed));
            measurements.Add(m);

            currentGpsCount++;
        }

        /// <summary>
        /// Calculate the average Z axis speed for each GPS measurement and update.
        /// </summary>
        /// <param name="row"></param>
        private void ProcessGyro(string[] row)
        {
            accumZ += double.Parse(GetColumn(row, Column.Z));

            // When we have the appropriate # of gyro's per a GPS measurement, calculate.
            if (currentGyroCount > 0 && currentGyroCount % gyrosPerGps == 0)
            {
                // Update measurement.
                double radS = accumZ / gyrosPerGps;
                int mIndex = gyrosPerGps - (currentGyroCount / gyrosPerGps);
                if (measurements.Count <= mIndex)
                {
                    //Console.WriteLine("Not enough measurements?"); // log error here
                    return;
                }
                measurements[measurements.Count - mIndex].RopeSwingSpeedRadS = radS;
                accumZ = 0;
            }
            currentGyroCount++;
        }

        private string ParseMetadata(string mp4Path)
        {
            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "/";

            if (!File.Exists(mp4Path))
                throw new FileNotFoundException("MP4 file does not exist at: " + mp4Path);

            if (!File.Exists(exePath + GPMFEXE))
                throw new FileNotFoundException("gpmfdemo doesn't exist at: " + exePath + GPMFEXE);

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath + GPMFEXE,
                    Arguments = mp4Path,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true /*,
                    WorkingDirectory = */
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }

        private string GetColumn(string[] row, Column column)
        {
            return row[(int)column];
        }
    }
}
