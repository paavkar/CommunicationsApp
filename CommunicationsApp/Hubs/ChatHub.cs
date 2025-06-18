using CommunicationsApp.Models;
using Microsoft.AspNetCore.SignalR;
using System.Text;
using System.Text.Json;

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
            return Clients.Group(channelId)
                      .SendAsync("ReceiveChannelMessage",
                                 serverId, channelId, message);
        }
    }
}