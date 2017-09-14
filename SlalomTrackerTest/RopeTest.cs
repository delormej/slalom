using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace SlalomTracker
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

        [TestMethod]
        public void TestGetHandlePosition()
        {
            Rope rope = new Rope(23);

            // Handle is on the right side.
            var pos = rope.GetHandlePosition(65);

            Assert.IsTrue(Math.Round(pos.X,4) == Math.Round(20.845079101842948, 4));
            Assert.IsTrue(Math.Round(pos.Y,4) == Math.Round(9.7202200200360878, 4));

            // When handle is past the pilon and on left side.
            var pos2 = rope.GetHandlePosition(-91.2);

            // X - 22.994955719921446 double
            // Y - 0.48167565731721124    double
            // Just ensure they are both negative.
            Assert.IsTrue(pos2.X < 0 && pos2.Y < 0);
        }
    }
}
