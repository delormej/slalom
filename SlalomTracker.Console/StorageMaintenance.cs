
using System;
using SlalomTracker.Cloud;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Logger = jasondel.Tools.Logger;

namespace SkiConsole
{
    public class StorageMaintenance
    {
        private GoogleStorage _googleStore;
        private AzureStorage _azureStore;
        protected record VideoFiles(string Video, string Thumbnail);

        public StorageMaintenance()
        {
            string dbProjectId = System.Environment.GetEnvironmentVariable("FIRESTORE_PROJECT_ID");
            _googleStore = new GoogleStorage(dbProjectId);            
            _azureStore = new AzureStorage();
        }

        public async Task<int> MoveFromGcpToAzureAsync(int count)
        {
            var videos = await GetGcpVideosAsync(count);
            var tasks = videos.Select(v => MoveVideoAsync(v));
            await Task.WhenAll(tasks);
            
            var completed = tasks.Where(t => t.IsCompletedSuccessfully == true).Count();
            
            return completed;
        }

        private async Task<IEnumerable<SkiVideoEntity>> GetGcpVideosAsync(int count) {
            var videos = await _googleStore.GetAllMetdataAsync();
            var gcpVideos = videos.Where(v => v.Url.StartsWith("https://storage.googleapis.com") );

            return gcpVideos.Take(count);
        }

        private async Task MoveVideoAsync(SkiVideoEntity video) 
        {
            Logger.Log($"Moving {video.Date}\t{video.Url}");
            try
            {
                VideoFiles localFiles = await DownloadFromGoogleAsync(video);
                VideoFiles azureUrls = await UploadToAzureAsync(video, localFiles);
                string gcpUrl = video.Url;
                
                await Task.WhenAll(
                    UpdateMetadataAsync(video, azureUrls)
                    // DeleteFromGoogleAsync(gcpUrl)                
                );
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to move {video.Url}", e);
            }
            Logger.Log($"Moved {video.Date}\t{video.Url}");
        }

        private async Task<VideoFiles> DownloadFromGoogleAsync(SkiVideoEntity video)
        {
            Logger.Log($"Downloading {video.Date}\t{video.Url} and {video.ThumbnailUrl}");

            string videoPath = "", thumbnailPath = "";

            await Task.WhenAll(
                Task.Run(() => videoPath = _googleStore.DownloadVideo(video.Url)),
                Task.Run(() => thumbnailPath = _googleStore.DownloadVideo(video.ThumbnailUrl))
            );

            // Local paths
            return new VideoFiles(videoPath, thumbnailPath);
        }

        private async Task<VideoFiles> UploadToAzureAsync(SkiVideoEntity video, VideoFiles localFiles)
        {
            Logger.Log($"Uploading to Azure {localFiles.Video} & {localFiles.Thumbnail}");
            
            string videoUrl = "", thumbnailUrl = "";
            await Task.WhenAll(
                Task.Run(() => videoUrl = _azureStore.UploadVideo(localFiles.Video, video.RecordedTime)),
                Task.Run(() => thumbnailUrl = _azureStore.UploadVideo(localFiles.Thumbnail, video.RecordedTime))
            );

            // Return AzureUrls
            return new VideoFiles(videoUrl, thumbnailUrl);
        }

        private async Task UpdateMetadataAsync(SkiVideoEntity video, VideoFiles azureUrls)
        {
            if (string.IsNullOrWhiteSpace(azureUrls.Video) || 
                    string.IsNullOrWhiteSpace(azureUrls.Thumbnail))
            {
                throw new ApplicationException($"Azure Urls are empty for {video.Url}");
            }

            Logger.Log($"Updating metadata {video.Date}\t{azureUrls.Video}");
            video.Url = azureUrls.Video;
            video.ThumbnailUrl = azureUrls.Thumbnail;
            video.HotUrl = null;
            
            await _googleStore.AddTableEntityAsync(video);
        }

        // private async Task DeleteFromGoogleAsync(string gcpUrl)
        // {
        //     Console.WriteLine($"Deleting {gcpUrl}");
        // }
    }
}
