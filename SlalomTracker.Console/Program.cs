using System;
using SlalomTracker;
using SlalomTracker.Cloud;
using SlalomTracker.Video;
using MetadataExtractor;
using System.Drawing.Imaging;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;

namespace SkiConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            PrintVersion();
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

                    // Grab geo coordinates if passed.
                    CourseCoordinates coords = CourseCoordinates.Default;
                    if (args.Length >= 8)
                    {
                        coords = new CourseCoordinates() {
                            EntryLat = double.Parse(args[4]),
                            EntryLon = double.Parse(args[5]),
                            ExitLat = double.Parse(args[6]),
                            ExitLon = double.Parse(args[7])
                        };
                    }

                    string imagePath = CreateImage(jsonPath, clOffset, rope, coords);
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
            else if (args[0] == "-t") 
            {
                Train();
            }
            else if (args[0] == "-x")
            {
                PrintCourses();
                if (args.Length > 3) {
                    string course = args[1];
                    double meters = double.Parse(args[2]);
                    double heading = double.Parse(args[3]);
                    GetNewCoords(course, meters, heading);
                }
            }
            else if (args[0] == "-s" && args.Length > 2) 
            {
                OutputHandleSpeed(args[1], double.Parse(args[2]));
            }
            else
                ShowUsage();
        }

        private static void ShowUsage()
        {
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
                                "ski -i https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4\n\t\t" +
                                "ski -i https://skivideostorage.blob.core.windows.net/ski/2019-09-27/GOPR2170_ts.json 0 32 42.286974 -71.36495 42.285677 -71.362336\n\t" +
                                "Download video, process and upload metadata.\n\t\t" +
                                "ski -p https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4\n\t" +
                                "Update video to YouTube.\n\t\t" +
                                "ski -y 2018-06-20/GOPR0194.MP4\n\t" +
                                "Print video creation time.\n\t\t" +
                                "ski -c 2018-06-20/GOPR0194.MP4\n\t\t" +
                                "Download courses.\n\t\t" +
                                "ski -x\n\t\t" +
                                "Output handle speed.\n\t\t" +
                                "ski -s\n\t\t" +
                                "Train the model with all the data we have.\n\t\t" +
                                "ski -t\n\t\t"                                       
                            );
        }

        private static void PrintVersion()
        {
            SkiVideoEntity video = new SkiVideoEntity("http://test/test", DateTime.Now);
            Console.WriteLine("Version: " + video.SlalomTrackerVersion);
        }

        private static string DownloadAndCreateImage(string url)
        {
            string localPath = Storage.DownloadVideo(url);
            string json = Extract.ExtractMetadata(localPath);
            CoursePass pass = new CoursePassFactory().FromJson(json);
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

        private static void Train()
        {
            var metadataTask = LoadVideosAsync();
            Console.WriteLine("Loading videos to train.");
            metadataTask.Wait();
            List<SkiVideoEntity> videos = metadataTask.Result;
            
            Console.WriteLine("Training rope length detection.");
            RopeMachineLearning ropeMl = new RopeMachineLearning();
            ropeMl.Train(videos);

            // Console.WriteLine("Training skier detection.");
            // SkierMachineLearning skierMl = new SkierMachineLearning();
            // skierMl.Train(videos);
        }

        private static string CreateImage(string jsonPath, double clOffset, double rope, 
            CourseCoordinates coords)
        {
            CoursePass pass;
            CoursePassFactory factory = new CoursePassFactory();
            factory.CenterLineDegreeOffset = clOffset;
            factory.RopeLengthOff = rope;
            factory.Course55Coordinates = coords;

            if (jsonPath.StartsWith("http")) 
                pass = factory.FromUrl(jsonPath);
            else
                pass = factory.FromFile(jsonPath);

            string imagePath = GetImagePath(jsonPath);
            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();
            bitmap.Save(imagePath, ImageFormat.Png);

            Console.WriteLine("Gate precision == {0} for {1}", pass.GetGatePrecision(), jsonPath);
            Console.WriteLine("Wrote image to: " + imagePath);

            return imagePath;
        }

        private static void OutputHandleSpeed(string jsonPath, double rope)
        {
            if (!jsonPath.StartsWith("http")) 
                throw new ApplicationException("Must pass http path to jsonUrl.");

            CoursePassFactory factory = new CoursePassFactory();
            factory.RopeLengthOff = rope;
            CoursePass pass = factory.FromUrl(jsonPath);     
       
            foreach(var m in pass.Measurements) 
            {
                Console.WriteLine($"{m.Timestamp.ToString("ss.fff")}, {m.HandleSpeedMps}");
            }
        }

        private static string GetImagePath(string jsonPath)
        {
            string file = System.IO.Path.GetFileName(jsonPath);
            return file.Replace(".json", ".png");
        }

        private static void PrintAllMetadata()
        {
            var metadataTask = LoadVideosAsync();
            metadataTask.Wait();
            Console.WriteLine("Videos available:");
            foreach (SkiVideoEntity e in metadataTask.Result)
            {
                Console.WriteLine("\t{0}\\{1}", e.PartitionKey, e.RowKey);
            }
        }

        private static Task<List<SkiVideoEntity>> LoadVideosAsync()
        {
            Storage storage = new Storage();
            return storage.GetAllMetdataAsync();
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
            KnownCourses knownCourses = new KnownCourses();
            // One time run only:
            // knownCourses.AddKnownCourses();
            Console.WriteLine("Courses available:");
            foreach (Course c in knownCourses.List)
            {
                Console.WriteLine("\tName:{0}, Entry(Lat/Lon):{1}\\{2}, Heading:{3}", 
                    c.Name, 
                    c.Course55EntryCL.Latitude, 
                    c.Course55EntryCL.Longitude,
                    c.GetCourseHeadingDeg());
            }
        }

        private static void GetNewCoords(string courseName, double meters, double heading)
        {
            Console.WriteLine($"Moving {courseName} by {meters}m @ {heading} degrees.");
            KnownCourses courses = new KnownCourses();
            var coords = courses.GetNewCoordinates(courseName, meters, heading);
            Console.WriteLine($"Lat: {coords.Latitude}, Lon: {coords.Longitude}");
        }
    }
}
