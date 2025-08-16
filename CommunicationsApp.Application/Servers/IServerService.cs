using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.Interfaces
{
    public interface IServerService
    {
        Task<Server> CreateServerAsync(Server server, ApplicationUser user);
        Task<Server?> GetServerByIdAsync(string serverId, string userId = "");
        Task UpdateCacheAsync(string serverId, Server server);
        Task<ServerResult> GetServerByInvitationAsync(string invitationCode);
        Task<ServerResult> JoinServerAsync(Server server, ServerProfile profile);
        Task<ServerResult> LeaveServerAsync(string serverId, string userId);
        Task<ServerResult> KickMembersAsync(string serverId, List<string> userIds);
        Task<ChannelClassResult> AddChannelClassAsync(ChannelClass channelClass);
        Task<ChannelResult> AddChannelAsync(string channelClassId, Channel channel);
        Task<ServerPermissionResult> AddServerPermissionsAsync();
        Task<List<ServerPermission>> GetServerPermissionsAsync();
        Task<ResultBaseModel> UpdateServerNameDescriptionAsync(string serverId, ServerInfoUpdate update);
        Task<ResultBaseModel> UpdateRoleAsync(string serverId, ServerRole role, RoleMemberLinking linking);
        Task<ResultBaseModel> AddRoleAsync(string serverId, ServerRole role);
    }
}
