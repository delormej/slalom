using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MetadataExtractor;

namespace MetadataExtractor.Tests
{
    [TestClass]
    public class QueueTest
    {
        [TestMethod]
        public void TestAdd()
        {
            Queue queue = new Queue(Program.ConnectToStorage());
            string blobName = "2018-08-24/GOPR0565.MP4";
            string url = "https://delormej.blob.core.windows.net/ski/2018-08-24/GOPR0565.MP4";
            queue.Add(blobName, url);
        }
    }
}
