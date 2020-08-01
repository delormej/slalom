using System;
using System.Net;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
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

                Measurement measurement = GetMeasurement(entity, seconds);

                return Content(measurement.ToString(), 
                    new MediaTypeHeaderValue("application/json"));
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to find handle position: " + e.Message);
                return StatusCode(500);
            }
        }      

        private Measurement GetMeasurement(SkiVideoEntity entity, double seconds)
        {
            List<Measurement> measurements = null;
            if (!string.IsNullOrWhiteSpace(entity.CourseName))
            {
                CoursePassFactory factory = new CoursePassFactory();
                CoursePass pass = factory.FromSkiVideo(entity);
                measurements = pass.Measurements;
            }
            else
            {
                WebClient client = new WebClient();
                string json = client.DownloadString(entity.JsonUrl);
                measurements = Measurement.DeserializeMeasurements(json);
            }

            Measurement measurement = measurements.
                FindHandleAtSeconds(seconds);
            
            return measurement;            
        }
    }
}
