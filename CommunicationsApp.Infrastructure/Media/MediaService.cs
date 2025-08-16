using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Models;
using CommunicationsApp.SharedKernel.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace CommunicationsApp.Infrastructure.Services
{
    public class MediaService(IConfiguration configuration, ILogger<MediaService> logger,
        IStringLocalizer<CommunicationsAppLoc> localizer) : IMediaService
    {
        readonly string ConnectionString = configuration.GetValue<string>("BlobStorage:DefaultConnection")!;
        readonly string MessageContainerName = configuration.GetValue<string>("BlobStorage:MessageContainerName")!;
        readonly string AccountName = configuration.GetValue<string>("BlobStorage:AccountName")!;
        readonly string AccountKey = configuration.GetValue<string>("BlobStorage:AccountKey")!;
        readonly List<string> ImageFileExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".svg", ".heic", ".raw", ".ico"];
        readonly Dictionary<string, string> VideoFileMimeTypes = new()
        {
            { ".mp4", "video/mp4" },
            { ".mov", "video/quicktime" },
            { ".avi", "video/x-msvideo" },
            { ".mkv", "video/x-matroska" },
            { ".wmv", "video/x-ms-wmv" },
            { ".flv", "video/x-flv" },
            { ".webm", "video/webm" },
            { ".3gp", "video/3gpp" },
            { ".mpeg", "video/mpeg" },
            { ".mpg", "video/mpeg" },
            { ".ts", "video/mp2t" }
        };
        readonly Dictionary<string, string> AudioFileMimeTypes = new()
        {
            { ".mp3", "audio/mpeg" },
            { ".wav", "audio/wav" },
            { ".flac", "audio/flac" },
            { ".aac", "audio/aac" },
            { ".ogg", "audio/ogg" },
            { ".wma", "audio/x-ms-wma" },
            { ".m4a" , "audio/mp4" },
            { ".aiff", "audio/aiff" },
            { ".alac", "audio/alac" },
            { ".opus", "audio/opus" }
        };
        public async Task<MediaUploadResult> UploadPostMediaAsync(FileUploadList fileUpload, string messageId)
        {
            BlobContainerClient containerClient;
            if (configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

                        return chain.Build(cert);
                    }
                };

                var httpClient = new HttpClient(handler);
                var transport = new HttpClientTransport(httpClient);

                var options = new BlobClientOptions
                {
                    Transport = transport
                };

                var serviceUri = new Uri($"https://127.0.0.1:10000/{AccountName}");
                var cred = new Azure.Storage.StorageSharedKeyCredential(AccountName, AccountKey);
                var blobServiceClient = new BlobServiceClient(serviceUri, cred, options);
                containerClient = blobServiceClient.GetBlobContainerClient(MessageContainerName);
            }
            else
            {
                var blobServiceClient = new BlobServiceClient(ConnectionString);
                containerClient = blobServiceClient.GetBlobContainerClient(MessageContainerName);
            }

            List<MediaAttachment> files = [];

            var imageIndex = 0;
            var videoIndex = 0;
            var audioIndex = 0;
            var fileIndex = 0;

            foreach (var file in fileUpload.Files)
            {
                try
                {
                    var fileName = file.Value.FileName;
                    var fileExtension = Path.GetExtension(fileName).ToLower();
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", messageId, file.Key);
                    BlobClient blobClient;

                    if (ImageFileExtensions.Contains(fileExtension))
                    {
                        blobClient = containerClient.GetBlobClient($"{messageId}/image{imageIndex}/{fileName}");
                        await blobClient.UploadAsync(path: filePath, overwrite: true);
                        imageIndex++;
                    }
                    else if (VideoFileMimeTypes.TryGetValue(fileExtension, out var videoMime))
                    {
                        var uploadOptions = new BlobUploadOptions
                        {
                            HttpHeaders = new BlobHttpHeaders
                            {
                                ContentType = videoMime
                            }
                        };
                        blobClient = containerClient.GetBlobClient($"{messageId}/video{videoIndex}/{fileName}");
                        await blobClient.UploadAsync(path: filePath, options: uploadOptions);
                        videoIndex++;
                    }
                    else if (AudioFileMimeTypes.TryGetValue(fileExtension, out var audioMime))
                    {
                        var uploadOptions = new BlobUploadOptions
                        {
                            HttpHeaders = new BlobHttpHeaders
                            {
                                ContentType = audioMime
                            }
                        };

                        blobClient = containerClient.GetBlobClient($"{messageId}/audio{audioIndex}/{fileName}");
                        await blobClient.UploadAsync(path: filePath, options: uploadOptions);
                        audioIndex++;
                    }
                    else
                    {
                        blobClient = containerClient.GetBlobClient($"{messageId}/file{fileIndex}/{fileName}");
                        await blobClient.UploadAsync(path: filePath, overwrite: true);
                        fileIndex++;
                    }

                    file.Value.Url = blobClient.Uri.ToString();
                    files.Add(file.Value);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error uploading file to Blob storage in {Method}", nameof(UploadPostMediaAsync));
                    return new MediaUploadResult
                    {
                        Succeeded = false,
                        ErrorMessage = localizer["FileUploadProcessError"]
                    };
                }
            }

            return new MediaUploadResult { Succeeded = true, Files = files };
        }
    }
}
