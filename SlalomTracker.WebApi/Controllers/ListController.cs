using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SlalomTracker.Cloud;

namespace SlalomTracker.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            try 
            {
                Storage storage = new Storage();
                Task<List<SkiVideoEntity>> task = storage.GetAllMetdataAsync();
                task.Wait();
                List<SkiVideoEntity> list = task.Result;
                var filtered = list.Where(s => s.MarkedForDelete == false);
                var newestFirst = filtered.OrderByDescending(s => s.RecordedTime).ThenBy(s => s.Timestamp);   
                return Json(newestFirst);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unable to list all blobs. " + e);
                return StatusCode(500);
            }
        }
    }
}
