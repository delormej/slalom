using System;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;


namespace SlalomTracker.Cloud
{
    public class RopeMachineLearning : MachineLearning
    {
        public RopeMachineLearning()
        {
            ProjectId = new Guid("4668e0c2-7e00-40cb-a58a-914eb988f44d");
            CustomVisionPredictionKey = "8d326cd29a0b4636beced3a4658c09cb";
            CustomVisionTrainingKey = "7191c8190b4949b98b35c140efd7b7e6";     
            CustomVisionModelName = "RopeLength"; 
            InitializeApis();
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