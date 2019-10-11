using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using PredictionModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;

namespace SlalomTracker.Cloud
{
    public class MachineLearning
    {
        const string CropThumbnailUrl = "https://ski-app.azurewebsites.net/api/crop?thumbnailUrl=";

        const string CustomVisionEndPoint = "https://ropelengthvision.cognitiveservices.azure.com/";
        const string CustomVisionPredictionKey = "8d326cd29a0b4636beced3a4658c09cb";
        const string CustomVisionTrainingKey = "7191c8190b4949b98b35c140efd7b7e6";

        const string CustomVisionModelName = "RopeLength";

        private CustomVisionPredictionClient predictionApi;
        private CustomVisionTrainingClient trainingApi;

        public static Guid ProjectId = new Guid("4668e0c2-7e00-40cb-a58a-914eb988f44d");

        public MachineLearning()
        {
            predictionApi = new CustomVisionPredictionClient()
            {
                ApiKey = CustomVisionPredictionKey,
                Endpoint = CustomVisionEndPoint
            };

            trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = CustomVisionTrainingKey,
                Endpoint = CustomVisionEndPoint
            };            
        }

        public double PredictRopeLength(string thumbnailUrl)
        {
            Console.WriteLine("Making a prediction of rope length for: " + thumbnailUrl);

            PredictionModels.ImageUrl thumbnail = new PredictionModels.ImageUrl(CropThumbnailUrl + thumbnailUrl);
            var result = predictionApi.ClassifyImageUrl(ProjectId, CustomVisionModelName, thumbnail);

            // Loop over each prediction and write out the results
            foreach (var c in result.Predictions)
            {
                Console.WriteLine($"\t{c.TagName}: {c.Probability:P1}");
            }

            return GetHighestRankedPrediction(result.Predictions);
        }

        public void Train()
        {
            try
            {
                // Load videos & tags.
                var loadVideosTask = LoadVideosAsync();
                var getTagsTask = trainingApi.GetTagsAsync(ProjectId);
                Task.WaitAll(loadVideosTask, getTagsTask);

                // Tag all the videos & train the model.
                Train(loadVideosTask.Result, getTagsTask.Result);
            }
            catch (TrainingModels.CustomVisionErrorException e)
            {
                System.Console.WriteLine("Unable to train model.\n" + e.Message);
            }            
        }

        private double GetHighestRankedPrediction(IList<PredictionModels.PredictionModel> predictions)
        {
            double ropeLength = 0.0d;
            
            string ropeTagName = predictions.Where(p => IsTagRopeLength(p.TagName))
                .OrderByDescending(p => p.Probability)
                .Select(p => p.TagName)
                .First();

            if (ropeTagName != null)
                ropeLength = double.Parse(ropeTagName);
            
            return ropeLength;
        }

        private bool IsTagRopeLength(string tagName)
        {
            double rope;
            return double.TryParse(tagName, out rope);
        }

        private Task<List<SkiVideoEntity>> LoadVideosAsync()
        {
            Storage storage = new Storage();
            return storage.GetAllMetdataAsync();
        }

        private void Train(List<SkiVideoEntity> videos, IList<TrainingModels.Tag> tags)
        {
            const int BatchSize = 63;
            
            MachineLearningTags mlTags = new MachineLearningTags(trainingApi, videos, tags);
            List<TrainingModels.ImageUrlCreateEntry> entries = new List<TrainingModels.ImageUrlCreateEntry>();

            foreach (var video in videos)
            {
                IList<Guid> tagIds = mlTags.GetTagIds(video);
                if (tagIds.Count == 0) // No tags.
                    continue;

                if (string.IsNullOrEmpty(video.ThumbnailUrl))
                    continue;

                entries.Add(new TrainingModels.ImageUrlCreateEntry() 
                { 
                    Url = CropThumbnailUrl + video.ThumbnailUrl, 
                    TagIds = tagIds
                });

                if (entries.Count <= BatchSize)
                {
                    SendBatch(entries);
                    entries.Clear();
                }
            }

            // Kick off the training.
            trainingApi.TrainProject(ProjectId);
        }

        private void SendBatch(List<TrainingModels.ImageUrlCreateEntry> entries)    
        {
            try
            {
                var batch = new TrainingModels.ImageUrlCreateBatch(entries);
                trainingApi.CreateImagesFromUrls(ProjectId, batch);        
            }
            catch (Exception e)
            {
                Console.WriteLine("Error writing ML training batch." + e);
            }
        }
    }
}