using System;
using System.IO;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;

namespace MetadataExtractor
{
    class Program
    {
        const string ENV_SKIBLOBS = "skiblobs";
        static CloudStorageAccount _account;

        static void Main(string[] args)
        {
            //Connect(@"DefaultEndpointsProtocol=https;AccountName=delormej;AccountKey=4Ewy9Alh/F4wqePCTtZl9Pd7o8JWXkKCMVOUCSVJs1p46z1lrBthq9/3tBB8bE+iIuXFOgELWfzpYACUA3LozQ==;EndpointSuffix=core.windows.net");
            Connect(Environment.GetEnvironmentVariable(ENV_SKIBLOBS));
            Storage storage = new Storage(_account);

            string path = args[0];            
            storage.UploadVideos(path);

            /*
            //string videoUrl = args[0];
            //string path = args[1];
            string videoUrl = "https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4";

            string path = DownloadVideo(videoUrl);
            string csv = ParseMetadata(path);
            Console.WriteLine(csv);
            */
        }

        static void Connect(string connection)
        {
            if (!CloudStorageAccount.TryParse(connection, out _account))
            {
                // Otherwise, let the user know that they need to define the environment variable.
                string error =
                    "A connection string has not been defined in the system environment variables. " +
                    "Add a environment variable named 'skiblobs' with your storage " +
                    "connection string as a value.";
                throw new ApplicationException(error);
            }
        }
    }
}
