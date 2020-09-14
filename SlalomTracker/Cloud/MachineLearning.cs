using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using PredictionModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker.Cloud
{
    public abstract class MachineLearning
    {
        public const string ENV_MLKEY = "SKIMLKEY";
        public const string ENV_CROPURL = "SKICROPURL";
        protected string CropThumbnailUrl;
        protected string CustomVisionEndPoint;
        protected string CustomVisionKey;
        protected string CustomVisionModelName;
        protected string ResourceId;
        protected CustomVisionPredictionClient predictionApi;
        protected CustomVisionTrainingClient trainingApi;
        protected List<TrainingModels.ImageUrlCreateEntry> entries;
        protected IList<TrainingModels.Tag> tags;
        protected IEnumerable<SkiVideoEntity> videos;
        protected Guid ProjectId;
        protected const int MinimumForTag = 10;

        public MachineLearning()
        {
            // TODO: All of this needs to be moved to configuration file.            
            CustomVisionKey = Environment.GetEnvironmentVariable(ENV_MLKEY);
            CropThumbnailUrl = Environment.GetEnvironmentVariable(ENV_CROPURL) ?? 
                "https://ski.jasondel.com/api/crop?width=1600&thumbnailUrl=";
            CustomVisionEndPoint = "https://eastus.api.cognitive.microsoft.com/";    
            ResourceId = "/subscriptions/40a293b5-bd26-47ef-acc3-c001a5bfce82/resourceGroups/ski/providers/Microsoft.CognitiveServices/accounts/SlalomDetection";
        }

        public void Train(IEnumerable<SkiVideoEntity> allVideos)
        {
            trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = CustomVisionKey,
                Endpoint = CustomVisionEndPoint
            };        

            const int BatchSize = 10;
            this.videos = FilterVideos(allVideos);

            try
            {
                tags = trainingApi.GetTags(ProjectId);
                entries = new List<TrainingModels.ImageUrlCreateEntry>();

                foreach (var video in this.videos)
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

                // Send any remaining.
                if (entries.Count > 0)
                    SendBatch();

                // Kick off the training.
                var iteration = trainingApi.TrainProject(ProjectId);

                while (iteration.Status == "Training")
                {
                    System.Threading.Thread.Sleep(1000);

                    // Re-query the iteration to get it's updated status
                    iteration = trainingApi.GetIteration(ProjectId, iteration.Id);
                }

                // The iteration is now trained. Publish it to the prediction end point.
                trainingApi.PublishIteration(ProjectId, iteration.Id, CustomVisionModelName, ResourceId);

                Logger.Log($"Done publishing iteration {iteration.Id} of model {CustomVisionModelName}.");

            }
            catch (TrainingModels.CustomVisionErrorException e)
            {
                Logger.Log($"Unable to train model {CustomVisionModelName}:\n" + e.Response.Content, e);
            }            
        }

        /// <summary>
        /// Returns prediction or null if an error occurs.
        /// </summary>
        public virtual string Predict(string thumbnailUrl)
        {
            Logger.Log($"Making a prediction of {CustomVisionModelName} for: " + thumbnailUrl);

            predictionApi = new CustomVisionPredictionClient()
            {
                ApiKey = CustomVisionKey,
                Endpoint = CustomVisionEndPoint
            };

            try 
            {
                PredictionModels.ImageUrl thumbnail = new PredictionModels.ImageUrl(CropThumbnailUrl + thumbnailUrl);
                var result = predictionApi.ClassifyImageUrl(ProjectId, CustomVisionModelName, thumbnail);

                //LogPredicitions(result.Predictions);

                return GetHighestRankedPrediction(result.Predictions);
            }
            catch (PredictionModels.CustomVisionErrorException e)
            {
                Logger.Log($"Error making prediction for {thumbnailUrl}\n\t" + 
                    e.Response.Content, e);
                return null;
            }
        }

        private void LogPredicitions(IList<PredictionModels.PredictionModel> predictions)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine();

            foreach (var c in predictions)
                sb.Append($"\t{c.TagName}: {c.Probability:P1}\n");

            Logger.Log(sb.ToString());
        }

        private string GetHighestRankedPrediction(IList<PredictionModels.PredictionModel> predictions)
        {
            string ropeTagName = predictions
                .OrderByDescending(p => p.Probability)
                .Select(p => p.TagName)
                .First();
            
            return ropeTagName;
        }        

        /// <summary>
        /// Just select the valid videos that are valid for tagging.
        /// </summary>
        protected virtual IEnumerable<SkiVideoEntity> FilterVideos(IEnumerable<SkiVideoEntity> videos)
        {
            // Limited the scope to just the current year... this could be broader, but 
            // limiting it because that is all that has been audited to be properly tagged.
            var filtered = videos.ThisYear().Where(v => 
                    !v.MarkedForDelete &&
                    !string.IsNullOrEmpty(v.Skier) && 
                    !string.IsNullOrEmpty(v.ThumbnailUrl) &&
                    v.RopeLengthM > 0);

            //
            // All of this is handled by EnoughForTag
            // 
            
            // var skiers = filtered.Skiers();
            // var ropes = filtered.RopeLengths();

            // // Write out the groups;
            // foreach (var s in skiers)
            //     Console.WriteLine($"{s.Key}: {s.Count()}");
            // foreach (var r in ropes)
            //     Console.WriteLine($"{r.Key}: {r.Count()}");

            // // Must have at least 5 skiers, 5 ropes to be eligible to train.
            // var excludeSkiers = skiers.Where(s => s.Count() <= 5).Select(s => s.Key);
            // var excludeRopes = ropes.Where(s => s.Count() <= 5).Select(s => s.Key);

            // LogExcluded(excludeSkiers, excludeRopes);

            // var included = filtered.Where(v => 
            //     !excludeSkiers.Contains(v.Skier) &&
            //     !excludeRopes.Contains(v.RopeLengthM) );

            Logger.Log($"Original {videos.Count()}, filtered {filtered.Count()}");

            return filtered;
        }

        private void LogExcluded(IEnumerable<string> excludeSkiers, IEnumerable<double> excludeRopes)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach(var s in excludeSkiers)
                sb.AppendLine($"\t{s}");
            foreach(var s in excludeRopes)
                sb.AppendLine($"\t{s}");

            Logger.Log("Filtered out these tags:\n" + sb.ToString());            
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
            if (tag == null && EnoughForTag(tagName, enoughSelector))
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
                Logger.Log($"Sending batch of {entries.Count} urls to train {CustomVisionModelName}.");
                var batch = new TrainingModels.ImageUrlCreateBatch(entries);
                trainingApi.CreateImagesFromUrls(ProjectId, batch);        
            }
            catch (Exception e)
            {
                Logger.Log("Error writing ML training batch.", e);
            }
        }

        /// <summary>
        /// Ensures there are enough entries for a tag (ML requires at least 5).
        /// </summary>
        private bool EnoughForTag(string tagName, Func<SkiVideoEntity, string, bool> enoughSelector)
        {
            int count = videos.Where(v => enoughSelector(v, tagName)).Count();
            return count >= MinimumForTag;
        }

        protected abstract bool TagSelector(TrainingModels.Tag tag, SkiVideoEntity video);
        protected abstract bool EnoughSelector(SkiVideoEntity video, string tag);
        protected abstract string GetTagValue(SkiVideoEntity video);        
    }
}