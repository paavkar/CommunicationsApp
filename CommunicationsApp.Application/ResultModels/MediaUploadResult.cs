using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.ResultModels
{
    public class MediaUploadResult : ResultBaseModel
    {
        public List<MediaAttachment>? Files { get; set; }
        public MediaAttachment? File { get; set; }
    }
}
