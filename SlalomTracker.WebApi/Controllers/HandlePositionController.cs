using System;
using SlalomTracker.Cloud;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SlalomTracker.WebApi.Controllers
{
    [ApiController]
    public class HandlePositionController : Controller
    {
        ILogger<HandlePositionController> _logger;
        private readonly IConfiguration _config;

        public HandlePositionController(ILogger<HandlePositionController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        [Route("api/handle/{seconds}/{recordedDate}/{mp4Filename}")]
        public IActionResult Get(double seconds, string recordedDate, string mp4Filename)
        {
            try
            {
                Storage storage = new Storage();
                SkiVideoEntity entity = storage.GetSkiVideoEntity(recordedDate, mp4Filename);
                if (entity == null)
                    throw new ApplicationException($"Unable to load SkiVideo {recordedDate}, {mp4Filename}");

                CoursePassFactory factory = new CoursePassFactory();
                CoursePass pass = factory.FromSkiVideo(entity);
                Measurement measurement = pass.FindHandleAtSeconds(seconds + entity.EntryTime);
                
                return Content(measurement.ToString(), new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to find handle position: " + e.Message);
                return StatusCode(500);
            }
        }      
    }
}
