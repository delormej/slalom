using System.IO;
using System.Collections.Generic;
using SlalomTracker.Cloud.Tests;
using MetadataExtractor;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace SlalomTracker.Tests
{
    [TestClass]
    public class ParserTest
    {
        [TestMethod]
        public void TestLoadFromFile()
        {
            const string csvPath = "./Video/GOPR0194.csv";
            string csv = "";
            using (var sr = File.OpenText(csvPath))
                csv = sr.ReadToEnd();
            GpmfParser parser = new GpmfParser();
            List<Measurement> measurements = parser.LoadFromCsv(csv);
            AssertMeasurements(measurements);
        }

        [TestMethod]
        public void TestLoadFromMp4()
        {
            const string mp4Path = StorageTest.BLOBNAME;
            GpmfParser parser = new GpmfParser();
            List<Measurement> measurements = parser.LoadFromMp4(mp4Path);
            AssertMeasurements(measurements);
        }

        [TestMethod]
        public void TestMeasurementsToJson()
        {
            string csv, csvPath = "./Video/GOPR0565.csv";
            using (var sr = File.OpenText(csvPath))
                csv = sr.ReadToEnd();

            GpmfParser parser = new GpmfParser();
            List<Measurement> list = parser.LoadFromCsv(csv);
            string json = Measurement.ToJson(list);
            // File.WriteAllText("565.json", json);
            Assert.IsTrue(json.Length == 146971);

            //File.WriteAllText(@"..\..\..\..\MetadataExtractor\GOPR0565.json", json);
        }

        private void AssertMeasurements(List<Measurement> measurements)
        {
            Assert.AreEqual(measurements.Count, 1177);
            Assert.AreEqual(measurements[1000].BoatSpeedMps, 8.164);
        }
    }
}
