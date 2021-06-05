using Microsoft.Extensions.Logging;
using Google.Cloud.Logging.Console;

namespace SlalomTracker.Logging
{
    public static class SkiLogger
    {
        public static ILoggerFactory Factory =
                LoggerFactory.Create(builder =>
                {
                    builder.AddConsoleFormatter<GoogleCloudConsoleFormatter, 
                        GoogleCloudConsoleFormatterOptions>(options => 
                            options.IncludeScopes = true);
                    builder.AddConsole(options => 
                        options.FormatterName = nameof(GoogleCloudConsoleFormatter));
                });
    }
}