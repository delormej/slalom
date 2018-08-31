using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using SlalomTracker;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace MetadataExtractor
{
    class Program
    {
        const string ENV_SKIBLOBS = "skiblobs";

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:\n\t" +
                    "MetadataExtractor -d https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4)\n\t" +
                    "MetadataExtractor - u //files/Go Pro/2018-08-20\n");
                return;
            }

            if (args[0] == "-u")
            {
                // eg. MetadataExtractor -u //files/Go Pro/2018-08-20"
                UploadVideos(args[1]);
            }
            else if (args[0] == "-d")
            {
                // eg. MetadataExtractor -d https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4
                ExtractMetadata(args[1]);
            }
        }

        private static void UploadVideos(string path)
        {
            CloudStorageAccount account = Connect();           
            Storage storage = new Storage(account);
            storage.UploadVideos(path);
        }

        private static void ExtractMetadata(string videoUrl)
        {
            string path = Storage.DownloadVideo(videoUrl);
            Parser parser = new Parser();
            List<Measurement> measurements = parser.LoadFromMp4(path);
            string json = JsonConvert.SerializeObject(measurements);

            
            Console.WriteLine(json);
            // Clean up video.
            //File.Delete(path);
        }

        private static CloudStorageAccount Connect()
        {
            //Connect(@"DefaultEndpointsProtocol=https;AccountName=delormej;AccountKey=4Ewy9Alh/F4wqePCTtZl9Pd7o8JWXkKCMVOUCSVJs1p46z1lrBthq9/3tBB8bE+iIuXFOgELWfzpYACUA3LozQ==;EndpointSuffix=core.windows.net");
            string connection = Environment.GetEnvironmentVariable(ENV_SKIBLOBS);
            CloudStorageAccount account = null;
            if (!CloudStorageAccount.TryParse(connection, out account))
            {
                // Otherwise, let the user know that they need to define the environment variable.
                string error =
                    "A connection string has not been defined in the system environment variables. " +
                    "Add a environment variable named 'skiblobs' with your storage " +
                    "connection string as a value.";
                throw new ApplicationException(error);
            }
            return account;
        }
    }
}
