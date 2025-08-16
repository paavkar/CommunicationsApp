using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.DTOs
{
    public class RoleUpdateRequest
    {
        public ServerRole Role { get; set; }
        public RoleMemberLinking Linking { get; set; }
    }
}
