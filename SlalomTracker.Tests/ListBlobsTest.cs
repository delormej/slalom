using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SlalomTracker.Cloud;

namespace SlalomTracker.Cloud.Tests
{
    [TestClass]
    public class ListBlobsTest
    {
        [TestMethod]
        public void TestListBlobs()
        {  
            Storage storage = new Storage();
            var result = storage.GetAllBlobUris();

            Assert.IsTrue(result.Count > 1);
        }
    }
}
