using System;
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
            Parser parser = new Parser();
            List<Measurement> measurements = parser.LoadFromFile("../../../GOPR0194.csv");
            Assert.Equals(measurements.Count, 1177);
            Assert.Equals(measurements[1000].BoatSpeedMps, 8.164);
        }
    }
}
