using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SlalomTracker.WebApi
{
    public class Program
    {
         public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls(GetUrls());
                });

        private static string GetUrls()
        {
            string port = System.Environment.GetEnvironmentVariable("PORT") ?? "80";
            string protocol = (port == "443" ? "https" : "http");
            string url = string.Format("{0}://*:{1}", protocol, port);
            return url;
        }                
    }
}
