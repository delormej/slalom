using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SlalomTracker.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls(GetUrls());

        private static string GetUrls()
        {
            string port = System.Environment.GetEnvironmentVariable("PORT") ?? "80";
            string protocol = (port == "443" ? "https" : "http");
            string url = string.Format("{0}://*:{1}", protocol, port);
            return url;
        }
    }
}
