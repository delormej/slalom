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
            ProjectId = new Guid("6cd1137c-b7db-46ee-a764-08af11ac012d");
            CustomVisionPredictionKey = "8d326cd29a0b4636beced3a4658c09cb";
            CustomVisionTrainingKey = "7191c8190b4949b98b35c140efd7b7e6";     
            CustomVisionModelName = "SkierModel";      
            InitializeApis(); 
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