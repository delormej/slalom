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

        [HttpPost]
        public IActionResult QueueVideo(string videoUrl)
        {
            // To test, the only way videoUrl will get mapped here is if it's in the query string:
            //
            // curl -X POST -d "" "http://localhost:5000/api/image?videoUrl=http://skivideostorage.blob.core.windows.net/ski/2018-05-21/GOPR0084.MP4" 
            //
            try
            {
                Storage storage = new Storage();
                string blobName = GetBlobName(videoUrl);
                storage.Queue.Add(blobName, videoUrl);
                
                Console.WriteLine("Queued video: " + videoUrl);
                return StatusCode(200);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unable queue video for procesasing: " + e.Message);
                return StatusCode(500);
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

        private string GetBlobName(string videoUrl)
        {
            return Storage.GetBlobName(Storage.GetLocalPath(videoUrl));
        }
    }
}
