using System;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;


namespace SlalomTracker.Cloud
{
    public class RopeMachineLearning : MachineLearning
    {
        public RopeMachineLearning()
        {
            ProjectId = new Guid(Environment.GetEnvironmentVariable("SKIMLROPEID"));
            CustomVisionModelName = Environment.GetEnvironmentVariable("SKIMLROPEMODEL");
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