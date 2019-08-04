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
            try
            {
                string videoUrl = GetVideoUrlFromRequest();
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
       
        [HttpPost]
        [Route("api/processvideo")]
        public IActionResult StartProcessVideo()
        {
            string videoUrl = ""; 
            try
            {
                videoUrl = GetVideoUrlFromRequest();
                string containerGroup = ContainerInstance.Create(videoUrl);
                return Json(new {ContainerGroup=containerGroup,VideoUrl=videoUrl});
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error creating container instance for {videoUrl}\nError:{e}");
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost]
        [Route("api/acicleanup")]
        public IActionResult DeleteAllContainerGroups()
        {
            try 
            {
                int count = ContainerInstance.DeleteAllContainerGroups();
                var result = new {deletedCount=count};
                return Json(result);
            }
            catch (Exception e)
            {
                string message = $"Error deleting ACI container groups: \n{e.Message}";
                Console.WriteLine(message);
                return StatusCode(500, message);
            }
        }

        [HttpPost]
        [Route("api/updatevideo")]
        public IActionResult UpdateVideo()
        {
            try 
            {
                string json = GetJsonFromBody();
                SkiVideoEntity video = JsonConvert.DeserializeObject<SkiVideoEntity>(json);
                if (video == null)
                {
                    string message = $"Error reading video instance from payload:\n{json}";
                    throw new ApplicationException(message);
                }
                Storage storage = new Storage();
                storage.UpdateMetadata(video);
                this.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");                  
                return StatusCode(200);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return StatusCode(500, e.Message);                
            }
        }

        private string GetVideoUrlFromRequest()
        {
            string json = GetJsonFromBody();
            string videoUrl = QueueMessageParser.GetUrl(json);     
            
            if (videoUrl == null || videoUrl.Trim() == string.Empty)        
            {
                throw new ApplicationException("Unable to find video url in payload: \n" + json);
            }            

            return videoUrl;
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
