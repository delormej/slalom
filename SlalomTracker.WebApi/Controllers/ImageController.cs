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
using GeoCoordinatePortable;

namespace SlalomTracker.WebApi.Controllers
{
    [ApiController]
    public class ImageController : Controller
    {
        ILogger<ImageController> _logger;
        AzureStorage _storage;

        public ImageController(ILogger<ImageController> logger, IConfiguration config)
        {
            _logger = logger;
            _storage = new AzureStorage();
        }

        [HttpGet]
        [Route("api/image/{recordedDate}/{mp4Filename}")]
        public IActionResult Get(string recordedDate, string mp4Filename, double cl = 0,
            string course55Entry = null, string course55Exit = null)
        {
            try
            {
                SkiVideoEntity video = _storage.GetSkiVideoEntity(recordedDate, mp4Filename);
                if (video == null)
                    throw new ApplicationException($"Unable to find video entity {mp4Filename} recorded on {recordedDate}.");

                if (cl != 0)
                    video.CenterLineDegreeOffset = cl;

                Course course = GetCourse(video, course55Entry, course55Exit);
                Bitmap image = GetImage(video, course);
                
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return File(ms.ToArray(), "image/png");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Unable to create image for video entity {mp4Filename} recorded on {recordedDate}.", e);
                return StatusCode(500);
            }
        }

        private Bitmap GetImage(SkiVideoEntity video, Course course)
        {
            string http = _storage.BlobStorageUri.Replace("https:", "http:");
            string https = _storage.BlobStorageUri;
            string jsonUrl = video.JsonUrl;

            if (!(jsonUrl.StartsWith(http) || jsonUrl.StartsWith(https)))
            {
                if (jsonUrl.StartsWith("/"))
                    jsonUrl = jsonUrl.TrimStart('/');
                jsonUrl = _storage.BlobStorageUri + "ski/" + jsonUrl;
            }

            CoursePass pass = GetCoursePass(video);
            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();
            return bitmap;
        }

        private Course GetCourse(SkiVideoEntity video, string course55Entry, string course55Exit)
        {
            Course course = null;
            
            if (course55Entry != null && course55Exit != null)
            {
                GeoCoordinate entry = GeoCoordinateConverter.FromLatLon(course55Entry);
                GeoCoordinate exit = GeoCoordinateConverter.FromLatLon(course55Exit);

                course = new Course(entry, exit);
            }
            else
            {
                if (string.IsNullOrEmpty(video.CourseName))
                    throw new ApplicationException($"No course saved for {video.Url}.");
                
                KnownCourses courses = new KnownCourses();
                course = courses.ByName(video.CourseName);
            }

            return course;
        }

        private CoursePass GetCoursePass(SkiVideoEntity video)
        {
            CoursePassFactory factory = new CoursePassFactory();
            CoursePass pass = factory.FromSkiVideo(video); 
            if (pass == null)
                throw new ApplicationException($"Unable to create a pass for {video.Url}");              
            
            return pass;
        }
    }
}
