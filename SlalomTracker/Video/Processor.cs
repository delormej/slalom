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
        /// generates thumbnail, and uploads metadata, thumbnail, final video, deletes ingest video.
        /// </summary>
        /// <returns>Url of processed video.</returns>
        public void Process()
        {
            try
            {
                _localVideoPath = DownloadVideo();
                _videoTasks = new VideoTasks(_localVideoPath);

                var getCreationTime = GetCreationTimeAsync(); 
                var extractMetadata = ExtractMetadataAsync();

                CoursePass pass = null;

                do {
                    var createCoursePass = CreateCoursePass(extractMetadata);
                    var createThumbnail = CreateThumbnailAsync();
                    var uploadThumbnail = UploadThumbnailAsync(createThumbnail, getCreationTime);
                    var trimAndSilence =  TrimAndSilenceAsync(createCoursePass); 

                    var uploadVideo = UploadVideoAsync(trimAndSilence, getCreationTime);
                    
                    // TODO: so this seems like opp to use 'await' keyword??
                    //pass = await createCoursePass;
                    createCoursePass.Wait();
                    pass = createCoursePass.Result;

                    CreateAndUploadMetadata(
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

        private Task ExtractMetadataAsync()
        {
            Console.WriteLine($"Extracting metadata from video {_sourceVideoUrl}...");
            return Task.Run(() => {
                _json = MetadataExtractor.Extract.ExtractMetadata(_localVideoPath);
            });
        }     

        private Task<string> CreateThumbnailAsync()
        {
            Console.WriteLine($"Creating Thumbnail for video {_sourceVideoUrl}...");
            const double thumbnailAtSeconds = 0.5;
            return _videoTasks.GetThumbnailAsync(thumbnailAtSeconds);
        }

        private Task<string> TrimAndSilenceAsync(Task<CoursePass> extractMetadata)
        {
            return extractMetadata.ContinueWith(t => 
            {
                CoursePass pass = t.Result;
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

        private Task<CoursePass> CreateCoursePass(Task extractMetadata)
        {
            return Task.Run(() =>
                {
                    extractMetadata.Wait();
                    return _factory.FromJson(_json);
                }
            );
        }

        private CoursePass HasAnotherPass(in CoursePass lastPass)
        {
            CoursePass nextPass = _factory.GetNextPass(lastPass.Exit);
            return nextPass;
        }   

        private void CreateAndUploadMetadata(CoursePass pass,
                        Task<string> uploadThumbnail,
                        Task<string> uploadVideo)
        {

            Task.WaitAll(uploadThumbnail, uploadVideo);

            string thumbnailUrl = uploadThumbnail.Result;
            string videoUrl = uploadVideo.Result;

            Console.WriteLine($"Creating and uploading metadata for video {_localVideoPath}...");
            SkiVideoEntity entity = CreateSkiVideoEntity(pass, thumbnailUrl, videoUrl);
             
            // All I really need is the thumbnail to do predictions. TODO: Change this to parallelize.
            GetPredictions(entity);

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

        private void GetPredictions(SkiVideoEntity entity)
        {
            // Avoiding for now

            Console.WriteLine($"Getting machine learning preditions for video {_localVideoPath}...");
            RopeMachineLearning ropeMl = new RopeMachineLearning();
            SkierMachineLearning skierMl = new SkierMachineLearning();

            var rope = Task.Run(() => {
                entity.RopeLengthM = ropeMl.PredictRopeLength(entity.ThumbnailUrl);
            });
            var skier = Task.Run(() => {
                entity.Skier = skierMl.Predict(entity.ThumbnailUrl);          
            });
            
            Task.WaitAll(rope, skier);
        }     
        
        private void DeleteIngestVideo()
        {
            Console.WriteLine($"Deleting source video at {_sourceVideoUrl}...");
            // Note this only deletes from the ingest folder.  It will fail if not ingest, but that's ok.
            _storage.DeleteIngestedBlob(_sourceVideoUrl);
        }       
    }
}