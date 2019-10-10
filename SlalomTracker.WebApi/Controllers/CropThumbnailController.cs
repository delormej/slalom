using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Imaging;
using System.Drawing;

namespace SlalomTracker.WebApi.Controllers
{
    [Route("api/crop")]
    [ApiController]
    public class CropThumbnailController : Controller
    {
        [HttpGet]
        public IActionResult Get(string thumbnailUrl)
        {
            try
            {
                Bitmap image = CropImage(thumbnailUrl);
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png);
                    return File(ms.ToArray(), "image/png");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unable to get image. " + e.Message);
                return StatusCode(500);
            }
        }
        private Bitmap CropImage(string url)
        {
            const int cropWidth = 400;
            const int cropHeight = 800;
            
            string filename = DownloadImage(url);
            Bitmap src = Image.FromFile(filename) as Bitmap;
            int x = (src.Width / 2) - cropWidth;
            int y = (src.Height - cropHeight);
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