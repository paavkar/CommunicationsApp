using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.Interfaces
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
