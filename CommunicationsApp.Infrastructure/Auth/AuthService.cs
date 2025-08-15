using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CommunicationsApp.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly HybridCache _cache;
        private const int RefreshTokenExpiryDays = 7;

        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration, HybridCache cache)
        {
            _userManager = userManager;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<AuthResult> RegisterAsync(RegisterDto registerDto)
        {
            var user = new ApplicationUser
            {
                UserName = registerDto.UserName,
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return new AuthResult
                {
                    Succeeded = false,
                    Errors = result.Errors.Select(e => e.Description)
                };
            }

            // For registration, platform can be passed as a parameter or defaulted
            return await GenerateAuthResultAsync(user, "default");
        }

        public async Task<AuthResult> LoginAsync(LoginDto loginDto)
        {
            var user = loginDto.EmailOrUsername.Contains("@")
                ? await _userManager.FindByEmailAsync(loginDto.EmailOrUsername)
                : await _userManager.FindByNameAsync(loginDto.EmailOrUsername);

            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return new AuthResult
                {
                    Succeeded = false,
                    Errors = new[] { "Invalid login attempt." }
                };
            }

            // Platform can be passed as a parameter or defaulted
            return await GenerateAuthResultAsync(user, "default");
        }

        public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
        {
            var tokenInfo = await _cache.GetOrCreateAsync<RefreshTokenInfo>(refreshToken, async entry => null);
            if (tokenInfo == null || tokenInfo.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return new AuthResult
                {
                    Succeeded = false,
                    Errors = new[] { "Invalid or expired refresh token." }
                };
            }

            var user = await _userManager.FindByIdAsync(tokenInfo.UserId);
            if (user == null)
            {
                return new AuthResult
                {
                    Succeeded = false,
                    Errors = new[] { "User not found." }
                };
            }

            await _cache.RemoveAsync(refreshToken);
            return await GenerateAuthResultAsync(user, tokenInfo.Platform);
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            await _cache.RemoveAsync(refreshToken);
            return true;
        }

        private async Task<AuthResult> GenerateAuthResultAsync(ApplicationUser user, string platform)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            var refreshToken = GenerateRefreshToken();
            var refreshTokenInfo = new RefreshTokenInfo
            {
                Id = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(RefreshTokenExpiryDays),
                Platform = platform
            };
            await _cache.SetAsync(refreshToken, refreshTokenInfo, new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromDays(RefreshTokenExpiryDays)
            });

            return new AuthResult
            {
                Succeeded = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}