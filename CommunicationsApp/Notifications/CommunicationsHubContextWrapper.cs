using CommunicationsApp.Application.Notifications;
using CommunicationsApp.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CommunicationsApp.Notifications
{
    public class CommunicationsHubContextWrapper(
        IHubContext<CommunicationsHub> hubContext,
        ILogger<CommunicationsHubContextWrapper> logger) : ICommunicationsHubContext
    {
        public async Task SendToGroupAsync(string groupName, string methodName, object? arg, object? arg2,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await hubContext.Clients.Group(groupName)
                    .SendAsync(methodName, arg, arg2, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send message to group {GroupName} via method {Method}.", groupName, methodName);
            }
        }
    }
}
