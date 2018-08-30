using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SlalomTracker;

namespace SlalomTracker.Tests 
{
    [TestClass]
    public class RopeTest
    {
        [TestMethod]
        public void TestGetHandleApexDeg()
        {
            // Test case where rope is shorter than apex radius.
            Rope rope = new Rope(10.25);
            double apexDeg = rope.GetHandleApexDeg();
            Assert.IsTrue(Math.Round(apexDeg, 2) == 68.72);

            // Test case where rope is longer than apex radius.
            Rope rope2 = new Rope(23);
            double apexDeg2 = rope2.GetHandleApexDeg();
            Assert.IsTrue(Math.Round(apexDeg2, 2) == 28.57);
        }
    }    
}