using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Application.ResultModels;

namespace CommunicationsApp.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterDto registerDto);
        Task<AuthResult> LoginAsync(LoginDto loginDto);
        Task<AuthResult> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
    }
}