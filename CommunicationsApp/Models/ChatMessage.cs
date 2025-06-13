namespace CommunicationsApp.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.CreateVersion7().ToString();
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsEdited { get; set; } = false;
        public DateTimeOffset? EditedAt { get; set; } = null;
        public string PartitionKey { get; set; } = string.Empty;
        public string Type { get; set; } = "ChatMessage";

        public Channel Channel { get; set; } = null!;
        public Server Server { get; set; } = null!;
        public ServerProfile Sender { get; set; } = null!;
        public ChatMessage? ReplyTo { get; set; }

        public List<string> Attachments { get; set; } = [];
        public Dictionary<string, List<ServerProfile>> Reactions { get; set; } = [];
    }
}
