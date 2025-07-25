using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.Interfaces
{
    public interface IServerRepository
    {
        Task<int> InsertServerAsync(Server server);
        Task<Server?> GetServerByIdAsync(string serverId);
        Task<Server?> GetServerByInvitationAsync(string invitationCode);
        Task<int> UpdateServerInfoAsync(string serverId, ServerInfoUpdate update);

        Task<int> InsertServerRoleAsync(ServerRole role);
        Task<int> UpdateServerRoleAsync(ServerRole role);
        Task<int> UpdateServerRoleHierarchyAsync(ServerRole role);
        Task<int> UpsertServerRolePermissionsAsync(string roleId, IEnumerable<string> permissionIds);
        Task<int> DeleteServerRolePermissionsNotInAsync(string roleId, IEnumerable<string> permissionIds);
        Task<int> InsertUserServerRoleAsync(string userId, string serverId, string roleId);
        Task<int> DeleteUserServerRoleAsync(string userId, string serverId, string roleId);
        Task<IEnumerable<ServerPermission>> GetAllPermissionsAsync();
        Task UpdateServerPermissionAsync(ServerPermission permission);
        Task UpsertServerPermissionAsync(ServerPermission permission);
        Task<Server> GetServerRolePermissionsAsync(Server server);

        Task<int> InsertServerProfileAsync(ServerProfile profile);
        Task<int> DeleteServerProfileAsync(string serverId, string userId);
        Task<int> DeleteServerProfilesAsync(string serverId, IEnumerable<string> userIds);

        Task<int> InsertChannelClassAsync(ChannelClass channelClass);
        Task<int> InsertChannelAsync(Channel channel);

        Task<Server?> LoadServerProfilesAsync(Server server);
    }
}
