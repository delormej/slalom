using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Imaging;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace SlalomTracker.WebApi.Controllers
{
    [Route("api/crop")]
    [ApiController]
    public class CropThumbnailController : Controller
    {
        ILogger<CropThumbnailController> _logger;

        public CropThumbnailController(ILogger<CropThumbnailController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get(string thumbnailUrl, int width = 600, int height = 1200, int heightOffset = 0)
        {
            try
            {
                Bitmap image = CropImage(thumbnailUrl, width, height, heightOffset);
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png);
                    return File(ms.ToArray(), "image/png");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to get image.");
                return StatusCode(500);
            }
        }

        private Bitmap CropImage(string url, int cropWidth, int cropHeight, int heightOffset)
        {   
            string filename = DownloadImage(url);
            Bitmap src = Image.FromFile(filename) as Bitmap;
            int x = (src.Width / 2) - (cropWidth / 2);
            int y = (src.Height - cropHeight) - heightOffset;
            Rectangle cropRect = new Rectangle(x, y, cropWidth, cropHeight);
            Bitmap target = new Bitmap(cropWidth, cropHeight);
            
            using(Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height), 
                    cropRect,                        
                    GraphicsUnit.Pixel);
            }
            return target;
        }

        private string DownloadImage(string url)
        {
            string filename = System.IO.Path.GetFileName(url);
            using (WebClient client = new WebClient()) 
            {
                client.DownloadFile(new Uri(url), filename);
            }            
            return filename;
        }        
    }
}