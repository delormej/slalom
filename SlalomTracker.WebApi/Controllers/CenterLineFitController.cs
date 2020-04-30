using System;
using SlalomTracker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SlalomTracker.WebApi.Controllers
{
    [ApiController]
    public class CenterLineFitController : Controller
    {
        ILogger<CenterLineFitController> _logger;
        private readonly IConfiguration _config;

        public CenterLineFitController(ILogger<CenterLineFitController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        [Route("api/cl/")]
        public IActionResult Get(string jsonUrl)
        {
            double centerLineOffset = 0;
            try
            {
                CoursePassFactory factory = new CoursePassFactory();
                centerLineOffset = factory.FitPass(jsonUrl);
                return Content(centerLineOffset.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to fit pass: " + e.Message);
                return StatusCode(500);
            }
        }      
    }
}
