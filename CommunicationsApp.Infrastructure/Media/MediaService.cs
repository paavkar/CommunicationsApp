using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
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
        readonly List<string> VideoFileExtensions = [".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm", ".3gp", ".mpeg", ".mpg", ".ts"];
        readonly List<string> AudioFileExtensions = [".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".aiff", ".alac", ".opus"];

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
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", file.Key);
                    BlobClient blobClient;

                    if (ImageFileExtensions.Contains(fileExtension))
                    {
                        blobClient = containerClient.GetBlobClient($"{messageId}/image{imageIndex}/{fileName}");
                        imageIndex++;
                    }
                    else if (VideoFileExtensions.Contains(fileExtension))
                    {
                        blobClient = containerClient.GetBlobClient($"{messageId}/video{videoIndex}/{fileName}");
                        videoIndex++;
                    }
                    else if (AudioFileExtensions.Contains(fileExtension))
                    {
                        blobClient = containerClient.GetBlobClient($"{messageId}/audio{audioIndex}/{fileName}");
                        audioIndex++;
                    }
                    else
                    {
                        blobClient = containerClient.GetBlobClient($"{messageId}/file{fileIndex}/{fileName}");
                        fileIndex++;
                    }

                    await blobClient.UploadAsync(path: filePath, overwrite: true);
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
