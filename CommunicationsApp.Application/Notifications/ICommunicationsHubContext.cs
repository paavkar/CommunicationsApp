using CommunicationsApp.Application.ResultModels;

namespace CommunicationsApp.Application.Notifications
{
    public interface ICommunicationsHubContext
    {
        Task<ResultBaseModel> SendToGroupAsync(string groupName, string methodName, object? arg, object? arg2,
            CancellationToken cancellationToken = default);
    }
}
