using CommunicationsApp.Data;
using CommunicationsApp.Models;

namespace CommunicationsApp.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<ApplicationUser> GetUserFromDatabaseAsync(string userId, bool refreshCache = false);
        Task UpdateCacheAsync(ApplicationUser user);
        Task<dynamic> CreateAccountSettingsAsync(AccountSettings settings);
        Task<dynamic> GetAccountSettingsAsync(string userId);
    }
}
