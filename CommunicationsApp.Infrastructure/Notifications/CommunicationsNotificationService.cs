using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Application.Notifications;
using CommunicationsApp.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using static CommunicationsApp.Core.Models.Enums;

namespace CommunicationsApp.Infrastructure.Notifications
{
    public class CommunicationsNotificationService(
        ICommunicationsHubContext hubContext,
        ILogger<CommunicationsNotificationService> logger) : ICommunicationsNotificationService
    {
        public async Task SendMessageAsync(string serverId, string channelId, ChatMessage message)
        {
            try
            {
                var messageSize = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(message));
                logger.LogInformation("Received message of size {messageSize} B.", messageSize);
                await hubContext.SendToGroupAsync(serverId, "SendMessageToChannel", channelId, message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message to channel {channelId} in server {serverId}.", channelId, serverId);
            }
        }

        public async Task NotifyDataReadyAsync(string contextId, string dataType)
        {
            try
            {

            }
            catch (Exception)
            {

            }
        }

        public async Task NotifyMemberUpdateAsync(string serverId, ServerUpdateType updateType,
            ServerProfile member)
        {
            try
            {
                await hubContext.SendToGroupAsync(serverId, "NotifyMemberUpdate", updateType, member);
            }
            catch (Exception)
            {

            }
        }

        public async Task NotifyChannelClassUpdateAsync(string serverId, ServerUpdateType updateType,
            ChannelClass cc)
        {
            try
            {
                await hubContext.SendToGroupAsync(serverId, "NotifyChannelClassUpdate", updateType, cc);
            }
            catch (Exception)
            {

            }
        }

        public async Task NotifyChannelUpdateAsync(string serverId, ServerUpdateType updateType,
            Channel c)
        {
            try
            {
                await hubContext.SendToGroupAsync(serverId, "NotifyChannelUpdate", updateType, c);
            }
            catch (Exception)
            {

            }
        }

        public async Task NotifyServerInfoUpdateAsync(
            string serverId, ServerUpdateType updateType, ServerInfoUpdate update)
        {
            try
            {
                await hubContext.SendToGroupAsync(serverId, "NotifyServerInfoUpdate", updateType, update);
            }
            catch (Exception)
            {

            }
        }

        public async Task NotifyServerRoleUpdateAsync(
            string serverId, ServerUpdateType updateType, ServerRole role)
        {
            try
            {
                await hubContext.SendToGroupAsync(serverId, "NotifyServerRoleUpdate", updateType, role);
            }
            catch (Exception)
            {

            }
        }

        public async Task NotifyServerRoleMembersUpdateAsync(
            string serverId, ServerRole role, RoleMemberLinking linking)
        {
            try
            {
                await hubContext.SendToGroupAsync(serverId, "NotifyServerRoleMembersUpdate", role, linking);
            }
            catch (Exception)
            {

            }
        }
    }
}
