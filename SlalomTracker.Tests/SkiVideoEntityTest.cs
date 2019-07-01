using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace SlalomTracker.Cloud
{
    [TestClass]
    public class SkiVideoEntityTest
    {
        [TestMethod]
        public void TestSkiVideoEntity()
        {
            SkiVideoEntity entity = new SkiVideoEntity();
            Console.WriteLine(entity.SlalomTrackerVersion);
        }
    }
}