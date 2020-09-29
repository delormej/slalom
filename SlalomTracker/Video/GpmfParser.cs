using System;
using System.IO;
using System.Text;
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
        const int TimeoutMs = 1 /*mins*/ * 30 /*seconds*/ * 1000; /*milliseconds*/
        const string GPMFEXE = "gpmfdemo";
        const string GYRO = "GYRO";
        const string GPS = "GPS5";
        const string GPSP = "GPSP";
        const string GPSU = "GPSU";
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
        double accumZ;
        double gpsAccuracy = 0.0;

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

                    if (rowLabel == GPSU)
                    {
                        ProcessGPSTime(row);
                    }
                    else if (rowLabel == GPS)
                    {
                        ProcessGps(row);
                    }
                    else if (rowLabel == GPSP)
                    {
                        ProcessGpsPrecision(row);   
                    }                    
                    else if (rowLabel == GYRO)
                    {
                        ProcessGyro(row);
                    }
                }
            }

            return measurements;
        }

        private void ProcessGPSTime(string[] row)
        {
            // Reset datapoint item count.
            currentGpsCount = 0;
            currentGyroCount = 0;
            accumZ = 0;
                           
            initialTime = ParseUTC(row[1]);
        }

        private DateTime ParseUTC(string value)
        {
            string raw = value?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                throw new ApplicationException("UTC Date Value empty.");

            const string gpmfFormat = "yyMMddHHmmss.fff";
            DateTime gpmfDate = DateTime.ParseExact(raw, gpmfFormat, 
                System.Globalization.CultureInfo.InvariantCulture);
            return gpmfDate;
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
            DateTime startTime = initialTime.AddSeconds((currentGpsCount / gpsHz));

            Measurement m = new Measurement();
            m.Timestamp = startTime;
            m.BoatGeoCoordinate = new GeoCoordinate(
                double.Parse(GetColumn(row, Column.Lat)),
                double.Parse(GetColumn(row, Column.Lon)));
            m.BoatSpeedMps = double.Parse(GetColumn(row, Column.Speed));
            m.GpsAccuracy = gpsAccuracy;
            measurements.Add(m);

            currentGpsCount++;
        }

        /// <summary>
        /// Process GPS Precision
        /// </summary>
        /// <param name="row"></param>
        private void ProcessGpsPrecision(string[] row)
        {
            if (row.Length > 2)
                double.TryParse(row[1], out gpsAccuracy);
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
                    // Logger.Log("Not enough measurements?"); 
                    return;
                }
                measurements[measurements.Count - mIndex].RopeSwingSpeedRadS = radS;
                accumZ = 0;
            }
            currentGyroCount++;
        }

        private string ParseMetadata(string mp4Path)
        {
            string exePath = System.AppContext.BaseDirectory + "/";

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

            StringBuilder resultBuilder = new StringBuilder();
            process.OutputDataReceived += (p, e) => {
                resultBuilder.AppendLine(e.Data);
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit(TimeoutMs);
            process.CancelOutputRead();

            if (!process.HasExited)
            {
                process.Kill();
                throw new ApplicationException(
                    $"Timeout ({TimeoutMs / 1000} seconds) exceeded while reading gmpf from {mp4Path}");
            }
            
            if (resultBuilder.Length > 0)
                return resultBuilder.ToString();
            else
                throw new ApplicationException($"Nothing returned from GPMF parser for {mp4Path}.");
        }

        private string GetColumn(string[] row, Column column)
        {
            return row[(int)column];
        }
    }
}
