namespace CommunicationsApp.Services
{
    public class UnreadService
    {
        private readonly Dictionary<string, int> _unreadCounts = [];

        public event Action? OnChange;

        public int GetUnreadCount(string channelId) => _unreadCounts.ContainsKey(channelId) ? _unreadCounts[channelId] : 0;

        public void IncrementUnreadCount(string channelId)
        {
            if (_unreadCounts.ContainsKey(channelId))
            {
                _unreadCounts[channelId]++;
            }
            else
            {
                _unreadCounts[channelId] = 1;
            }
            OnChange?.Invoke();
        }

        public void ClearUnreadCount(string channelId)
        {
            if (_unreadCounts.ContainsKey(channelId))
            {
                _unreadCounts[channelId] = 0;
                OnChange?.Invoke();
            }
        }
    }
}
