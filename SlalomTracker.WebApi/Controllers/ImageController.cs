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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SlalomTracker.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Produces("image/png")]
    public class ImageController : Controller
    {
        ILogger<ImageController> _logger;

        public ImageController(ILogger<ImageController> logger, IConfiguration config)
        {
            _logger = logger;
        }


        [HttpGet]
        public IActionResult Get(string jsonUrl, double cl = 0, double rope = 15)
        {
            try
            {
                if (cl == 0) 
                {
                    _logger.LogInformation("Attempting CenterLineOffset fit.");
                    CoursePassFactory factory = new CoursePassFactory();
                    cl = factory.FitPass(jsonUrl);
                }

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
                RopeLengthOff = rope,
                Course = GetCourseFromMetadata(storage, jsonUrl) 
            };
            CoursePass pass = factory.FromUrl(jsonUrl);
            if (pass == null)
                throw new ApplicationException($"Unable to create a pass for {jsonUrl}");  

            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();
            return bitmap;
        }

        private Course GetCourseFromMetadata(Storage storage, string jsonUrl)
        {
            string date = ParseDate(jsonUrl);
            string filename = GetMP4FromJsonUrl(jsonUrl);
            SkiVideoEntity entity = storage.GetSkiVideoEntity(date, filename);
            
            // Should probably throw an exception here? 
            if (entity == null)
            {
                Console.WriteLine($"Couldn't load video metadata for {jsonUrl}");
                return null;
            }

            if (string.IsNullOrEmpty(entity.CourseName))
            {
                Console.WriteLine($"No course saved for {jsonUrl}");
                return null;
            }
            
            KnownCourses courses = new KnownCourses();
            return courses.ByName(entity.CourseName);
        }

        private string ParseDate(string jsonUrl)
        {
            // https://skivideostorage.blob.core.windows.net/ski/2019-10-10/GOPR2197_ts.json
            // YYYY-MM-DD
            string date = "";
            string[] parts = jsonUrl.Split('/');
            if (parts.Length > 2)
                // return 2nd to last element.
                date = parts[parts.Length - 2];
            return date;
        }

        private string GetMP4FromJsonUrl(string jsonUrl)
        {
            // GOPR01444.MP4
            string file = Path.GetFileNameWithoutExtension(jsonUrl);
            return file + ".MP4";
        }
    }
}
