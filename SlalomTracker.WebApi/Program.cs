using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Google.Cloud.Logging.Console;
using SlalomTracker.Cloud;

namespace SlalomTracker.WebApi
{
    public class Program
    {
        public static Task Main(string[] args) 
        {
            var host = CreateHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"Starting with Version: {GetVersion()}");            
            return host.RunAsync();
        }           

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(c =>
                    c.AddJsonFile("config/appsettings.json", optional: true)
                )
                .ConfigureLogging(loggingBuilder => loggingBuilder
                    .AddConsoleFormatter<GoogleCloudConsoleFormatter, 
                        GoogleCloudConsoleFormatterOptions>(options => options.IncludeScopes = true)
                    .AddConsole(options => options.FormatterName = nameof(GoogleCloudConsoleFormatter)))                
                .ConfigureWebHostDefaults(builder => {
                    builder.UseStartup<Startup>();
                    builder.UseUrls(GetUrls());
                });    

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
