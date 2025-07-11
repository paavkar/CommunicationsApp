using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.Services.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using static CommunicationsApp.Core.Models.Enums;

namespace CommunicationsApp.Infrastructure.Services
{
    public class CommunicationsHubService(
        NavigationManager navigationManager,
        ILogger<CommunicationsHubService> logger) : IAsyncDisposable
    {
        public HubConnection HubConnection { get; private set; }
        public event Action<string, string, ChatMessage>? ChannelMessageReceived;
        public event Action<string, ServerUpdateType, ServerProfile>? MemberUpdateReceived;
        public event Action<string, ServerUpdateType, ChannelClass>? ChannelClassUpdateReceived;
        public event Action<string, ServerUpdateType, Channel>? ChannelUpdateReceived;
        public event Action<string, string>? DataReady;

        private void EnsureHubConnection()
        {
            if (HubConnection == null)
            {
                HubConnection = new HubConnectionBuilder()
                  .WithUrl(
                    navigationManager.ToAbsoluteUri("/chathub"),
                    opts =>
                    {
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

                HubConnection.On<string, string>("DataReady", (contextId, dataType) =>
                {
                    DataReady?.Invoke(contextId, dataType);
                });

                HubConnection.On<string, ServerUpdateType, ServerProfile>(
                    "MemberUpdate",
                    (serverId, updateType, member) =>
                    {
                        MemberUpdateReceived?.Invoke(serverId, updateType, member);
                    });

                HubConnection.On<string, ServerUpdateType, ChannelClass>(
                    "ChannelClassUpdate",
                    (serverId, updateType, cc) =>
                    {
                        ChannelClassUpdateReceived?.Invoke(serverId, updateType, cc);
                    });

                HubConnection.On<string, ServerUpdateType, Channel>(
                    "ChannelUpdate",
                    (serverId, updateType, c) =>
                    {
                        ChannelUpdateReceived?.Invoke(serverId, updateType, c);
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

        public async Task JoinBroadcastChannelAsync(string channelId)
        {
            EnsureHubConnection();
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return;
            }
            await HubConnection.SendAsync("JoinBroadcastChannel", channelId);
        }

        public async Task LeaveBroadcastChannelAsync(string channelId)
        {
            EnsureHubConnection();
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return;
            }
            await HubConnection.SendAsync("LeaveBroadcastChannel", channelId);
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
                return new SignalRResult { Succeeded = true };
            }
            catch (HubException hubEx)
            {
                logger.LogError(hubEx, "SignalR hub error");
                return new SignalRResult { Succeeded = false, ErrorMessage = hubEx.Message };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "General error while sending message");
                return new SignalRResult { Succeeded = false, ErrorMessage = ex.Message };
            }
        }

        public async Task NotifyDataReadyAsync(string contextId, string dataType)
        {
            EnsureHubConnection();
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return;
            }
            await HubConnection.InvokeAsync("NotifyDataReady", contextId, dataType);
        }

        public async Task<dynamic> NotifyMemberUpdateAsync(string serverId, ServerUpdateType updateType, ServerProfile member)
        {
            EnsureHubConnection();
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return null!;
            }
            try
            {
                await HubConnection.InvokeAsync("NotifyMemberUpdate", serverId, updateType, member);
                return new SignalRResult { Succeeded = true };
            }
            catch (HubException hubEx)
            {
                logger.LogError(hubEx, "SignalR hub error");
                return new SignalRResult { Succeeded = false, ErrorMessage = hubEx.Message };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "General error while sending message");
                return new SignalRResult { Succeeded = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<dynamic> NotifyChannelClassUpdateAsync(string serverId, ServerUpdateType updateType, ChannelClass cc)
        {
            EnsureHubConnection();
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return null!;
            }
            try
            {
                await HubConnection.InvokeAsync("NotifyChannelClassUpdate", serverId, updateType, cc);
                return new SignalRResult { Succeeded = true };
            }
            catch (HubException hubEx)
            {
                logger.LogError(hubEx, "SignalR hub error");
                return new SignalRResult { Succeeded = false, ErrorMessage = hubEx.Message };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "General error while sending message");
                return new SignalRResult { Succeeded = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<dynamic> NotifyChannelUpdateAsync(string serverId, ServerUpdateType updateType, Channel c)
        {
            EnsureHubConnection();
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return null!;
            }
            try
            {
                await HubConnection.InvokeAsync("NotifyChannelUpdate", serverId, updateType, c);
                return new SignalRResult { Succeeded = true };
            }
            catch (HubException hubEx)
            {
                logger.LogError(hubEx, "SignalR hub error");
                return new SignalRResult { Succeeded = false, ErrorMessage = hubEx.Message };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "General error while sending message");
                return new SignalRResult { Succeeded = false, ErrorMessage = ex.Message };
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