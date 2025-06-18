using CommunicationsApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace CommunicationsApp.Services
{
    public class ChatHubService : IAsyncDisposable
    {
        private readonly NavigationManager NavigationManager;
        public HubConnection HubConnection { get; private set; }
        public event Action<string, string, ChatMessage>? ChannelMessageReceived;

        public ChatHubService(NavigationManager navigationManager)
        {
            NavigationManager = navigationManager;
        }

        private void EnsureHubConnection()
        {
            if (HubConnection == null)
            {
                HubConnection = new HubConnectionBuilder()
                  .WithUrl(
                    NavigationManager.ToAbsoluteUri("/chathub"),
                    opts => {
                        opts.Transports = HttpTransportType.WebSockets;
                        opts.SkipNegotiation = true;
                    })
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

        public async Task<dynamic> SendMessageAsync(string serverId, string channelId, ChatMessage message)
        {
            EnsureHubConnection();
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return null!;
            }
            try
            {
                await HubConnection.InvokeAsync("SendMessageToChannel", serverId, channelId, message);
                return new { Succeeded = true };
            }
            catch (HubException hubEx)
            {
                Console.Error.WriteLine($"SignalR hub error: {hubEx.Message}");
                return new { Succeeded = false, ErrorMessage = hubEx.Message };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"General SignalR error: {ex.Message}");
                return new { Succeeded = false, ErrorMessage = ex.Message };
            }
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