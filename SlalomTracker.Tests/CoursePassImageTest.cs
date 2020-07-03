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
            CoursePassFactory factory = new CoursePassFactory();
            factory.RopeLengthOff = 32;
            factory.CenterLineDegreeOffset = 19;
            CoursePass pass = factory.FromLocalJsonFile("./Video/GOPR0565.json");
            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();

            bitmap.Save("pass.png", ImageFormat.Png);
        }
    }
}
