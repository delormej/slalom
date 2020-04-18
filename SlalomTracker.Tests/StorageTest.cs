using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SlalomTracker.Cloud;

namespace SlalomTracker.Cloud.Tests
{
    [TestClass]
    public class StorageTest
    {
        const string TESTPATH = "2018-08-24/GOPR0565.MP4";
        const string URL = "https://skivideostorage.blob.core.windows.net/ski/2018-08-24/GOPR0565.MP4";
        public const string BLOBNAME = TESTPATH;

        [TestMethod]
        public void TestDownloadVideo()
        {
            string localPath = Storage.DownloadVideo(URL);
            Assert.AreEqual(localPath, TESTPATH);
        }

        [TestMethod]
        public void AddMetadataTest()
        {
            CoursePassFactory factory = new CoursePassFactory();
            string json = File.ReadAllText("./Video/GOPR0565.json");
            CoursePass pass = factory.FromJson(json);
            SkiVideoEntity entity = new SkiVideoEntity(URL, new DateTime(2018,08,24));

            Storage storage = new Storage();
            storage.AddMetadata(entity, json);
        }

        [TestMethod]
        public void TestUploadVideo()
        {
            Storage storage = new Storage();
            if (!storage.BlobNameExists(TESTPATH))
                storage.UploadVideo(TESTPATH, new DateTime(2018,08,24));
            Assert.IsTrue(storage.BlobNameExists(BLOBNAME), "Blob is missing: " + URL);
        }
    }
}
