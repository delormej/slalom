using System;
using System.Web.Http;
using SlalomTracker.Cloud;

namespace CreatePreviewService
{
    public class PreviewRequest
    {
        public string Video { get; set; }
        public int Entry { get; set; }
        public int Exit { get; set; }
    }

    /* 
     * Image Preview Service is a completely seperate service because it
     * has to run on Windows (full .NET) to use the video image libray.
     * 
     */
    public class CreatePreviewController : ApiController
    {
        private PreviewRequest _request;
        private Storage _storage;
        private string _localPath;
        private string _previewEntryImagePath;
        private string _previewExitImagePath;

        // POST api/values
        [HttpPost]
        public IHttpActionResult Post([FromBody]PreviewRequest value)
        {
            _request = value;
            _storage = new Storage();

            DownloadVideo();
            CreateAndUploadPreviews();
            return Ok();
        }

        private void DownloadVideo()
        {
            _localPath = Storage.DownloadVideo(_request.Video);
        }

        private void CreateAndUploadPreviews()
        {
            string entryPath = PreviewImage.Create(_localPath, "_entry", _request.Entry);
            _storage.UploadVideo(entryPath);

            string exitPath = PreviewImage.Create(_localPath, "_exit", _request.Exit);
            _storage.UploadVideo(exitPath);
        }
    }
}
