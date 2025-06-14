using CommunicationsApp.Data;

namespace CommunicationsApp.Models
{
    public class Server
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InvitationCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string IconUrl { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;
        public string ServerType { get; set; } = string.Empty;

        // Navigation properties
        public List<ChannelClass> ChannelClasses { get; set; } = [];
        public List<ServerProfile> Members { get; set; } = [];
        public List<ServerRole> Roles { get; set; } = [];
        public List<ChatMessage> Messages { get; set; } = [];

        public override string ToString()
        {
            return $"Server [Id={Id ?? "null"}, Name={Name}, InvitationCode={InvitationCode}, " +
                   $"Description={Description}, OwnerId={OwnerId}, CreatedAt={CreatedAt}, " +
                   $"IconUrl={IconUrl}, BannerUrl={BannerUrl}, ServerType={ServerType}]";
        }
    }
}
