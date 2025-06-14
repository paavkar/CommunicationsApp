using CommunicationsApp.Data;

namespace CommunicationsApp.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser> GetUserByIdAsync(string userId);
    }
}
