using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;

namespace SlalomTracker.Cloud
{
    public class MachineLearningTags
    {
        // ML requires at least 5 of a given tag
        const int MinimumForTag = 5;

        private IEnumerable<SkiVideoEntity> m_videos;
        private IList<TrainingModels.Tag> m_tags;
        private CustomVisionTrainingClient m_trainingApi;
        private Guid m_projectId;

        public MachineLearningTags(CustomVisionTrainingClient trainingApi, 
                Guid projectId,
                IEnumerable<SkiVideoEntity> videos)
        {
            this.m_trainingApi = trainingApi;
            this.m_videos = videos;
            this.m_projectId = projectId;
            this.m_tags = m_trainingApi.GetTags(this.m_projectId);
        }

        public Guid? GetRopeTagId(SkiVideoEntity video)
        {
            if (video.RopeLengthM == 0)
                return null;
            
            string rope = video.RopeLengthM.ToString();
            
            var tag = m_tags.Where(t => t.Name == rope).FirstOrDefault();               
            if (tag == null)
            {
                if (HasEnoughOfRope(video.RopeLengthM))
                {
                    tag = m_trainingApi.CreateTag(m_projectId, rope);
                    m_tags.Add(tag);
                }
            }

            return tag.Id;
        }

        public Guid? GetSkierTagId(SkiVideoEntity video)
        {
            try
            {
                if (string.IsNullOrEmpty(video.Skier))
                    return null;

                string skier = video.Skier.Trim();

                var tag = m_tags.Where(t => t.Name == skier).FirstOrDefault();               
                if (tag == null)
                {
                    if (HasEnoughOfSkier(skier))
                    {
                        tag = m_trainingApi.CreateTag(m_projectId, skier);
                        m_tags.Add(tag);
                    }
                }

                return tag.Id;
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to get SkierTag for {video.Skier}\n" + e);
                return null;
            }
        }    

        private bool HasEnoughOfSkier(string skier)
        {          
            int count = m_videos.Where(e => e.Skier == skier).Count();
            return count >= MinimumForTag;
        }

        private bool HasEnoughOfRope(double rope)
        {          
            int count = m_videos.Where(e => e.RopeLengthM == rope).Count();
            return count >= MinimumForTag;
        }        
    }
}
