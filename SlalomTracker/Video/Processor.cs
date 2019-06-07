using System;
using SlalomTracker;
using SlalomTracker.Cloud;

namespace SlalomTracker.Video 
{
    public class Processor
    {
        Storage _storage;

        public Processor()
        {
            _storage = new Storage();
        }

        /// <summary>
        /// Downloads video, extracts and uploads metadata, trims video to just the course pass,
        /// removes audio and uploads finalized video.
        /// </summary>
        /// <returns>Url of processed video.</returns>
        public string Execute(string videoUrl)
        {
            string localPath = Cloud.Storage.DownloadVideo(videoUrl);
            string json = MetadataExtractor.Extract.ExtractMetadata(localPath);
            CoursePass pass = CoursePassFactory.FromJson(json);
            string processedLocalPath = TrimAndSilenceVideo(localPath, pass);
            string finalVideoUrl = _storage.UploadVideo(localPath);
            _storage.AddMetadata(finalVideoUrl, json, pass);

            return finalVideoUrl;
        }

        private string TrimAndSilenceVideo(string localPath, CoursePass pass)
        {
            double start = pass.GetSecondsAtEntry();
            double duration = pass.GetDurationSeconds();

            VideoTasks tasks = new VideoTasks();
            
            var trimTask = tasks.TrimAsync(localPath, start, duration);
            trimTask.Wait();
            string trimmedPath = trimTask.Result;

            var silenceTask = tasks.RemoveAudioAsync(trimmedPath);
            trimTask.Wait();
            string silencedPath = trimTask.Result;

            return silencedPath;
        }
    }
}