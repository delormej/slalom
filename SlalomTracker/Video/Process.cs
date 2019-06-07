/*
/// Master end to end processing method, feel like this should live in SlalomTracker and not cli
Process(webUrl)
 localPath = Download(webUrl)
 pass = ExtractAndUploadMetadata(localPath)
 // what to do if no course pass found? Can't trim video... no start point
 processedVideoLocalPath = TrimAndSilenceVideo(localPath)
 webUrl = UploadVideo(processedVideoLocalPath)
 AddMetadata(pass, webUrl)
 return webUrl;

/// Trims and silences video
TrimAndSilenceVideo(localPath, pass)
 duration = pass.CourseExitTime - pass.CourseEntryTime
 trimmedPath = Trim(localPath, pass.CourseEntryTime, duration)
 silentPath = RemoveAudio(trimmedPath)
 return silentPath
 
ExtractAndUploadMetadata(localPath)
 json = ExractMetadata(localPath)
 pass = CreateCoursePass(json)
 // if no pass, don't upload measurements
 UploadMeasurements(localPath, json)
 return pass

1. Remove CoursePass creation from Storage.AddMetadata(), it shouldn't be it's responsibility, it should get passed as an object.
2. Remove BlobName from AddMetadata -- it should be able to figure that out.
2. ProcessVideoTrigger will call CreateAci() instead of ProcessVideoTrigger

 */
using System;
using SlalomTracker;
using SlalomTracker.Cloud;

namespace SlalomTracker.Video 
{
    public class Process
    {
        Storage _storage;

        public Process()
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