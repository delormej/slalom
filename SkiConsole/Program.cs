using System;
using SlalomTracker;
using SlalomTracker.Cloud;
using MetadataExtractor;
using System.Drawing.Imaging;
using System.Drawing;
using System.Diagnostics;

namespace SkiConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[0] == "-u" && args.Length >= 2)
            {
                // eg. ski -u //files/Go Pro/2018-08-20"
                // OR individual file:
                // ski -u //files/Go Pro/2018-08-20/GOPR0565.MP4
                UploadVideos(args[1]);
            }
            else if (args[0] == "-d" && args.Length >= 2)
            {
                // eg. ski -d https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4
                DownloadVideo(args[1]);
            }
            else if (args[0] == "-e" && args.Length >= 3)
            {
                // eg. ski -e 2018-06-20/GOPR0194.MP4 GOPR0194.json
                ExtractMetadataAsJson(args[1], args[2]);
            }
            else if (args[0] == "-i" && args.Length >= 4)
            {
                // eg. ski -i GOPR0194.json 0 22
                string jsonPath = args[1];
                double clOffset = args.Length > 2 ? double.Parse(args[2]) : 0;
                int rope = args.Length > 3 ? int.Parse(args[3]) : 22;

                string imagePath = CreateImage(jsonPath, clOffset, rope);
            }
            else
                ShowUsage();
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage:\n\t" +
                                "ski -d https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4)\n\t" +
                                "ski -u //files/Go Pro/2018-08-20\n" +
                                "ski -e 2018-06-20/GOPR0194.MP4 GOPR0194.json\n" +
                                "ski -i GOPR0194.json 0 22\n"
                            );
        }

        private static void UploadVideos(string localPath)
        {
            Storage storage = new Storage();
            if (IsDirectory(localPath))
                storage.UploadVideos(localPath);
            else
                UploadVideo(localPath);
        }

        public static void UploadVideo(string localPath)
        {
            Storage storage = new Storage();
            string url = storage.UploadVideo(localPath);
        }

        private static void DownloadVideo(string url)
        {
            string localPath = Storage.DownloadVideo(url);
            Console.WriteLine("Downloaded to:\n\t" + localPath);
        }

        private static void ExtractMetadataAsJson(string videoLocalPath, string jsonPath)
        {
            string json = Extract.ExtractMetadata(videoLocalPath);
            System.IO.File.WriteAllText(jsonPath, json);
        }

        private static string CreateImage(string jsonPath, double clOffset, int rope)
        {
            string imagePath = GetImagePath(jsonPath);
            CoursePass pass = CoursePassFactory.FromFile(jsonPath, clOffset, Rope.Off(rope));
            pass = GetBestFit(pass);
            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();
            bitmap.Save(imagePath, ImageFormat.Png);

            Console.WriteLine("Gate precision == {0} for {1}", pass.GetGatePrecision(), jsonPath);

            return imagePath;
        }

        private static bool IsDirectory(string localPath)
        {
            if (System.IO.Directory.Exists(localPath))
                return true;
            else
                return false;
        }

        private static string GetImagePath(string jsonPath)
        {
            return jsonPath.Replace(".json", ".png");
        }

        private static CoursePass GetBestFit(CoursePass pass)
        {
            CoursePass bestPass = CoursePassFactory.FitPass(pass.Measurements, pass.Course, pass.Rope);
            Console.WriteLine("Best pass is {0} CL offset.", bestPass.CenterLineDegreeOffset);
            return bestPass;
        }
    }
}
