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
        protected IList<TrainingModels.Tag> tags;
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
            const int BatchSize = 10;

            try
            {
                tags = trainingApi.GetTags(ProjectId);
                entries = new List<TrainingModels.ImageUrlCreateEntry>();

                foreach (var video in FilterVideos(videos))
                {
                    IList<Guid> tagIds = GetTagIds(video);
                    if (tagIds == null) // No tags, move on.
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

        private IEnumerable<SkiVideoEntity> FilterVideos(IEnumerable<SkiVideoEntity> videos)
        {
            // Just select the valid videos that are valid for tagging.
            return videos.Where(v => 
                        v.RopeLengthM > 0 && 
                        !string.IsNullOrEmpty(v.Skier) &&
                        !string.IsNullOrEmpty(v.ThumbnailUrl)
                    );
        }

        private IList<Guid> GetTagIds(SkiVideoEntity video)
        {
            //
            // Returns a list of Guids, even though there is only 1 item, or return null if none.
            //

            List<Guid> tagIds = null;
            var tag = GetTagId(video, GetTagValue(video), TagSelector, EnoughSelector);
            if (tag != null)
            {
                tagIds = new List<Guid>();
                tagIds.Add((Guid)tag);
            }

            return tagIds;
        }

        private Guid? GetTagId(SkiVideoEntity video, string tagName,
            Func<TrainingModels.Tag, SkiVideoEntity, bool> tagSelector,
            Func<SkiVideoEntity, string, bool> enoughSelector)
        {
            // Forgive the ridiculous nature of all the Func<> parameters, but this generalizes this 
            // function so that the derived classes can specify their own implementation of what 
            // constitutes a tag without repeating itself; i.e. skier, ropeLength, etc...

            var tag = tags.Where(t => tagSelector(t, video)).FirstOrDefault();
            // No tags, see if we have enough data to create one.
            if (tag == null && enoughSelector(video, tagName))
                tag = CreateTag(video, tagName);
            
            if (tag != null)
                return tag.Id;
            else 
                return null;
        }        

        private TrainingModels.Tag CreateTag(SkiVideoEntity video, string tagName)
        {
            TrainingModels.Tag tag = trainingApi.CreateTag(ProjectId, tagName);
            tags.Add(tag);
            return tag;
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

        protected abstract bool TagSelector(TrainingModels.Tag tag, SkiVideoEntity video);
        protected abstract bool EnoughSelector(SkiVideoEntity video, string tag);
        protected abstract string GetTagValue(SkiVideoEntity video);        
    }
}