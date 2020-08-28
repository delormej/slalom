using System;
using System.Collections.Generic;
using System.Linq;
 using SlalomTracker.Cloud;

namespace SlalomTracker
{
    /// <summary>
    /// Helper extension methods for IEnumerable<SkiVideoEntity>.
    /// </summary>
    public static class SkiVideoEntityExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<SkiVideoEntity> ThisYear(this IEnumerable<SkiVideoEntity> videos)
        {
            IEnumerable<SkiVideoEntity> thisYearVideos = from v in videos
                where v.RecordedTime.Year == DateTime.UtcNow.Year
                    && !string.IsNullOrEmpty(v.Skier) 
                    && v.RopeLengthM > 0
                select v;

            return thisYearVideos;
        }

        public static IEnumerable<IGrouping<string, SkiVideoEntity>> Skiers(this IEnumerable<SkiVideoEntity> videos)
        {
            var skiers = 
                from v in videos
                group v by v.Skier into skiersGroup
                select skiersGroup;
            
            return skiers;
        }

        public static IEnumerable<IGrouping<double, SkiVideoEntity>> RopeLengths(this IEnumerable<SkiVideoEntity> videos)
        {
            var ropes = 
                from v in videos
                group v by v.RopeLengthM into ropeGroup
                select ropeGroup;

            return ropes;
        }

        public static IEnumerable<IGrouping<string, SkiVideoEntity>> Dates(this IEnumerable<SkiVideoEntity> videos)
        {
            var dates = 
                from v in videos
                group v by v.RecordedTime.ToShortDateString() into dateGroup
                select dateGroup;

            return dates;
        }
    }
}