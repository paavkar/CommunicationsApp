using CommunicationsApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text;
using System.Text.Json;

namespace CommunicationsApp.Services
{
    public class ChatHubService(
        NavigationManager navigationManager,
        ILogger<ChatHubService> logger) : IAsyncDisposable
    {
        public HubConnection HubConnection { get; private set; }
        public event Action<string, string, ChatMessage>? ChannelMessageReceived;

        private void EnsureHubConnection()
        {
            if (HubConnection == null)
            {
                HubConnection = new HubConnectionBuilder()
                  .WithUrl(
                    navigationManager.ToAbsoluteUri("/chathub"),
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
                var messageSize = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(message));
                logger.LogInformation("Received message of size {messageSize} B.", messageSize);
                await HubConnection.InvokeAsync("SendMessageToChannel", serverId, channelId, message);
                return new { Succeeded = true };
            }
            catch (HubException hubEx)
            {
                logger.LogError(hubEx, "SignalR hub error");
                return new { Succeeded = false, ErrorMessage = hubEx.Message };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "General error while sending message");
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