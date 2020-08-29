using System;
using System.Threading.Tasks;
using SlalomTracker.Cloud;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker.Video 
{
    public class CompareVideoProcessor : IProcessor
    {
        const string COMPARETABLE = "comparevideos";
        string _videoUrl1;
        string _videoUrl2;

        public CompareVideoProcessor(string videoUrl1, string videoUrl2)
        {
            _videoUrl1 = videoUrl1;
            _videoUrl2 = videoUrl2;
        }

        public async Task ProcessAsync()
        {
            try
            {
                string[] videos = await DownloadVideosAsync();
                
                VideoTasks videoTasks = new VideoTasks(videos[0]);
                string output = await videoTasks.CombineVideoAsync(videos[1]);
                
                Logger.Log($"Video {videos[0]} combined with {videos[1]} into {output}");

                Storage storage = new Storage();
                string url = storage.UploadVideo(output, DateTime.Now);

                ComparedVideoEntity entity = new ComparedVideoEntity(url, DateTime.Now) {
                    Video1Url = _videoUrl1,
                    Video2Url = _videoUrl2
                };

                await storage.AddTableEntityAsync(entity, COMPARETABLE);
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to combine {_videoUrl1} combined with {_videoUrl2}.", e);
            }
        }

        private async Task<string[]> DownloadVideosAsync()
        {
            IStorage storage = new Storage();
            Task<string> download1 = Task.Run( () => { return storage.DownloadVideo(_videoUrl1); });
            Task<string> download2 = Task.Run( () => { return storage.DownloadVideo(_videoUrl2); });
            await Task.WhenAll(download1, download2);

            return new string[] { download1.Result, download2.Result };
        }
    }
}