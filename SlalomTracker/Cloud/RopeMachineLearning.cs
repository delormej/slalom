using System;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;


namespace SlalomTracker.Cloud
{
    public class RopeMachineLearning : MachineLearning
    {
        public RopeMachineLearning()
        {
            ProjectId = new Guid("e3ee86a8-f298-46b5-87fd-31a09f0480d7");
            CustomVisionModelName = "RopeDetection"; 
        }

        public double PredictRopeLength(string thumbnail)
        {
            string ropeTag = Predict(thumbnail);
            double rope = 0;

            if (!string.IsNullOrEmpty(ropeTag))
                double.TryParse(ropeTag, out rope);

            return rope;
        }

        protected override string GetTagValue(SkiVideoEntity video)
        {
            return video.RopeLengthM.ToString();
        }

        protected override bool TagSelector(TrainingModels.Tag tag, SkiVideoEntity video)
        {
            if (tag == null || video == null)
                return false;
            else
                return tag.Name == video.RopeLengthM.ToString();
        }

        protected override bool EnoughSelector(SkiVideoEntity video, string tag)
        {
            double rope;
            if (!double.TryParse(tag, out rope))
                return false;
            
            return video.RopeLengthM == rope;
        }
    }
}