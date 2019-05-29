using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

namespace SlalomTracker.WebApi.Controllers
{
    //[Route("api/[controller]")]
    [ApiController]
    public class VideoController : Controller
    {

        [HttpPost]
        [Route("api/body")]
        public string JsonStringBody([FromBody] string content)
        {
            return content;
        }

        [HttpPost]
        [Route("api/video")]
        public string ProcessVideo()
        {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {  
                return reader.ReadToEnd();
            }            
        }
    }
}