using CommunicationsApp.Data;

namespace CommunicationsApp.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<ApplicationUser> GetUserFromDatabaseAsync(string userId, bool refreshCache = false);
        Task UpdateCacheAsync(string userId, ApplicationUser user);
    }
}
