using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;

namespace SlalomTracker.Cloud
{
    public class MachineLearning
    {
        const string cropThumbnailUrl = "https://ski-app.azurewebsites.net/api/crop?thumbnailUrl=";

        const string CustomVisionEndPoint = "https://ropelengthvision.cognitiveservices.azure.com/";
        const string CustomVisionPredictionKey = "8d326cd29a0b4636beced3a4658c09cb";
        const string CustomVisionModelName = "RopeLength";
        static Guid projectId = new Guid("4668e0c2-7e00-40cb-a58a-914eb988f44d");

        private CustomVisionPredictionClient endpoint;

        public MachineLearning()
        {
            endpoint = new CustomVisionPredictionClient()
            {
                ApiKey = CustomVisionPredictionKey,
                Endpoint = CustomVisionEndPoint
            };
        }

        public double PredictRopeLength(string thumbnailUrl)
        {
            Console.WriteLine("Making a prediction of rope length for: " + thumbnailUrl);

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
    }
}