using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

namespace SlalomTracker.WebApi.Controllers
{
    //[Route("api/[controller]")]
    [ApiController]
    public class VideoController : Controller
    {

        [HttpPost]
        [Route("api/body")]
        public string JsonStringBody([FromBody] string content)
        {
            return content;
        }

        [HttpPost]
        [Route("api/video")]
        public string ProcessVideo()
        {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {  
                return reader.ReadToEnd();
            }          
/*
root@d62ac81750f9:/# gcloud pubsub subscriptions pull --format json --auto-ack onVideoUploadPull
[
  {
    "ackId": "BCEhPjA-RVNEUAYWLF1GSFE3GQhoUQ5PXiM_NSAoRRIBCBQFfH1xQV51VVUaB1ENGXJ8ZydqWxUFAhMGeVVbEQ16bVxXOFQLHHV7Z31pXxIDC0dQd3eJ_ZPB-eXFNksxIdaU5qJfeu63saRiZho9XxJLLD5-PTNFQV5AEkw2BkRJUytDCypYEU4",
    "message": {
      "attributes": {
        "bucketId": "skivideos",
        "eventTime": "2019-05-29T02:21:03.066822Z",
        "eventType": "OBJECT_FINALIZE",
        "notificationConfig": "projects/_/buckets/skivideos/notificationConfigs/7",
        "objectGeneration": "1559096463067100",
        "objectId": "server.js",
        "payloadFormat": "JSON_API_V1"
      },
      "data": "ewogICJraW5kIjogInN0b3JhZ2Ujb2JqZWN0IiwKICAiaWQiOiAic2tpdmlkZW9zL3NlcnZlci5qcy8xNTU5MDk2NDYzMDY3MTAwIiwKICAic2VsZkxpbmsiOiAiaHR0cHM6Ly93d3cuZ29vZ2xlYXBpcy5jb20vc3RvcmFnZS92MS9iL3NraXZpZGVvcy9vL3NlcnZlci5qcyIsCiAgIm5hbWUiOiAic2VydmVyLmpzIiwKICAiYnVja2V0IjogInNraXZpZGVvcyIsCiAgImdlbmVyYXRpb24iOiAiMTU1OTA5NjQ2MzA2NzEwMCIsCiAgIm1ldGFnZW5lcmF0aW9uIjogIjEiLAogICJjb250ZW50VHlwZSI6ICJ0ZXh0L2phdmFzY3JpcHQiLAogICJ0aW1lQ3JlYXRlZCI6ICIyMDE5LTA1LTI5VDAyOjIxOjAzLjA2NloiLAogICJ1cGRhdGVkIjogIjIwMTktMDUtMjlUMDI6MjE6MDMuMDY2WiIsCiAgInN0b3JhZ2VDbGFzcyI6ICJORUFSTElORSIsCiAgInRpbWVTdG9yYWdlQ2xhc3NVcGRhdGVkIjogIjIwMTktMDUtMjlUMDI6MjE6MDMuMDY2WiIsCiAgInNpemUiOiAiNTY3IiwKICAibWQ1SGFzaCI6ICJSeTNQV0ZlK2krUVR6V0pCU0ZGK2lBPT0iLAogICJtZWRpYUxpbmsiOiAiaHR0cHM6Ly93d3cuZ29vZ2xlYXBpcy5jb20vZG93bmxvYWQvc3RvcmFnZS92MS9iL3NraXZpZGVvcy9vL3NlcnZlci5qcz9nZW5lcmF0aW9uPTE1NTkwOTY0NjMwNjcxMDAmYWx0PW1lZGlhIiwKICAiY3JjMzJjIjogImY5WmZuZz09IiwKICAiZXRhZyI6ICJDTnlQNlBiV3YrSUNFQUU9Igp9Cg==",
      "messageId": "565766835129239",
      "publishTime": "2019-05-29T02:21:03.632Z"
    }
  }
]
*/              
        }
    }
}