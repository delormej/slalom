using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MetadataExtractor;
using SlalomTracker;

namespace MetadataExtractor.Tests
{
    [TestClass]
    public class StorageTest
    {
        readonly string TESTPATH = "2018-08-24/GOPR0565.MP4";
        const string URL = "https://delormej.blob.core.windows.net/ski/2018-08-24/GOPR0565.MP4";
        const string BLOBNAME = "2018-08-24/GOPR0565.MP4";

        [TestMethod]
        public void TestDownloadVideo()
        {
            string localPath = Storage.DownloadVideo(URL);
            Assert.AreEqual(localPath, TESTPATH);
        }

        [TestMethod]
        public void TestUploadVideo()
        {
            Storage storage = new Storage(Extract.ConnectToStorage());
            storage.UploadVideo(TESTPATH);
            Assert.IsTrue(storage.BlobNameExists(BLOBNAME), "Blob is missing: " + URL);
        }

        [TestMethod]
        public void TestAddMetadata()
        {
            const string csvPath = "../../../GOPR0194.csv";
            string csv = "";
            using (var sr = File.OpenText(csvPath))
                csv = sr.ReadToEnd();
            Parser parser = new Parser();
            List<Measurement> list = parser.LoadFromCsv(csv);

            string path = "2018-08-24/GOPR0194.MP4";
            Storage storage = new Storage(Extract.ConnectToStorage());
            string json = Parser.MeasurementsToJson(list);
            storage.UploadMeasurements(path, json);
            storage.AddMetadata(path);
        }
    }
}
