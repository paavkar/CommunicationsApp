using Azure.Storage.Blobs;
using CommunicationsApp.Application.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;

namespace CommunicationsApp.Infrastructure.Services
{
    public class MediaService(IConfiguration configuration) : IMediaService
    {
        readonly string ConnectionString = configuration.GetValue<string>("BlobStorage:DefaultConnection")!;
        readonly string MessageContainerName = configuration.GetValue<string>("BlobStorage:MessageContainerName")!;

        public async Task<List<string>> UploadPostImagesAsync(List<IBrowserFile> files)
        {
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(MessageContainerName);

            List<string> fileUrls = [];

            foreach (var file in files)
            {
                var fileName = file.Name;
                var blobClient = containerClient.GetBlobClient(fileName);
                await blobClient.UploadAsync(file.OpenReadStream(), true);

                fileUrls.Add(blobClient.Uri.ToString());
            }

            return fileUrls;
        }
    }
}
