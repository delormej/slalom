using System;
using System.Threading.Tasks;
using SlalomTracker.Cloud;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker.Video 
{
    public class SkiVideoProcessor : IProcessor
    {
        Storage _storage;
        CoursePassFactory _factory;
        VideoTasks _videoTasks;
        string _sourceVideoUrl;
        string _localVideoPath;
        string _json;
        DateTime _creationTime;
        VideoProcessedNotifier _processedNotifer;

        public SkiVideoProcessor(string videoUrl)
        {
            _sourceVideoUrl = videoUrl;
            _storage = new Storage();
            _factory = new CoursePassFactory();
            _processedNotifer = new VideoProcessedNotifier();
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
                var download = DownloadVideoAsync();
                var timeOverride = GetPassOverrideAsync();
                
                await Task.WhenAll(download, timeOverride);

                _localVideoPath = download.Result;
                VideoTime videoTime = timeOverride.Result;
                bool hasTimeOverride = videoTime != null;

                _videoTasks = new VideoTasks(_localVideoPath);

                var getCreationTime = GetCreationTimeAsync(); 
                CoursePass pass = await CreateCoursePassAsync();
                
                if (!hasTimeOverride && pass == null)
                    throw new ApplicationException($"No time override and no course pass found in {_localVideoPath}");

                do {
                    if (!hasTimeOverride)
                        videoTime = pass.VideoTime;

                    var createThumbnail = CreateThumbnailAsync(videoTime.Start); 
                    var uploadThumbnail = UploadThumbnailAsync(createThumbnail, getCreationTime);
                    var trimAndSilence = TrimAndSilenceAsync(videoTime); 
                    var uploadVideo = UploadVideoAsync(trimAndSilence, getCreationTime);
                    var uploadHotVideo = UploadGoogleVideoAsync(trimAndSilence, getCreationTime);
                    
                    // Some challenges with fit right now, so avoid till resolved.
                    // await FitCenterLineAsync(pass);
                    await CreateAndUploadMetadataAsync(
                        pass,
                        uploadThumbnail,
                        uploadVideo,
                        uploadHotVideo
                    );
                } while (!hasTimeOverride && (pass = HasAnotherPass(in pass)) != null);

                DeleteIngestVideo();
            }
            catch (System.AggregateException aggEx)
            {
                throw new ApplicationException($"Unable to process {_sourceVideoUrl}.  Failed at: \n" +
                    aggEx.GetBaseException().Message);
            }
        }

        private async Task<string> DownloadVideoAsync()
        {
            string localPath = null;
            await Task.Run( () => {
                Logger.Log($"Downloading video {_sourceVideoUrl}...");
                localPath = Cloud.Storage.DownloadVideo(_sourceVideoUrl);
            });
            return localPath;
        }

        private async Task GetCreationTimeAsync()
        {
            Logger.Log($"Getting creation time from video {_sourceVideoUrl}...");
            _creationTime = await _videoTasks.GetCreationTimeAsync();
            Logger.Log($"Creation time is {_creationTime}");
        }

        private async Task<string> CreateThumbnailAsync(double atSeconds)
        {  
            Logger.Log($"Creating Thumbnail for video {_sourceVideoUrl}...");           
            string localThumbnailPath = await _videoTasks.GetThumbnailAsync(atSeconds);
            Logger.Log($"Thumbnail created at {localThumbnailPath}");
            
            return localThumbnailPath;
        }

        private Task<string> TrimAndSilenceAsync(VideoTime videoTime)
        {
            Logger.Log($"Trimming and silencing video {_sourceVideoUrl}...");           
            return _videoTasks.TrimAndSilenceVideoAsync(videoTime.Start, videoTime.Duration); 
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
            string videoUrl = null;

            try 
            {
                Logger.Log($"Uploading video {processedVideoPath}...");
                videoUrl = _storage.UploadVideo(processedVideoPath, _creationTime);
                Logger.Log($"Video uploaded to {videoUrl}");
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to upload {processedVideoPath} to Azure.", e);
            }

            return videoUrl;
        }

        private async Task<string> UploadGoogleVideoAsync(Task<string> trimAndSilence, Task getCreationTime)
        {
            string videoUrl = "";
            try 
            {
                await getCreationTime; 
                string processedVideoPath = await trimAndSilence;

                Logger.Log($"Uploading video to Google {processedVideoPath}...");
                GoogleStorage storage = new GoogleStorage();
                videoUrl = await storage.UploadVideoAsync(processedVideoPath, _creationTime);
                Logger.Log($"Video uploaded to Google: {videoUrl}");
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to upload to google, but ignoring.", e);
            }
            
            return videoUrl;
        }        

        private async Task<CoursePass> CreateCoursePassAsync()
        {
            await ExtractMetadataAsync();
            return _factory.FromJson(_json);
        }

        private async Task ExtractMetadataAsync()
        {
            Logger.Log($"Extracting metadata from video {_sourceVideoUrl}...");
            await Task.Run(() => {              
                _json = MetadataExtractor.Extract.ExtractMetadata(_localVideoPath);
            });
            Logger.Log("Extracted metadata.");
        } 

        /// <summary>
        /// Download and process JSON CoursePass override if it exists.
        /// </summary>
        private async Task<VideoTime> GetPassOverrideAsync()    
        {
            VideoTime overrides = null;
            try 
            {
                await Task.Run( () => {
                    string jsonOverrideUrl = VideoTime.GetVideoJsonUrl(_sourceVideoUrl);
                    string jsonPath = Cloud.Storage.DownloadVideo(jsonOverrideUrl);
                    if (jsonPath != null)
                    {
                        overrides = VideoTime.FromJsonFile(jsonPath);
                        Logger.Log($"Video overrides found start, duration: {overrides.Start}, {overrides.Duration}");
                    }
                });
            }
            catch (System.Net.WebException)
            {
                Logger.Log("No json override found for " + _sourceVideoUrl);
            }
        
            return overrides;
        }

        private CoursePass HasAnotherPass(in CoursePass lastPass)
        {
            CoursePass nextPass = null;
            
            try 
            {            
                if (lastPass != null && lastPass?.Exit != null)
                    nextPass = _factory.GetNextPass(lastPass.Exit);
            }
            catch (ApplicationException e)
            {
                Logger.Log("Unable to find another pass.", e);
            }
            
            if (nextPass != null)
                _creationTime = _creationTime.AddSeconds(nextPass.GetSecondsAtEntry55());

            return nextPass;
        }   

        /// <summary>
        /// Finds the best center line offset to use and updates the pass object.
        /// </summary>
        // private Task FitCenterLineAsync(CoursePass pass)
        // {
        //     if (pass == null)
        //         return Task.CompletedTask;

        //     return Task.Run( () => {
        //         CoursePassFactory factory = new CoursePassFactory();
        //         pass.CenterLineDegreeOffset = factory.FitPass(pass);
        //     });
        // }

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
            SkiVideoEntity entity = CreateSkiVideoEntity(pass, videoUrl);
            entity.Skier = await getSkierPrediction;
            entity.RopeLengthM = await getRopePrediction;
            entity.ThumbnailUrl = thumbnailUrl;

            if (!string.IsNullOrWhiteSpace(hotVideoUrl)) 
                entity.HotUrl = hotVideoUrl;

            Logger.Log($"Creating and uploading metadata for video {_localVideoPath}...");
            _storage.AddMetadata(entity, _json);
            
            await _processedNotifer.NotifyAsync(entity.Skier, entity.RowKey);
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

        private SkiVideoEntity CreateSkiVideoEntity(CoursePass pass, string videoUrl)
        {
            SkiVideoEntity entity = new SkiVideoEntity(videoUrl, _creationTime);

            if (pass != null) 
            {
                entity.BoatSpeedMph = pass.AverageBoatSpeed;
                entity.CourseName = pass.Course.Name;
                entity.EntryTime = pass.GetSecondsAtEntry55();     
                entity.RopeLengthM = pass.Rope != null ? pass.Rope.FtOff : 0;
                entity.CenterLineDegreeOffset = pass.CenterLineDegreeOffset;   
            }
            
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