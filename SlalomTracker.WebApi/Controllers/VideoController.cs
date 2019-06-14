using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using SlalomTracker.Cloud;

namespace SlalomTracker.WebApi.Controllers
{
    [ApiController]
    public class VideoController : Controller
    {
        [HttpPost]
        [Route("api/video")]
        public IActionResult QueueVideo()
        {
            string json = GetJsonFromBody();
            string videoUrl = QueueMessageParser.GetUrl(json);
            if (videoUrl == null)        
            {
                Console.Error.WriteLine("Unable to find videoUrl in payload.");
                return StatusCode(500);
            }

            try
            {
                Storage storage = new Storage();
                string blobName = GetBlobName(videoUrl);
                storage.Queue.Add(blobName, videoUrl);
                
                Console.WriteLine("Queued video: " + videoUrl);
                return StatusCode(200);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unable queue video for procesasing: " + e.Message);
                return StatusCode(500);
            }
        }

       private string GetJsonFromBody()
        {
            string json = null;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {  
                json = reader.ReadToEnd();
            }
            return json;
        }
        private string GetBlobName(string videoUrl)
        {
            return Storage.GetBlobName(Storage.GetLocalPath(videoUrl));
        }        
    }
}
