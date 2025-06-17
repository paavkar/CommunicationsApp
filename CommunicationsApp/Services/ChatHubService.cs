using CommunicationsApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace CommunicationsApp.Services
{
    public class ChatHubService : IAsyncDisposable
    {
        private readonly NavigationManager _navigationManager;
        public HubConnection HubConnection { get; private set; }
        public event Action<string, string, ChatMessage>? ChannelMessageReceived;

        public ChatHubService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        private void EnsureHubConnection()
        {
            // Only create the HubConnection if it hasn't been created already.
            if (HubConnection == null)
            {
                // This code now runs "just-in-time" when a component
                // actually needs the connection, at which point NavigationManager is ready.
                HubConnection = new HubConnectionBuilder()
                    .WithUrl(_navigationManager.ToAbsoluteUri("/chathub"))
                    .WithAutomaticReconnect()
                    .Build();

                HubConnection.On<string, string, ChatMessage>(
                    "ReceiveChannelMessage",
                    (serverId, channelId, message) =>
                    {
                        ChannelMessageReceived?.Invoke(serverId, channelId, message);
                    });
            }
        }

        public async Task StartAsync()
        {
            EnsureHubConnection();

            if (HubConnection.State == HubConnectionState.Disconnected)
            {
                await HubConnection.StartAsync();
            }
        }

        public async Task JoinChannelAsync(string channelId)
        {
            EnsureHubConnection();
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return;
            }
            await HubConnection.SendAsync("JoinChannel", channelId);
        }

        public async Task SendMessageAsync(string serverId, string channelId, ChatMessage message)
        {
            EnsureHubConnection();
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return;
            }
            await HubConnection.SendAsync("SendMessageToChannel", serverId, channelId, message);
        }

        public async ValueTask DisposeAsync()
        {
            if (HubConnection != null)
            {
                await HubConnection.DisposeAsync();
            }
        }

    }
}