using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using SlalomTracker;


namespace MetadataExtractor
{
    class Parser
    {
        // GYRO 403.846 Hz
        // GPS5 18.169 Hz

        const string GPMFEXE = "gpmfdemo";
        const string GYRO = "GYRO";
        const string GPS = "GPS5";
        const string TIME = "TIME";

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

        public List<Measurement> Load(string csv)
        {
            List<Measurement> measurements = new List<Measurement>();

            using (var sr = new StringReader(csv))
            {
                string line = sr.ReadLine(); // Advance 1st line header.
                Column currentItem = Column.Label;
                Measurement currentMeasurement = null; 
                DateTime initialTime = DateTime.MinValue;
                double seconds, lat, lon, z, speed;
                int currentItemCount = 0;
                string rowLabel;

                while ((line = sr.ReadLine()) != null)
                {
                    string[] row = line.Split(',');
                    rowLabel = GetColumn(row, Column.Label);

                    if (rowLabel == TIME)
                    {
                        // Reset datapoint item count.
                        currentItemCount = 0;

                        double start = double.Parse(GetColumn(row, Column.SecondsIn));
                        //double end = double.Parse(GetColumn(row, Column.SecondsOut));

                        DateTime startTime = initialTime.AddSeconds(start);
                        if (startTime == currentMeasurement.Timestamp)
                            // if time matches current time, move to GYRO data.
                            continue;
                        else 
                        {
                            // Start a new measurement object
                            if (currentMeasurement != null)
                                measurements.Add(currentMeasurement);
                            currentMeasurement = new Measurement();
                            currentMeasurement.Timestamp = startTime;
                        }
                    }
                    else if (rowLabel == GPS)
                    {
                        // Get all GPS, until
                        while ((rowLabel == GPS)
                        {

                        }
                    }
                    else if (rowLabel == GYRO)
                    {

                    }


                        GetColumn(row, Column.Seconds, out seconds);
                    GetColumn(row, Column.Lat, out lat);
                    GetColumn(row, Column.Lon, out lon);
                    GetColumn(row, Column.Speed, out speed);
                    GetColumn(row, Column.Z, out z);
                    DateTime timestamp = DateTime.Now.AddDays(-1).AddSeconds(seconds);
                    pass.Track(timestamp, z, lat, lon, speed);
                }
            }

            return pass;
        }

        private string ParseMetadata(string path)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GPMFEXE,
                    Arguments = path,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
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
