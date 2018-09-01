using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.Drawing.Imaging;

namespace SlalomTracker
{
    [TestClass]
    public class CoursePassImageTest
    {
        [TestMethod]
        public void TestDraw()
        {
            int scaleFactor = 5;
            CoursePass pass = CoursePassTest.TestTrack(14.0, 0, 13.0);
            CoursePassImage image = new CoursePassImage(pass, scaleFactor);
            Bitmap bitmap = image.Draw();

            bitmap.Save("pass.png", ImageFormat.Png);
        }
    }
}
