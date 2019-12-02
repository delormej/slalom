using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using SlalomTracker;
using SlalomTracker.Cloud;

namespace SlalomTracker.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelloController : Controller
    {
        [HttpGet]
        public JsonResult Get()
        {
            SkiVideoEntity video = new SkiVideoEntity("http://test/video.MP4", DateTime.Now);
            string version = video.SlalomTrackerVersion;
            return Json($"Version: {version}");
        }
    }
}