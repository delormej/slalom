using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SlalomTracker;
using SlalomTracker.Cloud;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;

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
                string videoLocalPath = Cloud.Storage.DownloadVideo(videoUrl);
                var creationTimeTask = _videoTasks.GetCreationTimeAsync(videoLocalPath);
                string json = MetadataExtractor.Extract.ExtractMetadata(videoLocalPath);
                
                CoursePassFactory factory = new CoursePassFactory();
                CoursePass pass = factory.FromJson(json);

                // TODO: Try to fit CLOffset

                creationTimeTask.Wait();
                DateTime creationTime = creationTimeTask.Result;

                var thumbnailTask = CreateThumbnailAsync(videoLocalPath, creationTime, 
                    pass.GetSecondsAtEntry());               
                
                string processedLocalPath = TrimAndSilenceVideo(videoLocalPath, pass); // TODO: this could happen async as well.
                
                finalVideoUrl = _storage.UploadVideo(processedLocalPath, creationTime); // This could be a continuation of creationTime & Silence
                
                SkiVideoEntity entity = new SkiVideoEntity(finalVideoUrl, creationTime);
                CopyCoursePassToEntity(pass, entity);

                thumbnailTask.ContinueWith(t => 
                { 
                    entity.ThumbnailUrl = t.Result;
                    entity.RopeLengthM = PredictRopeLength(t.Result); 
                }).Wait();

                _storage.AddMetadata(entity, json);
                
                // Wait until everything succeeds before deleting source video.
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

        private double PredictRopeLength(string thumbnailUrl)
        {
            const string cropThumbnailUrl = "https://ski-app.azurewebsites.net/api/crop?thumbnailUrl=";

            const string CustomVisionEndPoint = "https://ropelengthvision.cognitiveservices.azure.com/";
            const string CustomVisionPredictionKey = "8d326cd29a0b4636beced3a4658c09cb";
            const string CustomVisionModelName = "RopeLength";
            Guid projectId = new Guid("4668e0c2-7e00-40cb-a58a-914eb988f44d");

            Console.WriteLine("Making a prediction of rope length for: " + thumbnailUrl);
            CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
            {
                ApiKey = CustomVisionPredictionKey,
                Endpoint = CustomVisionEndPoint
            };

            ImageUrl thumbnail = new ImageUrl(cropThumbnailUrl + thumbnailUrl);
            var result = endpoint.ClassifyImageUrl(projectId, CustomVisionModelName, thumbnail);

            // Loop over each prediction and write out the results
            foreach (var c in result.Predictions)
            {
                Console.WriteLine($"\t{c.TagName}: {c.Probability:P1}");
            }

            return GetHighestRankedPrediction(result.Predictions);
        }

        private double GetHighestRankedPrediction(IList<PredictionModel> predictions)
        {
            double ropeLength = 0.0d;
            
            string ropeTagName = predictions.OrderByDescending(p => p.Probability)
                .Select(p => p.TagName)
                .First();

            if (ropeTagName != null)
                ropeLength = double.Parse(ropeTagName);
            
            return ropeLength;
        }

        private void CopyCoursePassToEntity(CoursePass pass, SkiVideoEntity entity)
        {
            entity.BoatSpeedMph = pass.AverageBoatSpeed;
            entity.CourseName = pass.Course.Name;
            entity.EntryTime = pass.GetSecondsAtEntry();     
            entity.RopeLengthM = pass.Rope != null ? pass.Rope.FtOff : 0;
            entity.CenterLineDegreeOffset = pass.CenterLineDegreeOffset;      
        }        
    }
}