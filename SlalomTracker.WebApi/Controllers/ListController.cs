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
                Task<List<SkiVideoEntity>> task = storage.GetAllMetdata();
                task.Wait();
                List<SkiVideoEntity> list = task.Result;
                var newestFirst = list.OrderByDescending(s => s.Timestamp);   
                this.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");  
                return Json(newestFirst);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unale to list all blobs. " + e);
                return StatusCode(500);
            }
        }
    }
}
