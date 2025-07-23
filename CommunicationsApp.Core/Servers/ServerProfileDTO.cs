namespace CommunicationsApp.Core.Models
{
    public class ServerProfileDTO : ServerProfileBase
    {
        public List<ServerRoleDTO> Roles { get; set; }
    }
}
