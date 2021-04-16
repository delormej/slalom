using System;
using SlalomTracker.Cloud;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace SlalomTracker.WebApi.Controllers
{
    [ApiController]
    public class VttController : Controller
    {
        ILogger<VttController> _logger;
        private readonly IConfiguration _config;

        public VttController(ILogger<VttController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        [Route("api/vtt/{recordedDate}/{mp4Filename}")]
        public IActionResult Get(string recordedDate, string mp4Filename)
        {
            string vttContent;
            try
            {
                GoogleStorage storage = new GoogleStorage(_config["FIRESTORE_PROJECT_ID"]);
                SkiVideoEntity entity = storage.GetSkiVideoEntity(recordedDate, mp4Filename);
                if (entity == null)
                    throw new ApplicationException($"Unable to load SkiVideo {recordedDate}, {mp4Filename}");
                WebVtt vtt = new WebVtt(entity);
                vttContent = vtt.Create();
                
                _logger.LogInformation($"Created WebVtt for {recordedDate}, {mp4Filename}");

                return Content(vttContent, new MediaTypeHeaderValue("text/vtt"));
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to create WebVtt: " + e.Message);
                return StatusCode(500);
            }
        }      
    }
}
