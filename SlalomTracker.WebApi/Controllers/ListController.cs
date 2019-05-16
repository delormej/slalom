using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Imaging;
using System.Drawing;
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
                List<Video> list = Video.GetVideoList(storage.GetAllBlobUris());
                return Json(list);
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
