using Newtonsoft.Json;

namespace CommunicationsApp.Models
{
    public class ChatMessage
    {
        [JsonProperty(PropertyName = "id")]
        public string? Id { get; set; }
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "sentAt")]
        public DateTimeOffset SentAt { get; set; }
        [JsonProperty(PropertyName = "isEdited")]
        public bool IsEdited { get; set; } = false;
        [JsonProperty(PropertyName = "editedAt")]
        public DateTimeOffset? EditedAt { get; set; } = null;
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; } = "ChatMessage";

        [JsonProperty(PropertyName = "channel")]
        public Channel Channel { get; set; } = null!;
        //[JsonProperty(PropertyName = "server")]
        //public Server Server { get; set; } = null!;
        [JsonProperty(PropertyName = "sender")]
        public ServerProfile Sender { get; set; } = null!;
        [JsonProperty(PropertyName = "replyTo")]
        public ChatMessage? ReplyTo { get; set; }

        [JsonProperty(PropertyName = "attachments")]
        public List<string> Attachments { get; set; } = [];
        [JsonProperty(PropertyName = "reactions")]
        public Dictionary<string, List<ServerProfile>> Reactions { get; set; } = [];
        [JsonProperty(PropertyName = "mentions")]
        public List<ServerProfile> Mentions { get; set; } = [];

        public override string ToString()
        {
            var editedAtStr = EditedAt.HasValue ? EditedAt.Value.ToString() : "N/A";
            var replyToId = ReplyTo?.Id ?? "None";
            return $"ChatMessage [Id={Id ?? "null"}, Content={Content}, SentAt={SentAt}, IsEdited={IsEdited}, " +
                   $"EditedAt={editedAtStr}, PartitionKey={PartitionKey}, Type={Type}, " +
                   $"Channel={Channel?.Name ?? "null"}, Sender={Sender?.UserName ?? "null"}, ReplyToId={replyToId}]";
        }
    }
}
