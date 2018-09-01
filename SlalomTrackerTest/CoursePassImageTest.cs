using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SlalomTracker
{
    [TestClass]
    public class CoursePassImageTest
    {
        [TestMethod]
        public void TestDraw()
        {
            CoursePass pass = CoursePassTest.TestTrack(14.0, 0, 13.0);
            //CoursePassImage image = new CoursePassImage();
            //image.Draw(pass);
        }
    }
}
