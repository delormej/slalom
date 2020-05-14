using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using SlalomTracker.Cloud;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SlalomTracker.WebApi.Services;

namespace SlalomTracker.WebApi.Controllers
{
    [ApiController]
    public class VideoController : Controller
    {
        ILogger<VideoController> _logger;
        private readonly IConfiguration _config;

        public VideoController(ILogger<VideoController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

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
                
                _logger.LogInformation($"Queued video: {videoUrl}");
                return StatusCode(200);
            }
            catch (Exception e)
            {
                _logger.LogError("Unable queue video for processing: " + e.Message);
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
                {
                    string message = ReadResponse(response);
                    _logger.LogError(message);
                    return StatusCode(500, message);
                }
                else
                {
                    string message = ReadResponse(response);
                    return StatusCode(200, message);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error creating container instance for {videoUrl}\nError:{e}");
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
                _logger.LogError(e.Message);
                return StatusCode(500, e.Message);                
            }
        }

        [HttpGet]
        [Route("api/uploaded")]
        public async Task<IActionResult> GetUploadedQueueAsync()
        {
            const string ENV_SERVICEBUS = "SKISB";
            const string QueueName = "video-uploaded";            
            Microsoft.Azure.ServiceBus.Core.MessageReceiver mr = new Microsoft.Azure.ServiceBus.Core.MessageReceiver(_config[ENV_SERVICEBUS], QueueName);
            var messages = await mr.PeekAsync(int.MaxValue);
            StringBuilder sb = new StringBuilder();
            if (messages.Count > 0)
            {
                foreach (var message in messages)
                    sb.Append(Encoding.UTF8.GetString(message.Body));
            }

            return Content(sb.ToString());
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
            string baseUrl = _config["SKIJOBS_SERVICE"];
            string url = baseUrl + "/aci/create";
            _logger.LogInformation($"Calling {url} for video {videoUrl}");

            string content = JsonConvert.SerializeObject(videoUrl);

            // Encode parameters.
            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            HttpClient client = new HttpClient();                 
            return client.PostAsync(url, httpContent);
        }

        private string ReadResponse(HttpResponseMessage response)
        {
            var task = response.Content.ReadAsStringAsync();
            task.Wait();
            return task.Result;
        }
    }
}
