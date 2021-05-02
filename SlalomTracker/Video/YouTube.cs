using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YouTubeVideo = Google.Apis.YouTube.v3.Data.Video;

namespace SlalomTracker.Video
{
  /// <summary>
  /// YouTube Data API v3 sample: upload a video.
  /// Relies on the Google APIs Client Library for .NET, v1.7.0 or higher.
  /// See https://developers.google.com/api-client-library/dotnet/get_started
  /// </summary>
  public class YouTubeHelper
  {
    private static readonly string[] YouTubeTags = { "3SeasonSkiClub", "Waterski" };    
    private YouTubeService _youtubeService;
    private YouTubeVideo _video;

    public YouTubeHelper(UserCredential credential)
    {
      // GoogleCredential.FromAccessToken(accessToken);
      _youtubeService = new YouTubeService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = credential,
        ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
      });      
    }

    public async Task<string> UploadAsync(string filePath)
    {
      _video = new YouTubeVideo();
      _video.Snippet = new VideoSnippet();
      _video.Snippet.Title = "Ski Video";
      // video.Snippet.Description = "Default Video Description";
      _video.Snippet.Tags = YouTubeTags;
      _video.Snippet.CategoryId = "17"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
      _video.Status = new VideoStatus();
      _video.Status.PrivacyStatus = "public"; // "unlisted" or "private" or "public"

      using (var fileStream = new FileStream(filePath, FileMode.Open))
      {
        var videosInsertRequest = _youtubeService.Videos.Insert(_video, "snippet,status", fileStream, "video/*");
        videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
        videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;

        await videosInsertRequest.UploadAsync();

        return $"https://www.youtube.com/watch?v={_video.Id}";
      }
    }

    void videosInsertRequest_ProgressChanged(Google.Apis.Upload.IUploadProgress progress)
    {
      switch (progress.Status)
      {
        case UploadStatus.Uploading:
          Console.WriteLine("{0} bytes sent.", progress.BytesSent);
          break;

        case UploadStatus.Failed:
          Console.WriteLine("An error prevented the upload from completing.\n{0}", progress.Exception);
          break;
      }
    }

    void videosInsertRequest_ResponseReceived(YouTubeVideo video)
    {
      Console.WriteLine("Video id '{0}' was successfully uploaded.", video.Id);
      _video = video;
    }
  }
}