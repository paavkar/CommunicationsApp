using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.Interfaces
{
    public interface IMediaService
    {
        Task<List<MediaAttachment>> UploadPostMediaAsync(FileUploadList fileUpload, string messageId);
    }
}
