using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using PredictionModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;

namespace SlalomTracker.Cloud
{
    public class RopeMachineLearning : MachineLearning
    {
        public RopeMachineLearning()
        {
            ProjectId = new Guid("4668e0c2-7e00-40cb-a58a-914eb988f44d");
            CustomVisionEndPoint = "https://ropelengthvision.cognitiveservices.azure.com/";
            CustomVisionPredictionKey = "8d326cd29a0b4636beced3a4658c09cb";
            CustomVisionTrainingKey = "7191c8190b4949b98b35c140efd7b7e6";     
            CustomVisionModelName = "Iteration6"; //"RopeLength";       
            InitializeApis();
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

        private double GetHighestRankedPrediction(IList<PredictionModels.PredictionModel> predictions)
        {
            double ropeLength = 0.0d;
            
            string ropeTagName = predictions
                .OrderByDescending(p => p.Probability)
                .Select(p => p.TagName)
                .First();

            if (ropeTagName != null)
                ropeLength = double.Parse(ropeTagName);
            
            return ropeLength;
        }

        protected override IList<Guid> GetTagIds(SkiVideoEntity video)
        {
            // Requires a list object, but only a single tag is ever put in the list.
            List<Guid> tagIds = null;
            var ropeTag = mlTags.GetRopeTagId(video);
            if (ropeTag != null)
            {
                tagIds = new List<Guid>();
                tagIds.Add((Guid)ropeTag);
            }

            return tagIds;
        }
    }
}