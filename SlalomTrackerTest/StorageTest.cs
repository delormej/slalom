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
        readonly string TESTPATH = "2018-08-24/GOPR0565.MP4";
        const string URL = "https://delormej.blob.core.windows.net/ski/2018-08-24/GOPR0565.MP4";
        const string BLOBNAME = "2018-08-24/GOPR0565.MP4";

        /*[TestMethod]
        public void TestDownloadVideo()
        {
            string localPath = Storage.DownloadVideo(URL);
            Assert.AreEqual(localPath, TESTPATH);
        }*/

        [TestMethod]
        public void AddMetadataTest()
        {
            Storage storage = new Storage();
            string json = File.ReadAllText(@"GOPR0565.json");
            storage.AddMetadata(URL, json);
        }

        [TestMethod]
        public void TestGetBlobDirectory()
        {
            // Only test if this is Windows:
            if (!System.Runtime.InteropServices.RuntimeInformation
                .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                Console.WriteLine("This test only runs on Windows.");
                return;
            }

            string localPath = @"\\files\video\GoPro Import\2018-08-23\GOPR0563.MP4";
            Console.WriteLine("local: " + Path.GetFullPath(localPath));
            string dir = Storage.GetBlobDirectory(localPath);
            Assert.AreEqual<string>("2018-08-23/", dir);

            string heroPath = @"\\files\video\GoPro Import\2018-08-24\HERO5 Black 3\GOPR0565.MP4";
            dir = Storage.GetBlobDirectory(heroPath);
            Assert.AreEqual<string>("2018-08-24/", dir);

            string relativePath = @"2018-08-24\GOPR0565.MP4";
            Console.WriteLine("relative: " + Path.GetFullPath(relativePath));
            dir = Storage.GetBlobDirectory(relativePath);
            Assert.AreEqual<string>("2018-08-24/", dir);
        }

        [TestMethod]
        public void TestGetBlobName()
        {
            if (!System.Runtime.InteropServices.RuntimeInformation
                .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                Console.WriteLine("This test only runs on Windows.");
                return;
            }
                        
            // Remote without the HERO5 Black 3 directory.
            string localPath = @"\\files\video\GoPro Import\2018-08-23\GOPR0563.MP4";
            string blobName = Storage.GetBlobName(localPath);
            Assert.AreEqual<string>("2018-08-23/GOPR0563.MP4", blobName);

            // Remote with the HERO5 directory.
            blobName = "";
            string heroPath = @"\\files\video\GoPro Import\2018-08-24\HERO5 Black 3\GOPR0565.MP4";
            blobName = Storage.GetBlobName(heroPath);
            Assert.AreEqual<string>("2018-08-24/GOPR0565.MP4", blobName);

            // Local relative.
            blobName = "";
            string relativePath = @"2018-08-24\GOPR0565.MP4";
            blobName = Storage.GetBlobName(relativePath);
            Assert.AreEqual<string>("2018-08-24/GOPR0565.MP4", blobName);
        }

        [TestMethod]
        public void TestUploadVideo()
        {
            Storage storage = new Storage();
            if (!storage.BlobNameExists(TESTPATH))
                storage.UploadVideo(TESTPATH);
            Assert.IsTrue(storage.BlobNameExists(BLOBNAME), "Blob is missing: " + URL);
        }
    }
}
