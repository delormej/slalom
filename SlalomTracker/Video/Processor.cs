using System;
using System.Threading.Tasks;
using SlalomTracker;
using SlalomTracker.Cloud;

namespace SlalomTracker.Video 
{
    public class SkiVideoProcessor
    {
        Storage _storage;
        VideoTasks _videoTasks;

        public SkiVideoProcessor()
        {
            _storage = new Storage();
            _videoTasks = new VideoTasks();      
        }

        /// <summary>
        /// Downloads video, extracts metadata, trims video to just the course pass, removes audio, 
        /// generates thumbnail, and uploads metadata, thumbnail, final video, deletes ingest video.
        /// </summary>
        /// <returns>Url of processed video.</returns>
        public string Process(string videoUrl)
        {
            string localPath = Cloud.Storage.DownloadVideo(videoUrl);
            string json = MetadataExtractor.Extract.ExtractMetadata(localPath);
            CoursePass pass = CoursePassFactory.FromJson(json);
            var thumbnailTask = CreateThumbnailAsync(localPath, pass.GetSecondsAtEntry());
            string processedLocalPath = TrimAndSilenceVideo(localPath, pass);
            string finalVideoUrl = _storage.UploadVideo(processedLocalPath);
            
            // Wait to get the thumbnail path.
            thumbnailTask.Wait();
            string thumbnailUrl = thumbnailTask.Result;

            _storage.AddMetadata(finalVideoUrl, thumbnailUrl, json, pass);

            _storage.DeleteIngestedBlob(videoUrl);

            return finalVideoUrl;
        }

        private string TrimAndSilenceVideo(string localPath, CoursePass pass)
        {
            double start = pass.GetSecondsAtEntry();
            double duration = pass.GetDurationSeconds();
            double total = pass.GetTotalSeconds();
            
            if (start > 0 && duration == 0.0d)
            {
                // Likely a crash or didn't exit course, grab 15 seconds or less of the video.
                if (total > (start + 15.0d))
                    duration = 15.0d;                    
                else
                    duration = (total - start);
            }
                
            if (duration > 0.0d)
            {
                duration += 5.0; /* pad 5 seconds more */
                Console.WriteLine(
                    $"Trimming {localPath} from {start} seconds for {duration} seconds.");     
                
                var trimTask = _videoTasks.TrimAsync(localPath, start, duration);
                trimTask.Wait();
                string trimmedPath = trimTask.Result;

                Console.WriteLine($"Removing audio from {localPath}.");
                var silenceTask = _videoTasks.RemoveAudioAsync(trimmedPath);
                silenceTask.Wait();
                string silencedPath = silenceTask.Result;

                return silencedPath;
            }
            else
            {
                throw new ApplicationException(
                    $"Start ({start}) and duration ({duration}) invalid for video: {localPath}.  Total duration {total} seconds.");
            }
        }

        /// <summary>
        /// Creates and uploads a thumbnail async, when complete returns the full uri of 
        /// the uploaded thumbnail.
        /// </summary>
        private Task<string> CreateThumbnailAsync(string localVideoPath, double atSeconds = 0.5)
        {
            // Kick thumbnail generation off async.
            var thumbnailTask = _videoTasks.GetThumbnailAsync(localVideoPath, atSeconds)
                .ContinueWith<string>(t => 
                {
                    string thumbnailUrl = _storage.UploadThumbnail(t.Result);
                    return thumbnailUrl;
                });
            return thumbnailTask;
        }
    }
}