using CommunicationsApp.Data;
using CommunicationsApp.Models;

namespace CommunicationsApp.Interfaces
{
    public interface IServerService
    {
        Task<Server> CreateServerAsync(Server server, ApplicationUser user);
        Task<Server?> GetServerByIdAsync(string serverId, string userId);
        Task<Server?> GetServerFromDatabaseAsync(string serverId, string userId);
        Task UpdateCacheAsync(string serverId, Server server);
        Task<dynamic> JoinServerByInvitationCode(string code, ApplicationUser user);
    }
}
