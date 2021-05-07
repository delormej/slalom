using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SlalomTracker.Cloud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SlalomTracker.WebApi.Controllers
{
    
    [ApiController]
    public class ListController : Controller
    {
        ILogger<ListController> _logger;
        IStorage _storage;

        public ListController(ILogger<ListController> logger, IConfiguration config)
        {
            _logger = logger;
            _storage = new GoogleStorage(config["FIRESTORE_PROJECT_ID"]);
        }


        [Route("api/[controller]")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try 
            {
                var metadata = await GetAllMetadataAsync();
                return Json(metadata);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to list all blobs.");
                return StatusCode(500);
            }
        }

        [Route("api/[controller]/summary")]
        [HttpGet]
        public async Task<IActionResult> Summary()
        {
            var metadata = await GetAllMetadataAsync();

            var dates = from d in metadata.Dates()
                select new { Date = d.Key, Count = d.Count() };

            var skiers = from s in metadata.Skiers()
                select new { Skier = s.Key, Count = s.Count() };

            var ropes = from r in metadata.RopeLengths()
                select new { RopeLength = r.Key.ToString(), Count = r.Count() };

            return Json( new { Dates = dates, Skiers = skiers, Ropes = ropes } );
        }

        private async Task<IOrderedEnumerable<SkiVideoEntity>> GetAllMetadataAsync()
        {
            IEnumerable<SkiVideoEntity> list = await _storage.GetAllMetdataAsync();
            var filtered = list.Where(s => s.MarkedForDelete == false);
            var newestFirst = filtered.OrderByDescending(s => s.RecordedTime).ThenBy(s => s.Timestamp);   

            return newestFirst;
        }

        private IEnumerable<KeyValuePair<string, int>> GetSummary(IEnumerable<IGrouping<object, SkiVideoEntity>> list)
        {
            List<KeyValuePair<string, int>> kvp = new List<KeyValuePair<string, int>>();
            void Add(string key, int count) => kvp.Add(KeyValuePair.Create(key, count));

            foreach (var g in list)
                Add(g.Key.ToString(), g.Count());     

            return kvp;       
        }
    }
}
