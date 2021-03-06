﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SlalomTracker.Cloud;

namespace SlalomTracker.Cloud.Tests
{
    [TestClass]
    public class QueueTest
    {
        [TestMethod]
        public void TestAdd()
        {
            AzureStorage storage = new AzureStorage();
            string blobName = "2018-08-24/GOPR0565.MP4";
            string url = "https://skivideostorage.blob.core.windows.net/ski/2018-08-24/GOPR0565.MP4";
            storage.Queue.Add(blobName, url);
        }
    }
}
