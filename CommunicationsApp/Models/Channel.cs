using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationsApp.Models
{
    public class Channel
    {
        public string Id { get; set; } = Guid.CreateVersion7().ToString();
        public string Name { get; set; } = string.Empty;
        public string ServerId { get; set; } = string.Empty;
        public string ChannelClassId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPrivate { get; set; } = false;
        public int OrderNumber { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public List<ChatMessage> Messages { get; set; } = [];
    }
}
