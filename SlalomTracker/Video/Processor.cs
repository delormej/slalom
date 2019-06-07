using System;
using SlalomTracker;
using SlalomTracker.Cloud;

namespace SlalomTracker.Video 
{
    public class SkiVideoProcessor
    {
        Storage _storage;

        public SkiVideoProcessor()
        {
            _storage = new Storage();
        }

        /// <summary>
        /// Downloads video, extracts and uploads metadata, trims video to just the course pass,
        /// removes audio and uploads finalized video.
        /// </summary>
        /// <returns>Url of processed video.</returns>
        public string Process(string videoUrl)
        {
            string localPath = Cloud.Storage.DownloadVideo(videoUrl);
            string json = MetadataExtractor.Extract.ExtractMetadata(localPath);
            CoursePass pass = CoursePassFactory.FromJson(json);
            string processedLocalPath = TrimAndSilenceVideo(localPath, pass);
            string finalVideoUrl = _storage.UploadVideo(processedLocalPath);
            _storage.AddMetadata(finalVideoUrl, json, pass);
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
                    duration = (start - total);
            }
                
            if (duration > 0.0d)
            {
                duration += 5.0; /* pad 5 seconds more */
                Console.WriteLine(
                    $"Trimming {localPath} from {start} seconds for {duration} seconds.");

                VideoTasks tasks = new VideoTasks();           
                
                var trimTask = tasks.TrimAsync(localPath, start, duration);
                trimTask.Wait();
                string trimmedPath = trimTask.Result;

                Console.WriteLine($"Removing audio from {localPath}.");
                var silenceTask = tasks.RemoveAudioAsync(trimmedPath);
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
    }
}