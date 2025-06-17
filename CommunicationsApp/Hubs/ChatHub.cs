using CommunicationsApp.Models;
using Microsoft.AspNetCore.SignalR;

namespace CommunicationsApp.Hubs
{
    public class ChatHub : Hub
    {
        public Task JoinChannel(string channelId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, channelId);

        public Task LeaveChannel(string channelId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);

        public Task SendMessageToChannel(string serverId, string channelId, ChatMessage message)
        {
            Clients.Group(channelId)
                      .SendAsync("ReceiveChannelMessage",
                                 serverId, channelId, message);
            return Task.CompletedTask;
        }

    }
}