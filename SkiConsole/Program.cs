using System;
using SlalomTracker;
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
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:\n\t" +
                    "ski -d https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4)\n\t" +
                    "ski -u //files/Go Pro/2018-08-20\n" +
                    "ski -i GOPR0194.json 0 22\n"
                );
                return;
            }

            if (args[0] == "-u")
            {
                // eg. MetadataExtractor -u //files/Go Pro/2018-08-20"
                Extract.UploadVideos(args[1]);
            }
            else if (args[0] == "-d")
            {
                // eg. MetadataExtractor -d https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4
                Extract.ExtractMetadata(args[1]);
            }
            else if (args[0] == "-i")
            {
                string jsonPath = args[1];
                double clOffset = args.Length > 2 ? double.Parse(args[2]) : 0;
                int rope = args.Length > 3 ? int.Parse(args[3]) : 22;
                string imagePath = CreateImage(jsonPath, clOffset, rope);
                //LaunchImageViewer(imagePath);
            }
            else
                throw new ApplicationException("Missing execution parameters.");
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

        private static string GetImagePath(string jsonPath)
        {
            return jsonPath.Replace(".json", ".png");
        }

        private static void LaunchImageViewer(string imagePath)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = imagePath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }

        private static CoursePass GetBestFit(CoursePass pass)
        {
            CoursePass bestPass = CoursePassFactory.FitPass(pass.Measurements, pass.Course, pass.Rope);
            Console.WriteLine("Best pass is {0} CL offset.", bestPass.CenterLineDegreeOffset);
            return bestPass;
        }
    }
}
