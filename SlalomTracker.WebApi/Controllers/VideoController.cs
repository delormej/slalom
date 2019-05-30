using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;

namespace SlalomTracker.WebApi.Controllers
{
    //[Route("api/[controller]")]
    [ApiController]
    public class VideoController : Controller
    {
        [HttpPost]
        [Route("api/video")]
        public IActionResult QueueVideo()
        {
            string json = null;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {  
                json = reader.ReadToEnd();
            }

            if (json != null)        
            {
                string url = GetUrlFromEvent(json);
                Console.WriteLine("Url is: " + url);
                return StatusCode(204);
            }
            else
            {
                Console.Error.WriteLine("json was empty.");
            }

            Console.Error.WriteLine("Unable to parse json body.");
            return StatusCode(500);                
        }

        private string GetUrlFromEvent(string message)
        {
            dynamic objectEvent = JsonConvert.DeserializeObject(message);
            string bucketId = objectEvent.message.attributes.bucketId;
            string objectId = objectEvent.message.attributes.objectId;
            string data = objectEvent.message.data;           
            string url = string.Format("https://storage.googleapis.com/{0}/{1}", bucketId, objectId);
            return url;
        }
    }
}

/*
{ 
    "message": 
    {
        "attributes": {
            "bucketId":"skivideos",
            "eventTime":"2019-05-30T00:30:28.286785Z",
            "eventType":"OBJECT_FINALIZE",
            "notificationConfig":"projects/_/buckets/skivideos/notificationConfigs/7",
            "objectGeneration":"1559176228286891",
            "objectId":"Program.cs",
            "payloadFormat":"JSON_API_V1"
        },
        "data":"ewogICJraW5kIjogInN0b3JhZ2Ujb2JqZWN0IiwKICAiaWQiOiAic2tpdmlkZW9zL1Byb2dyYW0uY3MvMTU1OTE3NjIyODI4Njg5MSIsCiAgInNlbGZMaW5rIjogImh0dHBzOi8vd3d3Lmdvb2dsZWFwaXMuY29tL3N0b3JhZ2UvdjEvYi9za2l2aWRlb3Mvby9Qcm9ncmFtLmNzIiwKICAibmFtZSI6ICJQcm9ncmFtLmNzIiwKICAiYnVja2V0IjogInNraXZpZGVvcyIsCiAgImdlbmVyYXRpb24iOiAiMTU1OTE3NjIyODI4Njg5MSIsCiAgIm1ldGFnZW5lcmF0aW9uIjogIjEiLAogICJjb250ZW50VHlwZSI6ICJ0ZXh0L3BsYWluIiwKICAidGltZUNyZWF0ZWQiOiAiMjAxOS0wNS0zMFQwMDozMDoyOC4yODZaIiwKICAidXBkYXRlZCI6ICIyMDE5LTA1LTMwVDAwOjMwOjI4LjI4NloiLAogICJzdG9yYWdlQ2xhc3MiOiAiTkVBUkxJTkUiLAogICJ0aW1lU3RvcmFnZUNsYXNzVXBkYXRlZCI6ICIyMDE5LTA1LTMwVDAwOjMwOjI4LjI4NloiLAogICJzaXplIjogIjczNTciLAogICJtZDVIYXNoIjogIkI1N3FLbkNOa0M0NnpiZldPQ2w5SkE9PSIsCiAgIm1lZGlhTGluayI6ICJodHRwczovL3d3dy5nb29nbGVhcGlzLmNvbS9kb3dubG9hZC9zdG9yYWdlL3YxL2Ivc2tpdmlkZW9zL28vUHJvZ3JhbS5jcz9nZW5lcmF0aW9uPTE1NTkxNzYyMjgyODY4OTEmYWx0PW1lZGlhIiwKICAiY3JjMzJjIjogIjY4cm5aZz09IiwKICAiZXRhZyI6ICJDS3ZqNm9tQXd1SUNFQUU9Igp9Cg==",
        "messageId":"566505797466534",
        "message_id":"566505797466534",
        "publishTime":"2019-05-30T00:30:28.699Z",
        "publish_time":"2019-05-30T00:30:28.699Z"
    },
    "subscription":"projects/halogen-premise-241213/subscriptions/onVideoPush"
}
*/
