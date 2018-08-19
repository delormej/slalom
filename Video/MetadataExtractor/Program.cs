using System;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace MetadataExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            //string videoUrl = args[0];
            //string path = args[1];
            string videoUrl = "https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4";

            string path = DownloadVideo(videoUrl);
            string csv = ParseMetadata(path);
            Console.WriteLine(csv);
            
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
