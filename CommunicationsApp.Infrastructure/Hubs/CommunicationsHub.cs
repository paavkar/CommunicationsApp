using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Core.Models;
using Microsoft.AspNetCore.SignalR;
using static CommunicationsApp.Core.Models.Enums;

namespace CommunicationsApp.Infrastructure.Hubs
{
    public class CommunicationsHub : Hub
    {
        public Task JoinBroadcastChannel(string channelId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, channelId);
        }

        public Task LeaveBroadcastChannel(string channelId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);

        public Task SendMessageToChannel(string serverId, string channelId, ChatMessage message)
        {
            return Clients.Group(channelId)
                      .SendAsync("ReceiveChannelMessage",
                                 serverId, channelId, message);
        }

        public Task NotifyDataReady(string contextId, string dataType)
        {
            return Clients.Group(contextId)
                          .SendAsync("DataReady", contextId, dataType);
        }

        public Task NotifyMemberUpdate(string serverId, ServerUpdateType updateType, ServerProfile member)
        {
            return Clients.Group(serverId)
                          .SendAsync("MemberUpdate", serverId, updateType, member);
        }

        public Task NotifyChannelClassUpdate(string serverId, ServerUpdateType updateType, ChannelClass cc)
        {
            return Clients.Group(serverId)
                          .SendAsync("ChannelClassUpdate", serverId, updateType, cc);
        }

        public Task NotifyChannelUpdate(string serverId, ServerUpdateType updateType, Channel c)
        {
            return Clients.Group(serverId)
                          .SendAsync("ChannelUpdate", serverId, updateType, c);
        }

        public Task NotifyServerInfoUpdate(string serverId, ServerUpdateType updateType, ServerInfoUpdate update)
        {
            return Clients.Group(serverId)
                           .SendAsync("ServerInfoUpdate", serverId, updateType, update);
        }

        public Task NotifyServerRoleUpdate(string serverId, ServerUpdateType updateType, ServerRole role)
        {
            return Clients.Group(serverId)
                           .SendAsync("ServerRoleUpdate", serverId, updateType, role);
        }

        public Task NotifyServerRoleMembersUpdate(string serverId, ServerRole role, List<ServerProfile> members)
        {
            return Clients.Group(serverId)
                           .SendAsync("ServerRoleMembersUpdate", serverId, role, members);
        }
    }
}