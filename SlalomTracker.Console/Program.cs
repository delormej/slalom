﻿using System;
using System.Linq;
using SlalomTracker;
using SlalomTracker.Cloud;
using SlalomTracker.Video;
using MetadataExtractor;
using System.Drawing.Imaging;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlalomTracker.Logging;

namespace SkiConsole
{
    class Program
    {               
        private static ILogger<Program> _log = 
            SkiLogger.Factory.CreateLogger<Program>();

        const int Fail = -1;
        const int Success = 0;

        static int Main(string[] args)
        {
            PrintVersion();

            if (args.Length < 1)
            {
                ShowUsage();
                return Fail;
            }
            
            try 
            {
                Run(args);
            }
            catch (Exception e)
            {
                _log.LogError("Failed", e);
                return Fail;
            }

            return Success;
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
                "ski -t\n\t\t" +
                "Listen to service bus queue for new videos uploaded.\n\t\t" +
                "ski -l [optional]queueName\n\t\t" +
                "Republish videos currently in upload folder to service bus queue for new videos uploaded.\n\t\t" +
                "ski -r\n\t\t" +
                "Migrate metadata to Firestore.\n\t\t" +
                "ski --migrate\n\t\t" +
                "Backup Firestore.\n\t\t" +
                "ski --backup\n\t\t"  +
                "Restore Firestore.\n\t\t" +
                "ski --restore backup.json\n\t\t"                   

            );
        }

        private static void StartDebug(string[] args)
        {
            args[0] = args[0].Replace("debug", "");
            Console.WriteLine("Press any key to start debugging...");
            Console.ReadKey();            
        }

        private static void Run(string[] args)
        {
            if (args[0].StartsWith("debug"))
            {
                StartDebug(args);
            }

            if (args[0] == "-d" && args.Length >= 2)
            {
                // eg. ski -d https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4
                DownloadVideo(args[1]);
                // int count = int.Parse(args[1]);
                // StorageMaintenance maint = new StorageMaintenance();
                // Task<int> moved = maint.MoveFromGcpToAzureAsync(count);
                // moved.Wait();
                // Console.WriteLine("Moved: " + moved.Result);
            }
            else if (args[0] == "-e" && args.Length >= 3)
            {
                // eg. ski -e 2018-06-20/GOPR0194.MP4 GOPR0194.json
                ExtractMetadataAsJson(args[1], args[2]);
            }
            else if (args[0] == "-i" && args.Length >= 2)
            {
                CreateImage(args);
            }
            else if (args[0] == "-p" && args.Length >= 2)
            {
                // eg. ski -p https://jjdelormeski.blob.core.windows.net/videos/GOPR0194.MP4
                ProcessVideo(args[1]);
            }
            else if (args[0] == "-m")
            {
                PrintAllMetadata();
            }
            else if (args[0] == "-y")
            {
                // Int means upload the last n videos.  String arg is individual video.
                string arg = args[1];
                int count = 0;
                if (int.TryParse(arg, out count))
                    UploadYouTubeAsync(count).Wait();
                else 
                    UploadYouTubeAsync(arg).Wait();
            }
            else if (args[0] == "-c" && args.Length >= 2)
            {
                PrintCreationTime(args[1]);
            }
            else if (args[0] == "-t") 
            {
                TrainAsync().Wait();
            }
            else if (args[0] == "-g")
            {
                GoogleStorageOperations(args);
            }
            else if (args[0] == "-s" && args.Length > 2) 
            {
                OutputHandleSpeed(args[1], double.Parse(args[2]));
            }
            else if (args[0] == "-l") 
            {
                string queue = args.Length > 1 ? args[1] : null;
                Listen(queue, args.Length > 2);
            }
            else if (args[0] == "-u")
            {
                UpdateCreationTimeAsync().Wait();
            }
            else if (args[0] == "-n")
            {
                Notify();
            }            
            else if (args[0] == "-b")
            {
                UpdateThumbnailsAsync().Wait();
            }
            else if (args[0] == "--combine" && args.Length > 2)
            {
                CombineVideosAsync(args[1], args[2]).Wait();
            }      
            else if (args[0] == "--courses")
            {
                PrintCourses(args);
            }
            else if (args[0] == "-r")
            {
                RepublishStorageEventsAsync().Wait();
            }
            else if (args[0] == "--migrate")
            {
                FirestoreMigrateAsync().Wait();
            }
            else if (args[0] == "--pubsub")
            {
                PubSub().Wait();
            }
            else if (args[0] == "--backup")
            {
                BackupFirestoreAsync().Wait();
            }
            else if (args[0] == "--restore" && args.Length > 1)
            {
                RestoreFirestoreAsync(args[1]).Wait();
            }
            else
                ShowUsage();
        }

        private static void PrintVersion()
        {
            SkiVideoEntity video = new SkiVideoEntity("http://test/test", DateTime.Now);
            Console.WriteLine("Version: " + video.SlalomTrackerVersion);
        }

        private static void CreateImage(string[] args)
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

        private static string DownloadAndCreateImage(string url)
        {
            IStorage storage = new AzureStorage();
            string localPath = storage.DownloadVideo(url);
            string json = Extract.ExtractMetadata(localPath);
            CoursePass pass = new CoursePassFactory().FromJson(json);
            if (pass == null)
                throw new ApplicationException($"Unable to create a pass for {url}");  

            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();
            string imagePath = localPath.Replace(".MP4", ".png");
            bitmap.Save(imagePath, ImageFormat.Png);
            return imagePath;
        }

        private static async Task UploadYouTubeAsync(string localPath)
        {
            YouTubeHelper youtube = new YouTubeHelper(YouTubeCredentials.Create());
            string url = await youtube.UploadAsync(localPath);
            _log.LogInformation($"Uploaded to {url}");
        }

        private static async Task UploadYouTubeAsync(int count)
        {
            YouTubeHelper youtube = new YouTubeHelper(YouTubeCredentials.Create());
            var videos = await LoadVideosAsync();
            videos = videos.OrderByDescending(v => v.RecordedTime).Take(count);

            foreach (SkiVideoEntity e in videos)
            {
                // Console.WriteLine("\t{0}\\{1}\t{2}", e.PartitionKey, e.RowKey, e.Url);
                string localPath = DownloadVideo(e.Url);
                string url = await youtube.UploadAsync(localPath);
                _log.LogInformation($"Uploaded to {url}");

                // Update HotUrl to youtube.
                GoogleStorage storage = new GoogleStorage();
                e.HotUrl = url;
                storage.UpdateMetadata(e);
            }
        }

        private static string DownloadVideo(string url)
        {
            IStorage storage = new AzureStorage();
            string localPath = storage.DownloadVideo(url);
            _log.LogInformation($"Downloaded to: {localPath}");
            return localPath;
        }

        private static void ExtractMetadataAsJson(string videoLocalPath, string jsonPath)
        {
            string json = Extract.ExtractMetadata(videoLocalPath);
            System.IO.File.WriteAllText(jsonPath, json);
        }

        private static void ProcessVideo(string videoUrl)
        {
            SkiVideoProcessor processor = new SkiVideoProcessor(videoUrl);
            processor.ProcessAsync().Wait();
        }

        private static async Task TrainAsync()
        {
            _log.LogInformation("Loading videos to train.");
            IEnumerable<SkiVideoEntity> videos = await LoadVideosAsync();
            
            var ropeTask = Task.Run( () => {
                _log.LogInformation("Training rope length detection.");
                RopeMachineLearning ropeMl = new RopeMachineLearning();
                ropeMl.Train(videos);
            });

            var skierTask = Task.Run( () => {
                _log.LogInformation("Training skier detection.");
                SkierMachineLearning skierMl = new SkierMachineLearning();
                skierMl.Train(videos);
            });

            await Task.WhenAll(ropeTask, skierTask);
            _log.LogInformation("Done training.");
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
                pass = factory.FromJsonUrl(jsonPath);
            else
                pass = factory.FromLocalJsonFile(jsonPath);

            if (pass == null)
                throw new ApplicationException($"Unable to create a pass for {jsonPath}");  

            string imagePath = GetImagePath(jsonPath);
            CoursePassImage image = new CoursePassImage(pass);
            Bitmap bitmap = image.Draw();
            bitmap.Save(imagePath, ImageFormat.Png);

            _log.LogInformation($"Gate precision == {pass.GetGatePrecision()} for {jsonPath}");
            _log.LogInformation($"Wrote image to: {imagePath}");

            return imagePath;
        }

        private static void OutputHandleSpeed(string jsonPath, double rope)
        {
            if (!jsonPath.StartsWith("http")) 
                throw new ApplicationException("Must pass http path to jsonUrl.");

            CoursePassFactory factory = new CoursePassFactory();
            factory.RopeLengthOff = rope;
            CoursePass pass = factory.FromJsonUrl(jsonPath);     
            if (pass == null)
                throw new ApplicationException($"Unable to create a pass for {jsonPath}");    
                     
            foreach(var m in pass.Measurements) 
            {
                _log.LogInformation($"{m.Timestamp.ToString("ss.fff")}, {m.HandleSpeedMps}");
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
            
            var videos = metadataTask.Result;
            videos = videos.OrderByDescending(v => v.RecordedTime);

            foreach (SkiVideoEntity e in videos)
            {
                _log.LogInformation($"\t{e.PartitionKey}\\{e.RowKey}\t{e.Url}");
            }
        }

        private static Task<IEnumerable<SkiVideoEntity>> LoadVideosAsync()
        {
            IStorage storage = new GoogleStorage();
            return storage.GetAllMetdataAsync();
        }        

        private static void PrintCreationTime(string inputFile)
        {
            VideoTasks video = new VideoTasks(inputFile);
            DateTime creation = video.GetCreationTime();
            _log.LogInformation(
                $"File: {inputFile}, video creationtime " +
                creation.ToString("MM/dd/yyyy h:mm tt"));

            SkiVideoEntity entity = new SkiVideoEntity("http://localhost/TEST.MP4", creation);
            string obj = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
            _log.LogInformation("Object:\n" + obj);
        }

        /// <summary>
        /// 1-off job to fix timezone issues, video times were stamped as UTC, but they were 
        /// actually Eastern time zone (Americas/New York).
        /// </summary>
        private static async Task UpdateCreationTimeAsync()
        {
            TimeZoneInfo videoTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                VideoTasks.DefaultVideoRecordingTimeZone);
            AzureStorage storage = new AzureStorage();
            IEnumerable<SkiVideoEntity> entities = await storage.GetAllMetdataAsync();
            var list = entities.Where(e => e.RecordedTime > DateTime.Today.AddMonths(-3));
            foreach (SkiVideoEntity entity in list)
            {
                if (entity.RecordedTime == DateTime.MinValue)
                    continue;
                DateTime toConvertTime = DateTime.SpecifyKind(entity.RecordedTime, DateTimeKind.Unspecified);
                DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc(toConvertTime, videoTimeZone);
                entity.RecordedTime = utcTime;
                storage.UpdateMetadata(entity);
                _log.LogInformation($"Updated {entity.RowKey} to: {entity.RecordedTime}");
            }
        }

        private static void PrintCourses(string[] args)
        {
            if (args.Length > 3) {
                string course = args[1];
                double meters = double.Parse(args[2]);
                double heading = double.Parse(args[3]);
                GetNewCoords(course, meters, heading);
            }

            KnownCourses knownCourses = new KnownCourses();
            // One time run only:
            // knownCourses.AddKnownCourses();
            _log.LogInformation("Courses available:");
            foreach (Course c in knownCourses.List)
            {
                _log.LogInformation(string.Format("\tName:{0}, Entry(Lat/Lon):{1}\\{2}, Heading:{3}", 
                    c.Name, 
                    c.Course55EntryCL.Latitude, 
                    c.Course55EntryCL.Longitude,
                    c.CourseHeading));
            }
        }

        private static void GetNewCoords(string courseName, double meters, double heading)
        {
            Console.WriteLine($"Moving {courseName} by {meters}m @ {heading} degrees.");
            KnownCourses courses = new KnownCourses();
            var coords = courses.GetNew55Coordinates(courseName, meters, heading);
            Console.WriteLine($"course55Entry={coords[0].Latitude},{coords[0].Longitude}");
            Console.WriteLine($"course55Exit={coords[1].Latitude},{coords[1].Longitude}");
        }

        private static void GoogleStorageOperations(string[] args)
        {
            if (args.Length < 2)
            {
                GetGoogleStorageSizeAsync().Wait();
                PrintGoogleStorageUsage();
            }
            else
            {
                int count = int.Parse(args[1]);
                if (count < 0)
                    DeleteOldestAsync(Math.Abs(count)).Wait();
                else
                    UploadLatestToGoogleAsync(count).Wait();
            }
        }

        private static void PrintGoogleStorageUsage()
        {
            Console.WriteLine("Google Storage Usage:");
            Console.WriteLine("\t-g\tReturns bucket size in MiB");
            Console.WriteLine("\t-g 2\tUploads newest 2 to Google and updates metadata.");
            Console.WriteLine("\t-g -2\tDeletes 2 oldest from Google and updates metadata.");            
        }

        private static async Task UploadLatestToGoogleAsync(int count)
        {
            var videos = await LoadVideosAsync();
            var sortedVideos = SortVideos(videos);
            _log.LogInformation($"Found {videos.Count()} videos.");

            GoogleStorage gStore = new GoogleStorage();
            AzureStorage storage = new AzureStorage();
            List<Task<string>> uploadTasks = new List<Task<string>>();

            foreach (var e in sortedVideos)
            {
                Console.WriteLine("\t{0}\t{1}", e.RecordedTime, e.Url);
                uploadTasks.Add(UploadToGoogle(e));
            }

            Task.WaitAll(uploadTasks.ToArray());
            _log.LogInformation("Upload complete.");
            
            async Task<string> UploadToGoogle(SkiVideoEntity e)
            {   
                string localPath = await Task<string>.Run(() => DownloadVideo(e.Url));
                e.HotUrl = await gStore.UploadVideoAsync(localPath, e.RecordedTime);
                storage.UpdateMetadata(e);
                _log.LogInformation($"Google HotUrl updated {e.HotUrl}");
                return e.HotUrl;
            }

            IEnumerable<SkiVideoEntity> SortVideos(IEnumerable<SkiVideoEntity> videos)
            {
                return videos.OrderByDescending(v => v.RecordedTime)
                    .Where(v => v.HotUrl == null)
                    .Take(count);
            }        
        }

        private static async Task GetGoogleStorageSizeAsync()
        {
            GoogleStorage gstore = new GoogleStorage();
            float size = await gstore.GetBucketSizeAsync();
            Console.WriteLine($"Total bucket size is {size:0,0.0} MiB");
            ListLargest();
        }

        private static async Task<int> DeleteOldestAsync(int count)
        {
            GoogleStorage gstore = new GoogleStorage();
            AzureStorage storage = new AzureStorage();
            var videos = await LoadVideosAsync();
            var sortedVideos = SortVideos(videos);

            List<Task> videoTasks = new List<Task>();

            foreach (var video in sortedVideos)
            {
                videoTasks.Add(
                    DeleteGoogleVideoAsync(gstore, storage, video)
                );
            }

            Task.WaitAll(videoTasks.ToArray());
            return videoTasks.Count();

            IEnumerable<SkiVideoEntity> SortVideos(IEnumerable<SkiVideoEntity> videos)
            {
                return videos.OrderBy(v => v.RecordedTime)
                    .Where(v => !string.IsNullOrWhiteSpace(v.HotUrl) && v.Starred != true)
                    .Take(count);
            }                    
        }

        private static async Task<int> DeleteLargestAsync(int count)
        {
            int deleted = 0;
            GoogleStorage gstore = new GoogleStorage();
            AzureStorage storage = new AzureStorage();
            var videos = await LoadVideosAsync();
            var largest = gstore.GetLargest().Take(Math.Abs(count));

            foreach (var large in largest)
            {
                SkiVideoEntity video = videos.Single(v => v.HotUrl == gstore.BaseUrl + large.Name);
                if (!video.Starred)
                {
                    await DeleteGoogleVideoAsync(gstore, storage, video);
                    deleted++;
                }
            }

            return deleted;
        }

        private static void ListLargest()
        {
            GoogleStorage gstore = new GoogleStorage();
            var largest = gstore.GetLargest();
            foreach (var o in largest)
                System.Console.WriteLine("{0}, {1:,0.0}", o.Name, (o.Size / 1024F*1024F));
        }

        private static async Task DeleteGoogleVideoAsync(GoogleStorage gstore, AzureStorage storage, SkiVideoEntity video)
        {
            _log.LogInformation($"Deleting {video.HotUrl} recorded @ {video.RecordedTime}.");
            await gstore.DeleteAsync(video.HotUrl);
            video.HotUrl = "";
            storage.UpdateMetadata(video);
            _log.LogInformation($"Metadata updated for video recorded @ {video.RecordedTime}.");
        }

        private static async Task UpdateThumbnailsAsync()
        {
            AzureStorage storage = new AzureStorage();
            IEnumerable<SkiVideoEntity> videos = await storage.GetAllMetdataAsync();
            var selectedVideos = videos.OrderByDescending(v => v.RecordedTime).Take(5);

            foreach(var video in selectedVideos)
            {
                try
                {
                    await UpdateThumbnailAsync(storage, video);
                }
                catch (Exception e)
                {
                    _log.LogInformation($"Unable to update thumbnail for {video.PartitionKey}, {video.RowKey}", e);
                }
            }
        }

        private static async Task UpdateThumbnailAsync(IStorage storage, SkiVideoEntity video)
        {
            _log.LogInformation($"Updating thumbnail for {video.PartitionKey}, {video.RowKey}");
            double thumbnailAtSeconds = 0; // video.EntryTime;

            string localVideoPath = storage.DownloadVideo(video.HotUrl ?? video.Url);
            VideoTasks _videoTasks = new VideoTasks(localVideoPath);

            string localThumbnailPath = await _videoTasks.GetThumbnailAsync(thumbnailAtSeconds);
            string modifiedThumbnailPath = localThumbnailPath.Replace("_ts.PNG", ".PNG");
            System.IO.File.Move(localThumbnailPath, modifiedThumbnailPath);
            string thumbnailUrl = storage.UploadThumbnail(modifiedThumbnailPath, video.RecordedTime);

            _log.LogInformation($"New thumbnail at {thumbnailUrl}");            
        }

        private static async Task CombineVideosAsync(string videoUrl1, string videoUrl2)
        {
            _log.LogInformation($"Combining video {videoUrl1} combined with {videoUrl2}.");

            CompareVideoProcessor processor = new CompareVideoProcessor(videoUrl1, videoUrl2);
            await processor.ProcessAsync();
        }

        /// <summary>
        /// Listens to service bus queue and processes videos as they arrive.
        /// </summary>
        private static void Listen(string subscriptionId, bool openDeadLetter = false)
        {
            try 
            {
                IUploadListener listener = new PubSubVideoUploadListener(subscriptionId, openDeadLetter, 
                    PubSubVideoUploadListener.UnlimitedMessages);
                EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
                EventHandler Reset = (o, e) => {
                    _log.LogInformation("Signaling stop.");
                    listener.Stop();
                    ewh.Set();
                };
                AppDomain.CurrentDomain.ProcessExit += Reset;           
                listener.Completed += Reset;  // Force to only listen for 1 message, then exit.
                listener.Start();
                
                if (Console.WindowHeight > 0)
                {
                    Task.Run( () => {
                        Console.WriteLine("Press any key to cancel.");
                        Console.ReadKey();
                        ewh.Set();
                    });
                }

                Console.WriteLine("Waiting until signaled to close.");

                // Wait until signalled.
                ewh.WaitOne();
            
                _log.LogInformation($"Done listening for events.");
                AppDomain.CurrentDomain.ProcessExit -= Reset;
            }
            catch (Exception e)
            {
                _log.LogError("Error while listening to queue.", e);
                // Logger.Log("\tInner Exception.", e.InnerException);
            }
        }

        private static void Notify()
        {
            VideoProcessedNotifier notifier = new VideoProcessedNotifier();
            notifier.NotifyAsync("Jason", "video.MP4").Wait();
        }

        private static async Task RepublishStorageEventsAsync()
        {
            GoogleStorage storage = new GoogleStorage();
            PubSubVideoUploadPublisher publisher = new PubSubVideoUploadPublisher("video-uploads-topic5");
            var uploaded = storage.ListUploaded("gke-ski-video-uploads");
            var tasks = uploaded.Select(u => publisher.PublishAsync(u));
            
            await Task.WhenAll(tasks);
        }

        private static async Task FirestoreMigrateAsync()
        {
            IEnumerable<SkiVideoEntity> metadata = await LoadVideosAsync();
            IStorage storage = new GoogleStorage();

            var tasks = metadata
                .Where(v => v.ThumbnailUrl != null && v.JsonUrl != null)
                .Select(v => storage.AddTableEntityAsync(v));
            await Task.WhenAll(tasks);

            System.Console.WriteLine("Done");
        }

        private static async Task PubSub()
        {
            PullMessagesAsyncSample pull = new PullMessagesAsyncSample();
            int messages = await pull.PullMessagesAsync("gke-ski", "video-uploaded-sub", false);
            System.Console.WriteLine($"Messages: {0}");
        }

        private static async Task BackupFirestoreAsync() 
        {
            var data = await LoadVideosAsync();
            
            using (var stream = System.IO.File.Create("backup.json"))
            {
                await System.Text.Json.JsonSerializer.SerializeAsync<IEnumerable<SkiVideoEntity>>(
                    stream, data);
                await stream.FlushAsync();
            }

            System.Console.WriteLine("Wrote backup.json");
        }

        private static async Task RestoreFirestoreAsync(string backupFile) 
        {
            GoogleStorage storage = new GoogleStorage("fluted-quasar-240821");
            var json = await System.IO.File.ReadAllTextAsync(backupFile);
            var entities = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<SkiVideoEntity>>(json);

            await Task.WhenAll(entities.Select(async entity => 
            {
                await storage.AddTableEntityAsync(entity);
                System.Console.WriteLine($"Added metadata for {entity.Url}");
            }));
            
            System.Console.WriteLine($"Wrote {backupFile} to Firestore");
        }        
    }
}
