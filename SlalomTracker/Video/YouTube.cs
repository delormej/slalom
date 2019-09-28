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
using Data = Google.Apis.YouTube.v3.Data;

namespace MetadataExtractor
{
  /// <summary>
  /// YouTube Data API v3 sample: upload a video.
  /// Relies on the Google APIs Client Library for .NET, v1.7.0 or higher.
  /// See https://developers.google.com/api-client-library/dotnet/get_started
  /// </summary>
  public class YouTube
  {
    public void Upload(string localVideoPath)
    {
        Run(localVideoPath).Wait();
    }

    private async Task Run(string localVideoPath)
    {
      UserCredential credential;
      if (!File.Exists("youtube_client_secret.json"))
      {
        Console.WriteLine($"Unable to find secrets file in current dir: {Directory.GetCurrentDirectory()}");
        return;
      }

      string creds = File.OpenText("youtube_client_secret.json").ReadToEnd();
      Console.WriteLine("Creds: " + creds);

      using (var stream = new FileStream("youtube_client_secret.json", FileMode.Open, FileAccess.Read))
      {
        credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.Load(stream).Secrets,
            // This OAuth 2.0 access scope allows an application to upload files to the
            // authenticated user's YouTube channel, but doesn't allow other types of access.
            new[] { YouTubeService.Scope.YoutubeUpload },
            "user",
            CancellationToken.None
        );
      }

      var youtubeService = new YouTubeService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = credential,
        ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
      });

      var video = new Data.Video();
      video.Snippet = new Data.VideoSnippet();
      video.Snippet.Title = "Ski Video";
      //video.Snippet.Description = "Default Video Description";
      //video.Snippet.Tags = new string[] { "tag1", "tag2" };
      video.Snippet.CategoryId = "17"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
      video.Status = new Data.VideoStatus();
      video.Status.PrivacyStatus = "unlisted"; // or "private" or "public"
      var filePath = localVideoPath; // Replace with path to actual movie file.

      using (var fileStream = new FileStream(filePath, FileMode.Open))
      {
        var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
        videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
        videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;

        await videosInsertRequest.UploadAsync();
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

    void videosInsertRequest_ResponseReceived(Data.Video video)
    {
      Console.WriteLine("Video id '{0}' was successfully uploaded.", video.Id);
    }
  }
}