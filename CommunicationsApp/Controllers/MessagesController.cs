using Asp.Versioning;
using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.CosmosDb;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationsApp.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MessagesController : ControllerBase
    {
        private readonly ICosmosDbService _cosmosDbService;

        public MessagesController(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        [HttpGet("server/{serverId}")]
        public async Task<IActionResult> GetServerMessages(string serverId)
        {
            var result = await _cosmosDbService.GetServerMessagesAsync(serverId);
            if (!result.Succeeded)
            {
                return NotFound(result);
            }
            return Ok(result.Messages);
        }

        [HttpPost]
        public async Task<IActionResult> SaveMessage([FromBody] ChatMessage message)
        {
            var result = await _cosmosDbService.SaveMessageAsync(message);
            if (!result.Succeeded)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
