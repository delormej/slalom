using System;
using System.Collections.Generic;
using SlalomTracker;
using Microsoft.WindowsAzure.Storage;

namespace MetadataExtractor
{
    public class Extract
    {
        const string ENV_SKIBLOBS = "skiblobs";

        public static void UploadVideos(string path)
        {
            Storage storage = new Storage(ConnectToStorage());
            storage.UploadVideos(path);
        }

        public static void ExtractMetadata(string videoUrl)
        {
            string path = Storage.DownloadVideo(videoUrl);
            GpmfParser parser = new GpmfParser();
            List<Measurement> measurements = parser.LoadFromMp4(path);

            Storage storage = new Storage(ConnectToStorage());
            string json = Measurement.ToJson(measurements);
            storage.UploadMeasurements(path, json);
            storage.AddMetadata(path);

            // Clean up video.
            //File.Delete(path);
        }

        public static CloudStorageAccount ConnectToStorage()
        {
            //skiblobs = @"DefaultEndpointsProtocol=https;AccountName=delormej;AccountKey=4Ewy9Alh/F4wqePCTtZl9Pd7o8JWXkKCMVOUCSVJs1p46z1lrBthq9/3tBB8bE+iIuXFOgELWfzpYACUA3LozQ==;EndpointSuffix=core.windows.net"
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
