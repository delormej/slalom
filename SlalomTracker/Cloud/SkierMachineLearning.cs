using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using PredictionModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;

namespace SlalomTracker.Cloud
{
    public class SkierMachineLearning : MachineLearning
    {
        public SkierMachineLearning()
        {
            ProjectId = new Guid("c38bd611-86ee-43ff-ad76-20d339665e34");
            CustomVisionModelName = "SkierDetection";      
        }

        protected override string GetTagValue(SkiVideoEntity video)
        {
            return video.Skier.Trim();
        }

        protected override bool TagSelector(TrainingModels.Tag tag, SkiVideoEntity video)
        {
            if (tag == null || video == null)
                return false;
            else
                return tag.Name == video.Skier.Trim();
        }

        protected override bool EnoughSelector(SkiVideoEntity video, string tag)
        {
            return video.Skier.Trim() == tag;
        }
    }
}