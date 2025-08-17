using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Media;
using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.Interfaces
{
    public interface IMediaService
    {
        Task<MediaUploadResult> UploadPostMediaAsync(FileUploadList fileUpload, string messageId);
        Task<MediaUploadResult> UploadUserProfilePictureAsync(string userId, FileUploadSingle fileUpload);
        Task<MediaUploadResult> UploadServerProfilePictureAsync(string serverProfileId, FileUploadSingle fileUpload);
        Task<MediaUploadResult> UploadServerBannerAsync(string serverId, FileUploadSingle fileUpload);
        Task<MediaUploadResult> UploadServerIconAsync(string serverId, FileUploadSingle fileUpload);
        Task<MediaUploadResult> UploadSingleFileAsync(FileUploadSingle fileUpload, string blobPath);
    }
}
