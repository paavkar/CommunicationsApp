using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationsApp.Models
{
    public class ChatMessage
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.CreateVersion7().ToString();
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "sentAt")]
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
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
        [NotMapped]
        [JsonProperty(PropertyName = "reactions")]
        public Dictionary<string, List<ServerProfile>> Reactions { get; set; } = [];
    }
}
