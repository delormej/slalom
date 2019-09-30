﻿using System;
using SlalomTracker;
using SlalomTracker.Cloud;
using SlalomTracker.Video;
using MetadataExtractor;
using System.Drawing.Imaging;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkiConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                ShowUsage();
                return;
            }
            
            if (args[0].StartsWith("debug"))
            {
                args[0] = args[0].Replace("debug", "");
                Console.WriteLine("Press any key to start debugging...");
                Console.ReadKey();
            }

            if (args[0] == "-d" && args.Length >= 2)
            {
                // eg. ski -d https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4
                DownloadVideo(args[1]);
            }
            else if (args[0] == "-e" && args.Length >= 3)
            {
                // eg. ski -e 2018-06-20/GOPR0194.MP4 GOPR0194.json
                ExtractMetadataAsJson(args[1], args[2]);
            }
            else if (args[0] == "-i" && args.Length >= 2)
            {
                if (args.Length >= 4)
                {
                    // eg. ski -i GOPR0194.json 0 22
                    string jsonPath = args[1];
                    double clOffset = args.Length > 2 ? double.Parse(args[2]) : 0;
                    double rope = args.Length > 3 ? double.Parse(args[3]) : 22;

                    string imagePath = CreateImage(jsonPath, clOffset, rope);
                }
                else if (args.Length == 2)
                {
                    // eg. ski -i https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4
                    // does it all
                    string imagePath = DownloadAndCreateImage(args[1]);
                }
            }
            else if (args[0] == "-p" && args.Length >= 2)
            {
                // eg. ski -p https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4
                string url = ProcessVideo(args[1]);
            }
            else if (args[0] == "-m")
            {
                PrintAllMetadata();
            }
            else if (args[0] == "-y")
            {
                UploadYouTube(args[1]);
            }
            else if (args[0] == "-c" && args.Length >= 2)
            {
                PrintCreationTime(args[1]);
            }
            else if (args[0] == "-x") {
                PrintCourses();
            }
            else
                ShowUsage();
        }

        private static void ShowUsage()
        {
            PrintVersion();
            Console.WriteLine("Usage:\n\t" +
                                "Download a video from cloud storage:\n\t\t" +
                                "ski -d https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4)\n\t" +
                                "Extract metadata from MP4 GOPRO file:\n\t\t" +
                                "ski -e 2018-06-20/GOPR0194.MP4 GOPR0194.json\n\t" +
                                "List all metadata stored for videos:\n\t\t" +
                                "ski -m\n\t" +
                                "Generate an image of skiers path from video <center line offset>, <rope length>:\n\t\t" +
                                "ski -i GOPR0194.json 0 22\n\t\t" +
                                "ski -i https://delormej.blob.core.windows.net/ski/2018-08-24/GOPR0565.json 0 22\n\t\t" +
                                "ski -i https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4\n\t" +
                                "Download video, process and upload metadata.\n\t\t" +
                                "ski -p https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4\n\t" +
                                "Update video to YouTube.\n\t\t" +
                                "ski -y 2018-06-20/GOPR0194.MP4\n\t" +
                                "Print video creation time.\n\t\t" +
                                "ski -c 2018-06-20/GOPR0194.MP4\n\t\t" +
                                "Download courses.\n\t\t" +
                                "ski -x"                                 
                            );
        }

        private static void PrintVersion()
        {
            SkiVideoEntity video = new SkiVideoEntity();
            Console.WriteLine("Version: " + video.SlalomTrackerVersion);
        }

        private static string DownloadAndCreateImage(string url)
        {
            string localPath = Storage.DownloadVideo(url);
            string json = Extract.ExtractMetadata(localPath);
            CoursePass pass = CoursePassFactory.FromJson(json);
            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();
            string imagePath = localPath.Replace(".MP4", ".png");
            bitmap.Save(imagePath, ImageFormat.Png);
            return imagePath;
        }

        private static void UploadYouTube(string localPath)
        {
            YouTube youTube = new YouTube();
            youTube.Upload(localPath);
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

        private static string ProcessVideo(string videoUrl)
        {
            SkiVideoProcessor processor = new SkiVideoProcessor();
            string url = processor.Process(videoUrl);
            return url;
        }

        private static string CreateImage(string jsonPath, double clOffset, double rope)
        {
            CoursePass pass;

            if (jsonPath.StartsWith("http"))
                pass = CoursePassFactory.FromUrl(jsonPath, clOffset, rope);
            else
                pass = CoursePassFactory.FromFile(jsonPath, clOffset, Rope.Off(rope));

            string imagePath = GetImagePath(jsonPath);
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
            string file = System.IO.Path.GetFileName(jsonPath);
            return file.Replace(".json", ".png");
        }

        private static CoursePass GetBestCoursePass(CoursePass pass)
        {
            CoursePass bestPass = CoursePassFactory.FitPass(pass.Measurements, pass.Course, pass.Rope);
            Console.WriteLine("Best pass is {0} CL offset.", bestPass.CenterLineDegreeOffset);
            return bestPass;
        }

        private static void PrintAllMetadata()
        {
            Storage storage = new Storage();
            Task<List<SkiVideoEntity>> result = storage.GetAllMetdata();
            result.Wait();
            Console.WriteLine("Videos available:");
            foreach (SkiVideoEntity e in result.Result)
            {
                Console.WriteLine("\t{0}\\{1}", e.PartitionKey, e.RowKey);
            }
        }

        private static void PrintCreationTime(string inputFile)
        {
            VideoTasks video = new VideoTasks();
            DateTime creation = video.GetCreationTime(inputFile);
            Console.WriteLine(
                $"File: {inputFile}, video creationtime " +
                creation.ToString("MM/dd/yyyy h:mm tt"));
        }

        private static void PrintCourses()
        {
            Storage storage = new Storage();
            CourseEntity entity = new CourseEntity();
            entity.PartitionKey = "cochituate";
            entity.RowKey = "outside";
            entity.Course55EntryCL = new GeoCoordinatePortable.GeoCoordinate(-71.35911924, 42.28958014);
            entity.Course55ExitCL = new GeoCoordinatePortable.GeoCoordinate(-71.35950488, 42.28622924);
            storage.UpdateCourseEntity(entity);

            Task<List<CourseEntity>> result = storage.GetCourses();
            result.Wait();
            Console.WriteLine("Courses available:");
            foreach (CourseEntity e in result.Result)
            {
                Console.WriteLine("\t{0}\\{1}\\{2}", e.PartitionKey, e.RowKey, e.Course55EntryCL);
            }
        }
    }
}
