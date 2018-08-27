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
        const string GPMFEXE = "gpmfdemo";

        static void Main(string[] args)
        {
            string path = args[0];
            if (IsFilePath(path))
            {
                string outputUrl = UploadVideo(path);
                Console.WriteLine("Wrote " + path + " to " + outputUrl);
            }
            else
            {
                WalkDirectories(path);
            }
            /*
            //string videoUrl = args[0];
            //string path = args[1];
            string videoUrl = "https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4";

            string path = DownloadVideo(videoUrl);
            string csv = ParseMetadata(path);
            Console.WriteLine(csv);
            */
        }

        static void WalkDirectories(string path) 
        {
            string[] dirs = Directory.GetDirectories(path);
            for (int i = 0; i < dirs.Length; i++) 
            {
                WalkFiles(dirs[i]);
                WalkDirectories(dirs[i]);

            }         
        }

        static void WalkFiles(string path)
        {
            string[] files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; i++)
            {
                string outputUrl = UploadVideo(files[i]);
                Console.WriteLine("Wrote " + path + " to " + outputUrl);
            }
        }

        /* Checks to see if it's a valid File or Directory.  
            returns True if File, False if Directory, exception if neither.
        */
        static bool IsFilePath(string localFile)
        {
            if (!File.Exists(localFile))
            {
                if (!Directory.Exists(localFile))
                    throw new FileNotFoundException("Invalid file or directory: " + localFile);
                else
                    return false;
            }         
            else 
                return true;
        }

        static string GetBlobName(string localFile)
        {
            if (!File.Exists(localFile))
                throw new FileNotFoundException("Video file does not exist: " + localFile);

            string dir = GetBlobDirectory(localFile);
            string blob = dir + Path.GetFileName(localFile);
            return blob;
        }

        static string GetBlobDirectory(string localFile)
        {
            // Remove HERO5 Black x directory.
            int heroMonikerStart = localFile.IndexOf("HERO");
            if (heroMonikerStart > 0)
            {
                localFile = localFile.Substring(0, heroMonikerStart);
            }

            string dir = "";
            int end = 0, start = localFile.LastIndexOf(Path.DirectorySeparatorChar);
            if (start >= 0)
            {
                for (int i = start - 1; i > 0; i--)
                {
                    if (localFile[i] == Path.DirectorySeparatorChar)
                    {
                        end = i;
                        break;
                    }
                }
                dir = localFile.Substring(end + 1, start - (end + 1));
            }
            if (dir != string.Empty)
                dir += "/";
            return dir;
        }

        static string UploadVideo(string localFile)
        {
            // DefaultEndpointsProtocol=https;AccountName=delormej;AccountKey=4Ewy9Alh/F4wqePCTtZl9Pd7o8JWXkKCMVOUCSVJs1p46z1lrBthq9/3tBB8bE+iIuXFOgELWfzpYACUA3LozQ==;EndpointSuffix=core.windows.net
            CloudStorageAccount account = null;
            string connection = Environment.GetEnvironmentVariable(ENV_SKIBLOBS);
            string blobName = GetBlobName(localFile);
            CloudBlockBlob blob;

            if (CloudStorageAccount.TryParse(connection, out account))
            {
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
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GPMFEXE,
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
