using System;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SlalomTracker.Cloud;

namespace SlalomTracker.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            var host = CreateWebHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"Starting with Version: {GetVersion()}");            
            host.Run();
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

        private static string GetVersion()
        {
            SkiVideoEntity video = new SkiVideoEntity("http://test/video.MP4", DateTime.Now);
            string version = video.SlalomTrackerVersion;
            return version;
        }
    }
}
