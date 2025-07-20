using Azure.Storage.Blobs;
using CommunicationsApp.Application.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommunicationsApp.Infrastructure.Services
{
    public class MediaService(IConfiguration configuration, ILogger<MediaService> logger) : IMediaService
    {
        readonly string ConnectionString = configuration.GetValue<string>("BlobStorage:DefaultConnection")!;
        readonly string MessageContainerName = configuration.GetValue<string>("BlobStorage:MessageContainerName")!;

        public async Task<List<string>> UploadPostImagesAsync(List<IBrowserFile> files, string messageId)
        {
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(MessageContainerName);

            List<string> fileUrls = [];

            var i = 0;
            foreach (var file in files)
            {
                var fileName = file.Name;

                try
                {
                    var blobClient = containerClient.GetBlobClient($"{messageId}/image{i}/{fileName}");
                    await blobClient.UploadAsync(file.OpenReadStream(), true);
                    fileUrls.Add(blobClient.Uri.ToString());
                    i++;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error uploading file to Blob storage in {Method}", nameof(UploadPostImagesAsync));
                }
            }

            return fileUrls;
        }
    }
}
