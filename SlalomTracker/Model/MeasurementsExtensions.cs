using System;
using System.Collections.Generic;
using System.Linq;

namespace SlalomTracker
{
    /// <summary>
    /// Helper extension methods for List<Measurment>.
    /// </summary>
    public static class MeasurementsExtensions
    {
        /// <summary>
        /// Finds a measurement at the second mark in total video seconds.
        /// </summary>
        public static Measurement FindAtSeconds(this IEnumerable<Measurement> measurements, double seconds)
        {
            if (measurements?.Count() <= 0)
                return null;

            const double offset = 0.25;
            DateTime start = measurements.First().Timestamp.AddSeconds(seconds);
            DateTime end = start.AddSeconds(seconds + offset);
            
            // try to find an event within 1/4 second:
            var match = measurements.Where(m => 
                m.Timestamp >= start
                && m.Timestamp < end )
            .FirstOrDefault();

            return match;
        }
    
        public static Measurement FindHandleAtY(this IEnumerable<Measurement> measurements, double y)
        {
            const double tolerance_m = 1.5;
            double start = y;
            double end = start + tolerance_m; 
            var match = measurements.Where(m => 
                m.HandlePosition.Y >= start
                && m.HandlePosition.Y < end )
            .FirstOrDefault();

            return match;
        }

        public static Measurement FindBoatAtY(this IEnumerable<Measurement> measurements, double y)
        {
            const double tolerance_m = 1.5;
            double start = y;
            double end = start + tolerance_m; 
            var match = measurements.Where(m => 
                m.BoatPosition.Y >= start
                && m.BoatPosition.Y < end )
            .FirstOrDefault();

            return match;            
        }
    }
}
