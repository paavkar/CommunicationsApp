using Asp.Versioning;
using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Application.ResultModels;
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
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);
            Server result = await _serverService.CreateServerAsync(server, user);
            return Ok(result);
        }

        [HttpGet("{serverId}")]
        public async Task<IActionResult> GetServerById(string serverId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Server? server = await _serverService.GetServerByIdAsync(serverId, userId);
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
            ServerResult result = await _serverService.GetServerByInvitationAsync(invitationCode);
            if (!result.Succeeded)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinServer([FromBody] ServerJoinRequest request)
        {
            ServerResult result = await _serverService.JoinServerAsync(request.Server, request.Profile);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("leave/{serverId}")]
        public async Task<IActionResult> LeaveServer(string serverId, [FromBody] ServerProfile member)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ServerResult result = await _serverService.LeaveServerAsync(serverId, userId, member);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("kick/{serverId}")]
        public async Task<IActionResult> KickMembers(string serverId, [FromBody] List<ServerProfile> members)
        {
            ServerResult result = await _serverService.KickMembersAsync(serverId, members);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("channel-class")]
        public async Task<IActionResult> AddChannelClass([FromBody] ChannelClass channelClass)
        {
            ChannelClassResult result = await _serverService.AddChannelClassAsync(channelClass);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("channel/{channelClassId}")]
        public async Task<IActionResult> AddChannel(string channelClassId, [FromBody] Channel channel)
        {
            ChannelResult result = await _serverService.AddChannelAsync(channelClassId, channel);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("permissions")]
        public async Task<IActionResult> AddServerPermissions()
        {
            ServerPermissionResult result = await _serverService.AddServerPermissionsAsync();
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetServerPermissions()
        {
            List<ServerPermission> result = await _serverService.GetServerPermissionsAsync();
            return Ok(result);
        }

        [HttpPut("update/{serverId}")]
        public async Task<IActionResult> UpdateServerNameDescription(string serverId, [FromBody] ServerInfoUpdate update)
        {
            ResultBaseModel result = await _serverService.UpdateServerNameDescriptionAsync(serverId, update);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPut("role/{serverId}")]
        public async Task<IActionResult> UpdateRole(string serverId, [FromBody] RoleUpdateRequest request)
        {
            ResultBaseModel result = await _serverService.UpdateRoleAsync(serverId, request.Role, request.Linking);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("role/{serverId}")]
        public async Task<IActionResult> AddRole(string serverId, [FromBody] ServerRole role)
        {
            ResultBaseModel result = await _serverService.AddRoleAsync(serverId, role);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}