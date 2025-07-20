using static CommunicationsApp.Core.Models.Enums;

namespace CommunicationsApp.Core.Models
{
    public class ServerPermission
    {
        public string Id { get; set; }
        public ServerPermissionType PermissionType { get; set; }
        public string PermissionName { get; set; }

        public override bool Equals(object obj)
        {
            return obj is not ServerPermission other
                ? false
                : Id == other.Id
                && PermissionType == other.PermissionType
                && PermissionName == other.PermissionName;
        }

        public override int GetHashCode()
            => HashCode.Combine(Id, PermissionType, PermissionName);

        public override string ToString()
        {
            return $"ServerPermission [Id={Id ?? "null"}, PermissionType={PermissionType}," +
                $"PermissionName={PermissionName}]";
        }
    }
}
