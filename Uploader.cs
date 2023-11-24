using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class Uploader
{
    private readonly YouTubeService _youtubeService;

    public Uploader(string apiKey, string applicationName)
    {
        _youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = apiKey,
            ApplicationName = applicationName
        });
    }

    public async Task<Video> UploadVideoAsync(string videoFilePath, string title, string description, string[] tags, string categoryId, string privacyStatus)
    {
        var video = new Video
        {
            Snippet = new VideoSnippet
            {
                Title = title,
                Description = description,
                Tags = tags,
                CategoryId = categoryId // See https://developers.google.com/youtube/v3/docs/videoCategories/list
            },
            Status = new VideoStatus
            {
                PrivacyStatus = privacyStatus // "private", "unlisted", or "public"
            }
        };

        using (var fileStream = new FileStream(videoFilePath, FileMode.Open))
        {
            var videosInsertRequest = _youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
            videosInsertRequest.ProgressChanged += VideosInsertRequest_ProgressChanged;
            videosInsertRequest.ResponseReceived += VideosInsertRequest_ResponseReceived;

            await videosInsertRequest.UploadAsync();
            return video;
        }
    }

    private void VideosInsertRequest_ProgressChanged(IUploadProgress progress)
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

    private void VideosInsertRequest_ResponseReceived(Video video)
    {
        Console.WriteLine("Video id '{0}' was successfully uploaded.", video.Id);
    }
}
