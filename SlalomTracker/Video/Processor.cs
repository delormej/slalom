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
            string finalVideoUrl;
            try
            {
                string localPath = Cloud.Storage.DownloadVideo(videoUrl);
                var creationTimeTask = _videoTasks.GetCreationTimeAsync(localPath);
                string json = MetadataExtractor.Extract.ExtractMetadata(localPath);
                CoursePass pass = CoursePassFactory.FromJson(json);

                creationTimeTask.Wait();
                DateTime creationTime = creationTimeTask.Result;

                var thumbnailTask = CreateThumbnailAsync(localPath, creationTime, 
                    pass.GetSecondsAtEntry());               
                string processedLocalPath = TrimAndSilenceVideo(localPath, pass);
                finalVideoUrl = _storage.UploadVideo(processedLocalPath, creationTime);
                
                SkiVideoEntity entity = new SkiVideoEntity(finalVideoUrl, creationTime);
                entity.SetFromCoursePass(pass);
                thumbnailTask.Wait();
                entity.ThumbnailUrl = thumbnailTask.Result;               

                _storage.AddMetadata(entity, json);
                _storage.DeleteIngestedBlob(videoUrl);
            }
            catch (System.AggregateException aggEx)
            {
                throw new ApplicationException($"Unable to process {videoUrl}.  Failed at: \n" +
                    aggEx.GetBaseException().Message);
            }

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
                
                Console.WriteLine($"Trimmed: {trimmedPath}");
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
        private Task<string> CreateThumbnailAsync(string localVideoPath, DateTime creationTime, double atSeconds = 0.5)
        {
            // Kick thumbnail generation off async.
            var thumbnailTask = _videoTasks.GetThumbnailAsync(localVideoPath, atSeconds)
                .ContinueWith<string>(t => 
                {
                    string thumbnailPath = t.Result;
                    Console.WriteLine($"Generated thumbnail: {thumbnailPath}");
                    string thumbnailUrl = _storage.UploadThumbnail(thumbnailPath, creationTime);
                    return thumbnailUrl;
                });
            return thumbnailTask;
        }
    }
}