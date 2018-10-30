using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Imaging;
using System.Drawing;
using SlalomTracker;


namespace SlalomTracker.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Produces("image/png")]
    public class ImageController : Controller
    {
        [HttpGet]
        public IActionResult Get(string jsonUrl, double cl = 0, double rope = 15)
        {
            Bitmap image = GetImage(jsonUrl, cl, rope);
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return File(ms.ToArray(), "image/png");
            }
        }

        private Bitmap GetImage(string jsonUrl, double clOffset, double rope)
        {
            CoursePass pass = CoursePassFactory.FromUrl(jsonUrl, clOffset, rope);
            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();
            return bitmap;
            //bitmap.Save(imagePath, ImageFormat.Png);
        }
    }
}
