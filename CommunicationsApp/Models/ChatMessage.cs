namespace CommunicationsApp.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.CreateVersion7().ToString();
        public string Content { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
        public string ServerId { get; set; } = string.Empty;
        public string ChannelId { get; set; } = string.Empty;
        public bool IsEdited { get; set; } = false;
        public DateTimeOffset? EditedAt { get; set; } = null;
    }
}
