using CommunicationsApp.Data;
using CommunicationsApp.Models;

namespace CommunicationsApp.Interfaces
{
    public interface IServerService
    {
        Task<Server> CreateServerAsync(Server server, ApplicationUser user);
    }
}
