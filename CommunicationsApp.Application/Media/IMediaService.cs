using Microsoft.AspNetCore.Components.Forms;

namespace CommunicationsApp.Application.Interfaces
{
    public interface IMediaService
    {
        Task<List<string>> UploadPostImagesAsync(List<IBrowserFile> files, string messageId);
    }
}
