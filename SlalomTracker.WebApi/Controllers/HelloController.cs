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
            SkiVideoEntity video = new SkiVideoEntity();
            string version = video.SlalomTrackerVersion;
            string msi = System.Environment.GetEnvironmentVariable("MSI_ENDPOINT");
            string containerImage = System.Environment.GetEnvironmentVariable("SKICONSOLE_IMAGE");
            return Json("Version = " + version + "\nMSI: " + msi + "\nIMAGE: " + containerImage);
        }
    }
}