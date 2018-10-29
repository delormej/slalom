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

namespace SlalomTracker.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Produces("image/png")]
    public class ValuesController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            Bitmap canvas = new Bitmap(400,400);
            Graphics graphics = Graphics.FromImage(canvas);
            graphics.DrawLine(new Pen(Color.Pink, 3), new Point(1,1), new Point(300, 300));
            
            using (var ms = new MemoryStream())
            {
                canvas.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return File(ms.ToArray(), "image/png");
            }
        }
    }
}
