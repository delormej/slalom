using System;
using System.Threading.Tasks;
using SlalomTracker;
using SlalomTracker.Cloud;

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
                CoursePass pass = await CreateCoursePass();

                do {
                    var createThumbnail = CreateThumbnailAsync(pass); 
                    var uploadThumbnail = UploadThumbnailAsync(createThumbnail, getCreationTime);
                    var trimAndSilence = TrimAndSilenceAsync(pass); 
                    var uploadVideo = UploadVideoAsync(trimAndSilence, getCreationTime);

                    await CreateAndUploadMetadataAsync(
                        pass,
                        uploadThumbnail,
                        uploadVideo
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
            Console.WriteLine($"Downloading video {_sourceVideoUrl}...");
            return Cloud.Storage.DownloadVideo(_sourceVideoUrl);
        }

        private Task<DateTime> GetCreationTimeAsync()
        {
            Console.WriteLine($"Getting creation time from video {_sourceVideoUrl}...");
            return _videoTasks.GetCreationTimeAsync().ContinueWith(t => 
                _creationTime = t.Result);
        }

        private async Task<string> CreateThumbnailAsync(CoursePass pass)
        {
           
            Console.WriteLine($"Creating Thumbnail for video {_sourceVideoUrl}...");
            double thumbnailAtSeconds = pass.GetSecondsAtEntry();
            string localThumbnailPath = await _videoTasks.GetThumbnailAsync(thumbnailAtSeconds);

            return localThumbnailPath;
        }

        private Task<string> TrimAndSilenceAsync(CoursePass pass)
        {
            return Task.Run(() => 
            {
                Console.WriteLine($"Trimming and silencing video {_sourceVideoUrl}...");
                double start = pass.GetSecondsAtEntry();
                double duration = pass.GetDurationSeconds();
                double total = pass.GetTotalSeconds();                           
                return _videoTasks.TrimAndSilenceVideo(start, duration, total); 
            });
        }

        private Task<string> UploadThumbnailAsync(Task<string> createThumbnail, Task<DateTime> getCreationTime)
        {
            return createThumbnail.ContinueWith(t => 
            {
                // Ensure creation time has been generated.
                getCreationTime.Wait();

                string localThumbnailPath = t.Result;
                Console.WriteLine($"Uploading thumbnail {localThumbnailPath}...");
                return _storage.UploadThumbnail(localThumbnailPath, _creationTime);
            });
        }

        private Task<string> UploadVideoAsync(Task<string> trimAndSilence, Task<DateTime> getCreationTime)
        {
            return trimAndSilence.ContinueWith(t => 
            {
                getCreationTime.Wait();

                string processedVideoPath = t.Result;
                Console.WriteLine($"Uploading video {processedVideoPath}...");
                return  _storage.UploadVideo(processedVideoPath, _creationTime);
            });
        }

        private async Task<CoursePass> CreateCoursePass()
        {
            await ExtractMetadataAsync();
            return _factory.FromJson(_json);
        }

        private Task ExtractMetadataAsync()
        {
            Console.WriteLine($"Extracting metadata from video {_sourceVideoUrl}...");
            return Task.Run(() => {              
                _json = MetadataExtractor.Extract.ExtractMetadata(_localVideoPath);
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
                        Task<string> uploadVideo)
        {
            // Wait until thumbnail is uploaded
            string thumbnailUrl = await uploadThumbnail; 
            
            // Kick off ML tasks async once we have thumbnailUrl
            var getSkierPrediction = GetSkierPredictionAsync(thumbnailUrl);
            var getRopePrediction = GetRopePredictionAsync(thumbnailUrl);
            
            // Wait until the video has uploaded
            string videoUrl = await uploadVideo;
            
            // Create the table entity and wait for predictions to come back
            SkiVideoEntity entity = CreateSkiVideoEntity(pass, thumbnailUrl, videoUrl);
            entity.Skier = await getSkierPrediction;
            entity.RopeLengthM = await getRopePrediction;

            Console.WriteLine($"Creating and uploading metadata for video {_localVideoPath}...");
            _storage.AddMetadata(entity, _json);
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

        private void DeleteIngestVideo()
        {
            Console.WriteLine($"Deleting source video at {_sourceVideoUrl}...");
            // Note this only deletes from the ingest folder.  It will fail if not ingest, but that's ok.
            _storage.DeleteIngestedBlob(_sourceVideoUrl);
        }       
    }
}