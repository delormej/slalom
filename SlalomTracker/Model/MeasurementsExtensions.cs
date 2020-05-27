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
        public static Measurement FindHandleAtSeconds(this IEnumerable<Measurement> measurements, double seconds)
        {
            const double offset = 0.25;
            DateTime start = new DateTime().AddSeconds(seconds);
            DateTime end = new DateTime().AddSeconds(seconds + offset);
            
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

    }
}
