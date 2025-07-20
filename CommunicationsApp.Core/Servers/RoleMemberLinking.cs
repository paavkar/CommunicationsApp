namespace CommunicationsApp.Core.Models
{
    public class RoleMemberLinking
    {
        public List<ServerProfile> NewMembers { get; set; }
        public List<ServerProfile> RemovedMembers { get; set; }
    }
}
