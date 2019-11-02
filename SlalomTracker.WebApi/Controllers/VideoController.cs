using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using SlalomTracker.Cloud;
using System.Threading.Tasks;

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
        public async Task<IActionResult> StartProcessVideo()
        {
            string videoUrl = ""; 
            try
            {
                videoUrl = GetVideoUrlFromRequest();
                var response = await CreateContainerInstance(videoUrl);
                if (response.StatusCode != HttpStatusCode.OK)
                    return StatusCode(500, response);
                else
                    return StatusCode(200, response);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error creating container instance for {videoUrl}\nError:{e}");
                return StatusCode(500, e.Message);
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
            Uri uri = new Uri(videoUrl);
            return uri.LocalPath;
        }        

        private Task<HttpResponseMessage> CreateContainerInstance(string videoUrl)
        {
            // Construct end point url.
            string baseUrl = Environment.GetEnvironmentVariable("SKIJOBS_SERVICE");
            string url = baseUrl + "/aci/create";
            Console.WriteLine("Calling /aci/create @" + baseUrl);

            string content = JsonConvert.SerializeObject(videoUrl);

            // Encode parameters.
            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            HttpClient client = new HttpClient();                 
            return client.PostAsync(url, httpContent);
        }
    }
}
