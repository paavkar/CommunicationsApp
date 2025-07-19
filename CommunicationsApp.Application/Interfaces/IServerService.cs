using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.Interfaces
{
    public interface IServerService
    {
        Task<Server> CreateServerAsync(Server server, ApplicationUser user);
        Task<Server?> GetServerByIdAsync(string serverId);
        Task UpdateCacheAsync(string serverId, Server server);
        Task<dynamic> GetServerByInvitationAsync(string invitationCode);
        Task<dynamic> JoinServerAsync(Server server, ServerProfile profile);
        Task<dynamic> LeaveServerAsync(string serverId, string userId);
        Task<dynamic> AddChannelClassAsync(ChannelClass channelClass);
        Task<dynamic> AddChannelAsync(string channelClassId, Channel channel);
        Task<dynamic> AddServerPermissionsAsync();
        Task<List<ServerPermission>> GetServerPermissionsAsync();
        Task<dynamic> UpdateServerNameDescriptionAsync(string serverId, ServerInfoUpdate update);
        Task<dynamic> UpdateRoleAsync(string serverId, ServerRole role, List<ServerProfile> members);
        Task<dynamic> AddRoleAsync(string serverId, ServerRole role);
    }
}
