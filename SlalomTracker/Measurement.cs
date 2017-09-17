using System;
using System.Collections.Generic;
using System.Text;

namespace SlalomTracker
{
    /// <summary>
    /// Object to store measurements calculated from a rope rotation event.
    /// </summary>
    public class Measurement
    {
        public DateTime Timestamp { get; set; }

        public CoursePosition BoatPosition { get; set; }

        /// <summary>
        /// Rope swing speed in radians/second.
        /// </summary>
        public double RopeSwingSpeedRadS { get; set; }

        /// <summary>
        /// Current rope angle as it rotates on Y axis aound the ski pilon.
        /// </summary>
        public double RopeAngleDegrees { get; set; }

        public double BoatSpeedMps { get; set; }

        public CoursePosition HandlePosition { get; set; }



        // 1. GPS map entry and exit gates for course, store bearing (degrees) to be used to offset phone/rope bearing at launch.
        // 2. Calculate CL bearing based on gate entry, current bearing
        // 2. Calculate rope swing speed
        // 3. Calculate rope apex
        // 4. Calculate handle position based on degrees and rope length.
        // 5. Calculate the arc of the rope for a given length
        // 6. Calculate x,y position of handle in space.

        // Measurement:
        // Time Stamp
        // Boat Position (Lat/Long), but store x,y position?
        // Boat Speed (this-last position / this-last time)
        // Rope Swing Speed (rad/second)
        // Rope Angle (degrees) -- calculated based on swing speed and last degrees
        // Handle Position (x,y relative to course)


        // We're registering for rotation events, which will come with timestamp & rad/sec.
        // We need to then get the pilon position (lat/long) at that very instant
        // From all of this, we can create a measurement.

        // GPS

    }
}
