using Asp.Versioning;
using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationsApp.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPost("update-cache")]
        public async Task<IActionResult> UpdateCache([FromBody] ApplicationUser user)
        {
            await _userService.UpdateCacheAsync(user);
            return NoContent();
        }

        [HttpPost("account-settings")]
        public async Task<IActionResult> CreateAccountSettings([FromBody] AccountSettings settings)
        {
            var result = await _userService.CreateAccountSettingsAsync(settings);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("account-settings/{userId}")]
        public async Task<IActionResult> GetAccountSettings(string userId)
        {
            var result = await _userService.GetAccountSettingsAsync(userId);
            if (!result.Succeeded)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
    }
}