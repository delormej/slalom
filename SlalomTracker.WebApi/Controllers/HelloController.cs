using System;
using System.Reflection;
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
    public class HelloController : Controller
    {
        [HttpGet]
        public JsonResult Get()
        {
            string version = 
                Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var slalomTrackerVersion = typeof(SlalomTracker.Cloud.SkiVideoEntity).Assembly.GetName();
            string msi = System.Environment.GetEnvironmentVariable("MSI_ENDPOINT");
            string containerImage = System.Environment.GetEnvironmentVariable("SKICONSOLE_IMAGE");
            return Json("Version = " + version + "\nMSI: " + msi + "\nIMAGE: " + containerImage + 
                "\nSlalomTracker v" + slalomTrackerVersion);
        }
    }
}