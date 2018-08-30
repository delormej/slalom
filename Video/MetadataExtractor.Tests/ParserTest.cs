using System.IO;
using System.Collections.Generic;
using SlalomTracker;
using MetadataExtractor;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace MetadataExtractor.Tests
{
    [TestClass]
    public class ParserTest
    {
        [TestMethod]
        public void TestLoadFromFile()
        {
            const string csvPath = "../../../GOPR0194.csv";
            Parser parser = new Parser();
            string csv = "";
            using (var sr = File.OpenText(csvPath))
                csv = sr.ReadToEnd();

            List<Measurement> measurements = parser.LoadFromCsv(csv);
            Assert.AreEqual(measurements.Count, 1177);
            Assert.AreEqual(measurements[1000].BoatSpeedMps, 8.164);
        }
    }
}
