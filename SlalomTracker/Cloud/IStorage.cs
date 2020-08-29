using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SlalomTracker.Cloud
{
    public interface IStorage
    {
        string BlobStorageUri { get; }

        // QueueStorage
        void AddToQueue(string blobName, string videoUrl);

        // VideoStorage
        string DownloadVideo(string videoUrl);
        string UploadVideo(string localFile, DateTime creationTime);
        string UploadThumbnail(string localFile, DateTime creationTime);
        void DeleteIngestedBlob(string url);

        // VideoMetadataStorage
        void AddMetadata(SkiVideoEntity entity, string json);
        void UpdateMetadata(SkiVideoEntity entity);
        Task AddTableEntityAsync(BaseVideoEntity entity, string tableName);
        Task AddTableEntityAsync(SkiVideoEntity entity);
        SkiVideoEntity GetSkiVideoEntity(string recordedDate, string mp4Filename);
        Task<IEnumerable<SkiVideoEntity>> GetAllMetdataAsync();

        // CourseMetadataStorage
        List<Course> GetCourses();
        void UpdateCourse(Course course);
    }
}