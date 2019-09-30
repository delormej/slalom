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
using SlalomTracker.Cloud;

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
            try
            {
                Bitmap image = GetImage(jsonUrl, cl, rope);
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return File(ms.ToArray(), "image/png");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unable to get image. " + e.Message);
                return StatusCode(500);
            }
        }

        private Bitmap GetImage(string jsonUrl, double clOffset, double rope)
        {
            Storage storage = new Storage();
            string http = storage.BlobStorageUri.Replace("https:", "http:");
            string https = storage.BlobStorageUri;
            if (!(jsonUrl.StartsWith(http) || jsonUrl.StartsWith(https)))
            {
                if (jsonUrl.StartsWith("/"))
                    jsonUrl = jsonUrl.TrimStart('/');
                jsonUrl = storage.BlobStorageUri + "ski/" + jsonUrl;
            }

            CoursePassFactory factory = new CoursePassFactory() 
            {
                CenterLineDegreeOffset = clOffset,
                RopeLengthOff = rope
            };
            CoursePass pass = factory.FromUrl(jsonUrl);
            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();
            return bitmap;
            //bitmap.Save(imagePath, ImageFormat.Png);
        }
    }
}
