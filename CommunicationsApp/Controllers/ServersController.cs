using Asp.Versioning;
using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommunicationsApp.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ServersController : ControllerBase
    {
        private readonly IServerService _serverService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServersController(IServerService serverService, UserManager<ApplicationUser> userManager)
        {
            _serverService = serverService;
            _userManager = userManager;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateServer([FromBody] Server server)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var result = await _serverService.CreateServerAsync(server, user);
            return Ok(result);
        }

        [HttpGet("{serverId}")]
        public async Task<IActionResult> GetServerById(string serverId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var server = await _serverService.GetServerByIdAsync(serverId, userId);
            if (server == null)
            {
                return NotFound();
            }

            return Ok(server);
        }

        [HttpPost("update-cache/{serverId}")]
        public async Task<IActionResult> UpdateCache(string serverId, [FromBody] Server server)
        {
            await _serverService.UpdateCacheAsync(serverId, server);
            return NoContent();
        }

        [HttpGet("invitation/{invitationCode}")]
        public async Task<IActionResult> GetServerByInvitation(string invitationCode)
        {
            var result = await _serverService.GetServerByInvitationAsync(invitationCode);
            if (!result.Succeeded)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinServer([FromBody] ServerJoinRequest request)
        {
            var result = await _serverService.JoinServerAsync(request.Server, request.Profile);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("leave/{serverId}")]
        public async Task<IActionResult> LeaveServer(string serverId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _serverService.LeaveServerAsync(serverId, userId);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("kick/{serverId}")]
        public async Task<IActionResult> KickMembers(string serverId, [FromBody] List<string> userIds)
        {
            var result = await _serverService.KickMembersAsync(serverId, userIds);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("channel-class")]
        public async Task<IActionResult> AddChannelClass([FromBody] ChannelClass channelClass)
        {
            var result = await _serverService.AddChannelClassAsync(channelClass);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("channel/{channelClassId}")]
        public async Task<IActionResult> AddChannel(string channelClassId, [FromBody] Channel channel)
        {
            var result = await _serverService.AddChannelAsync(channelClassId, channel);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("permissions")]
        public async Task<IActionResult> AddServerPermissions()
        {
            var result = await _serverService.AddServerPermissionsAsync();
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetServerPermissions()
        {
            var result = await _serverService.GetServerPermissionsAsync();
            return Ok(result);
        }

        [HttpPut("update/{serverId}")]
        public async Task<IActionResult> UpdateServerNameDescription(string serverId, [FromBody] ServerInfoUpdate update)
        {
            var result = await _serverService.UpdateServerNameDescriptionAsync(serverId, update);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPut("role/{serverId}")]
        public async Task<IActionResult> UpdateRole(string serverId, [FromBody] RoleUpdateRequest request)
        {
            var result = await _serverService.UpdateRoleAsync(serverId, request.Role, request.Linking);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("role/{serverId}")]
        public async Task<IActionResult> AddRole(string serverId, [FromBody] ServerRole role)
        {
            var result = await _serverService.AddRoleAsync(serverId, role);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}