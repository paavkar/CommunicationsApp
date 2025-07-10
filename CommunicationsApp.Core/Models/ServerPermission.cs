using static CommunicationsApp.Core.Models.Enums;

namespace CommunicationsApp.Core.Models
{
    public class ServerPermission
    {
        public string Id { get; set; }
        public ServerPermissionType PermissionType { get; set; }
        public string PermissionName { get; set; }
    }
}
