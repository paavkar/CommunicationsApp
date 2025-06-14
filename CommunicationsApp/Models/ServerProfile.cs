namespace CommunicationsApp.Models
{
    public class ServerProfile
    {
        public string? Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ServerId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? JoinedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;

        // Navigation properties
        public List<ServerRole> Roles { get; set; } = [];

        public override string ToString()
        {
            return $"ServerProfile [Id={Id ?? "null"}, UserId={UserId}, UserName={UserName}, " +
                   $"ServerId={ServerId}, DisplayName={DisplayName}, ProfilePictureUrl={ProfilePictureUrl}, " +
                   $"BannerUrl={BannerUrl}, CreatedAt={(CreatedAt?.ToString() ?? "null")}, " +
                   $"JoinedAt={(JoinedAt?.ToString() ?? "null")}, Status={Status}, Bio={Bio}]";
        }
    }
}
