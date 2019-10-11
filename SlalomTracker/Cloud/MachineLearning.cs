using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;

namespace SlalomTracker.Cloud
{
    public abstract class MachineLearning
    {
        protected string CropThumbnailUrl;
        protected string CustomVisionEndPoint;
        protected string CustomVisionPredictionKey;
        protected string CustomVisionTrainingKey;
        protected string CustomVisionModelName;
        protected CustomVisionPredictionClient predictionApi;
        protected CustomVisionTrainingClient trainingApi;
        protected List<TrainingModels.ImageUrlCreateEntry> entries;
        protected MachineLearningTags mlTags;
        protected Guid ProjectId;

        public MachineLearning()
        {
            CropThumbnailUrl = "https://ski-app.azurewebsites.net/api/crop?thumbnailUrl=";         
        }

        protected void InitializeApis()
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

        public void Train(List<SkiVideoEntity> videos)
        {
            const int BatchSize = 63;
            var filteredVideos = videos.Where(v => 
                    v.RopeLengthM > 0 && 
                    !string.IsNullOrEmpty(v.Skier) &&
                    !string.IsNullOrEmpty(v.ThumbnailUrl)
            );

            try
            {
                mlTags = new MachineLearningTags(trainingApi, ProjectId, filteredVideos);
                entries = new List<TrainingModels.ImageUrlCreateEntry>();

                foreach (var video in filteredVideos)
                {
                    IList<Guid> tagIds = GetTagIds(video);
                    if (tagIds == null) // No tags.
                        continue;

                    entries.Add(new TrainingModels.ImageUrlCreateEntry() 
                    { 
                        Url = CropThumbnailUrl + video.ThumbnailUrl, 
                        TagIds = tagIds
                    });

                    if (entries.Count >= BatchSize)
                    {
                        SendBatch();
                        entries.Clear();
                    }
                }

                // Kick off the training.
                trainingApi.TrainProject(ProjectId);
            }
            catch (TrainingModels.CustomVisionErrorException e)
            {
                System.Console.WriteLine("Unable to train model.\n" + e.Message);
            }            
        }

        protected virtual IList<Guid> GetTagIds(SkiVideoEntity video)
        {
            return null;
        }

        private void SendBatch()    
        {
            try
            {
                Console.WriteLine($"Sending batch of {entries.Count} urls to train.");
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