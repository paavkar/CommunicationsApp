using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.Interfaces
{
    public interface IMediaService
    {
        Task<List<string>> UploadPostMediaAsync(FileUploadList fileUpload, string messageId);
    }
}
