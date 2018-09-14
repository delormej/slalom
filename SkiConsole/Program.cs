using System;
using SlalomTracker;
using MetadataExtractor;
using System.Drawing.Imaging;
using System.Drawing;

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
                string imagePath = GetImagePath(jsonPath);
                double clOffset = args.Length > 2 ? double.Parse(args[2]) : 0;
                double rope = args.Length > 3 ? int.Parse(args[3]) : 22;

                CoursePass pass = CoursePassFactory.FromFile(jsonPath, clOffset, Rope.Off(rope));
                CoursePassImage image = new CoursePassImage(pass);
                Bitmap bitmap = image.Draw();
                bitmap.Save(imagePath, ImageFormat.Png);
            }
            else
                throw new ApplicationException("Missing execution parameters.");
        }

        private static string GetImagePath(string jsonPath)
        {
            return jsonPath.Replace(".json", ".png");
        }
    }
}
