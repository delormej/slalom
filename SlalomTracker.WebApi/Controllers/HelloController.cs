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
            string msi = System.Environment.GetEnvironmentVariable("MSI_ENDPOINT") + ", " + 
                System.Environment.GetEnvironmentVariable("MSI_SECRET");
            return Json("Version = " + version + "\nMSI: " + msi);
        }
    }
}