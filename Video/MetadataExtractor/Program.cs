using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace MetadataExtractor
{
    class Program
    {
        const string ENV_SKIBLOBS = "skiblobs";
        const string SKICONTAINER = "ski";

        static void Main(string[] args)
        {
            string path = args[0];
            string outputUrl = UploadVideo(path);
            Console.WriteLine("Wrote " + path + " to " + outputUrl);
            /*
            //string videoUrl = args[0];
            //string path = args[1];
            string videoUrl = "https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4";

            string path = DownloadVideo(videoUrl);
            string csv = ParseMetadata(path);
            Console.WriteLine(csv);
            */
        }

        static string UploadVideo(string localFile)
        {
            // DefaultEndpointsProtocol=https;AccountName=delormej;AccountKey=4Ewy9Alh/F4wqePCTtZl9Pd7o8JWXkKCMVOUCSVJs1p46z1lrBthq9/3tBB8bE+iIuXFOgELWfzpYACUA3LozQ==;EndpointSuffix=core.windows.net
            CloudStorageAccount account = null;
            string connection = Environment.GetEnvironmentVariable(ENV_SKIBLOBS);
            string blobName = Path.GetFileName(localFile);
            CloudBlockBlob blob;

            if (CloudStorageAccount.TryParse(connection, out account))
            {
                Console.WriteLine("Connection string OK!");
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(SKICONTAINER);
                blob = blobContainer.GetBlockBlobReference(blobName);
                var task = blob.UploadFromFileAsync(localFile);
                task.Wait();
            }
            else
            {
                // Otherwise, let the user know that they need to define the environment variable.
                string error =
                    "A connection string has not been defined in the system environment variables. " +
                    "Add a environment variable named 'skiblobs' with your storage " +
                    "connection string as a value.";
                throw new ApplicationException(error);
            }

            string uri = blob.SnapshotQualifiedUri.AbsoluteUri;
            return uri; // URL to the uploaded video.
        }

        static string DownloadVideo(string videoUrl)
        {
            // Hard coded for now.
            string path = "GOPR0194.MP4";
            if (File.Exists(path)) 
            {
                Console.WriteLine("File already exists.");
            }
            else 
            {
                Console.Write("Requesting video: " + videoUrl + " ...");

                WebClient client = new WebClient();
                client.DownloadFile(videoUrl, path);

                Console.Write("DONE\n");
            }

            return path;
        }

        static string ParseMetadata(string path)
        {
            const string gpmfexe = "gpmfdemo";
    
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = gpmfexe,
                    Arguments = path,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }
}
