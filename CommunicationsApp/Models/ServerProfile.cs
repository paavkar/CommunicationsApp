namespace CommunicationsApp.Models
{
    public class ServerProfile
    {
        public string Id { get; set; } = Guid.CreateVersion7().ToString();
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ServerId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
        public string Status { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;

        // Navigation properties
        public List<ServerRole> Roles { get; set; } = [];
    }
}
