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
                return Json(newestFirst);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unale to list all blobs. " + e);
                return StatusCode(500);
            }
        }

        private class Video
        {
            public string VideoUrl;
            public bool HasJson;

            public Video(string url)
            {
                this.VideoUrl = url;
                this.HasJson = false;
            }

            private static string JsonToMp4(string uri)
            {
                return uri.ToLower().Replace(".json", ".mp4");
            }

            public static List<Video> GetVideoList(IEnumerable<string> blobUris)
            {
                List<Video> list = new List<Video>();
                foreach (string uri in blobUris.Where(s => s.ToLower().EndsWith(".mp4")))
                    list.Add(new Video(uri));

                foreach (string uri in blobUris.Where(s => s.ToLower().EndsWith(".json")))
                {
                    Video video = list.Find(v => v.VideoUrl.ToLower() == JsonToMp4(uri));
                    if (video != null)
                        video.HasJson = true;
                    else 
                        Console.WriteLine("[WARN] GetVideoList - Missing mp4 for json: " + uri);
                }

                return list;
            }
        }
    }
}
