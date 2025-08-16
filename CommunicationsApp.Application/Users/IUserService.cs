using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<ApplicationUser> GetUserFromDatabaseAsync(string userId);
        Task UpdateCacheAsync(ApplicationUser user);
        Task<AccountSettingsResult> CreateAccountSettingsAsync(AccountSettings settings);
        Task<AccountSettingsResult> GetAccountSettingsAsync(string userId);
    }
}
