namespace CommunicationsApp.Core.Models
{
    public class MediaAttachment
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public string? AltText { get; set; }
        public long FileSize { get; set; }
    }
}
