using System;
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
        const string TESTPATH = "2018-08-24/GOPR0565.MP4";

        [TestMethod]
        public void TestDownloadVideo()
        {
            string url = "https://delormej.blob.core.windows.net/ski/2018-08-24/GOPR0565.MP4";
            string localPath = Storage.DownloadVideo(url);
            Assert.AreEqual(localPath, TESTPATH);
        }

        [TestMethod]
        public void TestAddMetadata()
        {
            List<Measurement> list = new List<Measurement>();
            list.Add(new Measurement()
            {
                BoatGeoCoordinate = new GeoCoordinatePortable.GeoCoordinate(72.1, 42.3),
                BoatSpeedMps = 13.4,
                RopeSwingSpeedRadS = -0.003
            });
            list.Add(new Measurement()
            {
                BoatGeoCoordinate = new GeoCoordinatePortable.GeoCoordinate(72.1, 42.31),
                BoatSpeedMps = 13.3,
                RopeSwingSpeedRadS = -0.0024
            });

            Storage storage = Program.ConnectToStorage();
            storage.AddMetadata(TESTPATH, list);
        }
    }
}
