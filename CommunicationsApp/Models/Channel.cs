using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationsApp.Models
{
    public class Channel
    {
        public string Id { get; set; } = Guid.CreateVersion7().ToString();
        public string Name { get; set; } = string.Empty;
        public string ServerId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public List<ChatMessage> Messages { get; set; } = [];
    }
}
