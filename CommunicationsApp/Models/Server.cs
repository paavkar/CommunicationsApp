using CommunicationsApp.Data;

namespace CommunicationsApp.Models
{
    public class Server
    {
        public string Id { get; set; } = Guid.CreateVersion7().ToString();
        public string Name { get; set; } = string.Empty;
        public string InvitationCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public List<Channel> Channels { get; set; } = [];
        public List<ServerProfile> Members { get; set; } = [];
        public List<ServerRole> Roles { get; set; } = [];
        public List<ChatMessage> Messages { get; set; } = [];
    }
}
