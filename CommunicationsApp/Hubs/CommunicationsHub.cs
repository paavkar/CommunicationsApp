﻿using CommunicationsApp.Models;
using Microsoft.AspNetCore.SignalR;
using static CommunicationsApp.Models.Enums;

namespace CommunicationsApp.Hubs
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
    }
}