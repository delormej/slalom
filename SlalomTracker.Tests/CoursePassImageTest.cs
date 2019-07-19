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
            CoursePass pass = CoursePassFactory.FromFile("./Video/GOPR0565.json", 19, 
                Rope.Off(32));
            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();

            bitmap.Save("pass.png", ImageFormat.Png);
        }
    }
}
