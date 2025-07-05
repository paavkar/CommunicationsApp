using CommunicationsApp.Data;
using CommunicationsApp.Models;

namespace CommunicationsApp.Interfaces
{
    public interface IServerService
    {
        Task<Server> CreateServerAsync(Server server, ApplicationUser user);
        Task<Server?> GetServerByIdAsync(string serverId);
        Task<Server?> GetServerFromDatabaseAsync(string serverId);
        Task UpdateCacheAsync(string serverId, Server server);
        Task<dynamic> JoinServerByInvitationCode(string code, ApplicationUser user);
        Task<dynamic> LeaveServerAsync(string serverId, string userId);
        Task<dynamic> AddChannelClassAsync(ChannelClass channelClass);
        Task<dynamic> AddChannelAsync(string channelClassId, Channel channel);
    }
}
