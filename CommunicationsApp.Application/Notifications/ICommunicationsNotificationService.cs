using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Models;
using static CommunicationsApp.Core.Models.Enums;

namespace CommunicationsApp.Application.Notifications
{
    public interface ICommunicationsNotificationService
    {
        Task<MessageResult> SendMessageAsync(string serverId, string channelId, ChatMessage message);
        Task NotifyDataReadyAsync(string contextId, string dataType);
        Task NotifyMemberUpdateAsync(string serverId, ServerUpdateType updateType, ServerProfile member);
        Task NotifyChannelClassUpdateAsync(string serverId, ServerUpdateType updateType, ChannelClass cc);
        Task NotifyChannelUpdateAsync(string serverId, ServerUpdateType updateType, Channel c);
        Task NotifyServerInfoUpdateAsync(
            string serverId, ServerUpdateType updateType, ServerInfoUpdate update);
        Task NotifyServerRoleUpdateAsync(
            string serverId, ServerUpdateType updateType, ServerRole role);
        Task NotifyServerRoleMembersUpdateAsync(
            string serverId, ServerRole role, RoleMemberLinking linking);
    }
}
