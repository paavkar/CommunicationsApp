namespace CommunicationsApp.Application.Notifications
{
    public interface ICommunicationsHubContext
    {
        Task SendToGroupAsync(string groupName, string methodName, object? arg, object? arg2,
            CancellationToken cancellationToken = default);
    }
}
