using static CommunicationsApp.Models.Enums;

namespace CommunicationsApp.Models
{
    public class ServerPermission
    {
        public string Id { get; set; }
        public ServerPermissionType PermissionType { get; set; }
        public string PermissionName { get; set; }
    }
}
