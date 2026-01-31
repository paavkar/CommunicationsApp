using CommunicationsApp.Application.Notifications;
using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CommunicationsApp.Notifications
{
    public class CommunicationsHubContextWrapper(
        IHubContext<CommunicationsHub> hubContext,
        ILogger<CommunicationsHubContextWrapper> logger) : ICommunicationsHubContext
    {
        public async Task<ResultBaseModel> SendToGroupAsync(string groupName, string methodName, object? arg, object? arg2,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await hubContext.Clients.Group(groupName)
                    .SendAsync(methodName, groupName, arg, arg2, cancellationToken);

                return new ResultBaseModel { Succeeded = true };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send message to group {GroupName} via method {Method}.", groupName, methodName);
                return new ResultBaseModel
                {
                    Succeeded = false,
                    ErrorMessage = $"An error occurred while sending message to group {groupName} via method {methodName}: {ex.Message}"
                };
            }
        }
    }
}
