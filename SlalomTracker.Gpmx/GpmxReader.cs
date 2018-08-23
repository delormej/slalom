using Systen;
using Systen.IO;

namespace SlalomTracker.Gpmx {
    
// course starts @  42.28909, -71.35912, ends @ 42.28674, -71.35943

    /// <summary>
    /// Reads GPMX from a JSON file, creates slalom CoursePass from readings.
    /// </summary>
    public class GpmxReader {
        private CoursePass m_coursePass;
        private JSON m_json;

        public GpmxReader(string filename, Rope rope, Course course) {
            Rope rope = new Rope(//15 off//);
            Course course = new Course(//lat/long//);
            m_coursePass = new CoursePass(course, rope);
            // open file
            //GpmxReader(File.Open())
        }

        public GpmxReader(Stream stream) {

        }
        
        public CoursePass Read() {
            // foreach json member, parse
            
            foreach (var entry in m_json) {
                m_coursePass.Track(entry.Timestamp, entry.radS, entry.lat, entry.long);
            }
        }
    }
}