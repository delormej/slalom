using System;
using System.Threading.Tasks;
using SlalomTracker.Cloud;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker.Video 
{
    public class SkiVideoProcessor
    {
        Storage _storage;
        CoursePassFactory _factory;
        VideoTasks _videoTasks;
        string _sourceVideoUrl;
        string _localVideoPath;
        string _json;
        DateTime _creationTime;

        public SkiVideoProcessor(string videoUrl)
        {
            _sourceVideoUrl = videoUrl;
            _storage = new Storage();
            _factory = new CoursePassFactory();
        }

        /// <summary>
        /// Downloads video, extracts metadata, trims video to just the course pass, removes audio, 
        /// generates thumbnail, uploads metadata, thumbnail, final video, deletes ingest video.
        /// </summary>
        /// <returns>Url of processed video.</returns>
        public async Task ProcessAsync()
        {
            try
            {
                _localVideoPath = DownloadVideo();
                _videoTasks = new VideoTasks(_localVideoPath);

                var getCreationTime = GetCreationTimeAsync(); 
                CoursePass pass = await CreateCoursePassAsync();

                do {
                    var createThumbnail = CreateThumbnailAsync(pass); 
                    var uploadThumbnail = UploadThumbnailAsync(createThumbnail, getCreationTime);
                    var trimAndSilence = TrimAndSilenceAsync(pass); 
                    var uploadVideo = UploadVideoAsync(trimAndSilence, getCreationTime);
                    var uploadHotVideo = UploadGoogleVideoAsync(trimAndSilence, getCreationTime);

                    await CreateAndUploadMetadataAsync(
                        pass,
                        uploadThumbnail,
                        uploadVideo,
                        uploadHotVideo
                    );
                } while ((pass = HasAnotherPass(in pass)) != null);

                DeleteIngestVideo();
            }
            catch (System.AggregateException aggEx)
            {
                throw new ApplicationException($"Unable to process {_sourceVideoUrl}.  Failed at: \n" +
                    aggEx.GetBaseException().Message);
            }
        }

        private string DownloadVideo()
        {
            Logger.Log($"Downloading video {_sourceVideoUrl}...");
            return Cloud.Storage.DownloadVideo(_sourceVideoUrl);
        }

        private async Task GetCreationTimeAsync()
        {
            Logger.Log($"Getting creation time from video {_sourceVideoUrl}...");
            _creationTime = await _videoTasks.GetCreationTimeAsync();
            Logger.Log($"Creation time is {_creationTime}");
        }

        private async Task<string> CreateThumbnailAsync(CoursePass pass)
        {  
            Logger.Log($"Creating Thumbnail for video {_sourceVideoUrl}...");

            double thumbnailAtSeconds = pass.GetSecondsAtEntry();
            string localThumbnailPath = await _videoTasks.GetThumbnailAsync(thumbnailAtSeconds);

            Logger.Log($"Thumbnail created at {localThumbnailPath}");

            return localThumbnailPath;
        }

        private Task<string> TrimAndSilenceAsync(CoursePass pass)
        {
            Logger.Log($"Trimming and silencing video {_sourceVideoUrl}...");

            return Task.Run(() => 
            {
                double start = pass.GetSecondsAtEntry();
                double duration = pass.GetDurationSeconds();
                double total = pass.GetTotalSeconds();                           
                
                return _videoTasks.TrimAndSilenceVideo(start, duration, total); 
            });
        }

        private async Task<string> UploadThumbnailAsync(Task<string> createThumbnail, Task getCreationTime)
        {
            await getCreationTime; // Ensure creation time has been generated.
            string localThumbnailPath = await createThumbnail;

            Logger.Log($"Uploading thumbnail {localThumbnailPath}...");
            string thumbnailUrl = _storage.UploadThumbnail(localThumbnailPath, _creationTime);
            Logger.Log($"Uploaded thumbnail to {thumbnailUrl}");

            return thumbnailUrl;
        }

//TODO: Consolidate these two storage upload methods.
        private async Task<string> UploadVideoAsync(Task<string> trimAndSilence, Task getCreationTime)
        {
            await getCreationTime; // Ensure creation time has been generated.
            string processedVideoPath = await trimAndSilence;

            Logger.Log($"Uploading video {processedVideoPath}...");
            string videoUrl = _storage.UploadVideo(processedVideoPath, _creationTime);
            Logger.Log($"Video uploaded to {videoUrl}");
            return videoUrl;
        }

        private async Task<string> UploadGoogleVideoAsync(Task<string> trimAndSilence, Task getCreationTime)
        {
            await getCreationTime; 
            string processedVideoPath = await trimAndSilence;

            Logger.Log($"Uploading video to Google {processedVideoPath}...");
            GoogleStorage storage = new GoogleStorage();
            string videoUrl = await storage.UploadVideoAsync(processedVideoPath, _creationTime);
            Logger.Log($"Video uploaded to Google: {videoUrl}");
            return videoUrl;
        }        

        private async Task<CoursePass> CreateCoursePassAsync()
        {
            await ExtractMetadataAsync();
            return _factory.FromJson(_json);
        }

        private Task ExtractMetadataAsync()
        {
            Logger.Log($"Extracting metadata from video {_sourceVideoUrl}...");
            return Task.Run(() => {              
                _json = MetadataExtractor.Extract.ExtractMetadata(_localVideoPath);
                Logger.Log("Extracted metadata.");
            });
        }     

        private CoursePass HasAnotherPass(in CoursePass lastPass)
        {
            if (lastPass.Exit == null)
                return null;

            CoursePass nextPass = _factory.GetNextPass(lastPass.Exit);
            return nextPass;
        }   

        private async Task CreateAndUploadMetadataAsync(CoursePass pass,
                        Task<string> uploadThumbnail,
                        Task<string> uploadVideo,
                        Task<string> uploadHotVideo)
        {
            // Wait until thumbnail is uploaded
            string thumbnailUrl = await uploadThumbnail; 
            
            // Kick off ML tasks async once we have thumbnailUrl
            var getSkierPrediction = GetSkierPredictionAsync(thumbnailUrl);
            var getRopePrediction = GetRopePredictionAsync(thumbnailUrl);
            
            // Wait until the video has uploaded
            string videoUrl = await uploadVideo;
            string hotVideoUrl = await uploadHotVideo;
            
            // Create the table entity and wait for predictions to come back
            SkiVideoEntity entity = CreateSkiVideoEntity(pass, thumbnailUrl, videoUrl);
            entity.Skier = await getSkierPrediction;
            entity.RopeLengthM = await getRopePrediction;
            entity.HotUrl = hotVideoUrl;

            Logger.Log($"Creating and uploading metadata for video {_localVideoPath}...");
            _storage.AddMetadata(entity, _json);
        }   

        private Task<double> GetRopePredictionAsync(string thumbnailUrl)
        {
            RopeMachineLearning ropeMl = new RopeMachineLearning();
            return Task.Run(() => ropeMl.PredictRopeLength(thumbnailUrl));
        }

        private Task<string> GetSkierPredictionAsync(string thumbnailUrl)
        {
            SkierMachineLearning skierMl = new SkierMachineLearning();
            return Task.Run(() => skierMl.Predict(thumbnailUrl));
        }

        private SkiVideoEntity CreateSkiVideoEntity(CoursePass pass, string thumbnailUrl, string videoUrl)
        {
            SkiVideoEntity entity = new SkiVideoEntity(videoUrl, _creationTime);
            entity.BoatSpeedMph = pass.AverageBoatSpeed;
            entity.CourseName = pass.Course.Name;
            entity.EntryTime = pass.GetSecondsAtEntry();     
            entity.RopeLengthM = pass.Rope != null ? pass.Rope.FtOff : 0;
            entity.CenterLineDegreeOffset = pass.CenterLineDegreeOffset;   
            entity.ThumbnailUrl = thumbnailUrl;   
            
            return entity;
        }  
        
        private void DeleteIngestVideo()
        {
            Logger.Log($"Deleting source video at {_sourceVideoUrl}...");
            // Note this only deletes from the ingest folder.  It will fail if not ingest, but that's ok.
            _storage.DeleteIngestedBlob(_sourceVideoUrl);
        }       
    }
}