using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationsApp.Core.Models
{
    public class ServerRole : ServerRoleBase
    {
        [NotMapped]
        public List<ServerPermission> Permissions { get; set; } = [];

        public ServerRoleDTO ToDTO()
        {
            return new ServerRoleDTO
            {
                Id = Id,
                ServerId = ServerId,
                Name = Name,
                HexColour = HexColour,
                Hierarchy = Hierarchy,
                DisplaySeparately = DisplaySeparately,
            };
        }
    }
}
