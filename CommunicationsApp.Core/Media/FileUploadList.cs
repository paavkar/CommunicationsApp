using CommunicationsApp.Core.Media;

namespace CommunicationsApp.Core.Models
{
    public class FileUploadList : FileUploadBase
    {
        public Dictionary<string, string>? BlazorFiles { get; set; }
    }
}
