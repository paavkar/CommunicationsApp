namespace CommunicationsApp.Core.Models
{
    public class ServerProfile : ServerProfileBase
    {
        // Navigation properties
        public List<ServerRole> Roles { get; set; } = [];

        public ServerProfileDTO ToDTO()
        {
            return new ServerProfileDTO
            {
                Id = Id,
                UserId = UserId,
                UserName = UserName,
                ServerId = ServerId,
                DisplayName = DisplayName,
                ProfilePictureUrl = ProfilePictureUrl,
                BannerUrl = BannerUrl,
                CreatedAt = CreatedAt,
                JoinedAt = JoinedAt,
                Status = Status,
                Bio = Bio,
                Roles = [.. Roles.Select(role => role.ToDTO())]
            };
        }
    }
}
