using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.Interfaces
{
    public interface IMediaService
    {
        Task<MediaUploadResult> UploadPostMediaAsync(FileUploadList fileUpload, string messageId);
    }
}
