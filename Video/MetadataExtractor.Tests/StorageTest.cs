using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MetadataExtractor;

namespace MetadataExtractor.Tests
{
    [TestClass]
    public class StorageTest
    {
        [TestMethod]
        public void TestDownloadVideo()
        {
            string url = "https://delormej.blob.core.windows.net/ski/2018-08-24/GOPR0565.MP4";
            string localPath = Storage.DownloadVideo(url);
            Assert.AreEqual(localPath, "2018-08-24/GOPR0565.MP4");
        }
    }
}
