using Microsoft.AspNetCore.SignalR;

namespace CommunicationsApp.Hubs
{
    public class CommunicationsHub : Hub
    {
        public Task JoinBroadcastChannel(string groupName)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public Task LeaveBroadcastChannel(string groupName) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}