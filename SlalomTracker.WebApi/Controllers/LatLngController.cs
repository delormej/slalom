using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SlalomTracker.Cloud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace SlalomTracker.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class LatLngController : Controller
    {
        ILogger<ImageController> _logger;
        AzureStorage _storage;

        public LatLngController(ILogger<ImageController> logger, IConfiguration config)
        {
            _logger = logger;
            _storage = new AzureStorage();
        }

        
        [HttpGet]
        public IActionResult Get(string jsonUrl)
        {
            try
            {
                var measurements = GetMeasurements(jsonUrl);
                string formatted = FormatMeasurements(measurements);

                return Content(formatted, 
                    new MediaTypeHeaderValue("application/json"));
            }
            catch (Exception e)
            {
                _logger.LogError($"Unable to get measurements for {jsonUrl}: {e.Message}");
                return StatusCode(500);
            }
        }

        private List<Measurement> GetMeasurements(string jsonUrl)
        {
            WebClient client = new WebClient();
            string json = client.DownloadString(jsonUrl);
            if (string.IsNullOrEmpty(json))
                throw new ApplicationException("No JSON file at url: " + jsonUrl);            
            
            return Measurement.DeserializeMeasurements(json);
        }

        private string FormatMeasurements(List<Measurement> measurements)
        {
            var query = 
                from m in measurements
                select new { 
                    lat = m.BoatGeoCoordinate.Latitude, 
                    lng = m.BoatGeoCoordinate.Longitude, 
                    time = m.Timestamp.TimeOfDay.TotalSeconds };

            var result = query.ToList();
            string json = System.Text.Json.JsonSerializer.Serialize(result);
            
            return json;
        }
    }
}