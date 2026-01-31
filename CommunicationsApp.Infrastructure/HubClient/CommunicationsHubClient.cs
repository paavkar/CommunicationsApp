using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using static CommunicationsApp.Core.Models.Enums;

namespace CommunicationsApp.Infrastructure.HubClient
{
    public class CommunicationsHubClient : ICommunicationsHubClient
    {
        private readonly NavigationManager _navigationManager;
        private readonly ILogger<CommunicationsHubClient> _logger;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        public HubConnection HubConnection { get; private set; }
        public event Action<string, string, ChatMessage>? ChannelMessageReceived;
        public event Action<string, ServerUpdateType, ServerProfile>? MemberUpdateReceived;
        public event Action<string, ServerUpdateType, ChannelClass>? ChannelClassUpdateReceived;
        public event Action<string, ServerUpdateType, Channel>? ChannelUpdateReceived;
        public event Action<string, ServerUpdateType, ServerInfoUpdate>? ServerInfoUpdateReceived;
        public event Action<string, ServerUpdateType, ServerRole>? ServerRoleUpdateReceived;
        public event Action<string, ServerRole, RoleMemberLinking>? ServerRoleMembersUpdateReceived;
        public event Action<string, string>? DataReady;

        public CommunicationsHubClient(
            NavigationManager navigationManager,
            ILogger<CommunicationsHubClient> logger)
        {
            _navigationManager = navigationManager;
            _logger = logger;

            HubConnection = new HubConnectionBuilder()
                  .WithUrl(
                    _navigationManager.ToAbsoluteUri("/chathub"),
                    opts =>
                    {
                        opts.Transports = HttpTransportType.WebSockets;
                        opts.SkipNegotiation = true;
                    })
                  .WithAutomaticReconnect()
                  .Build();

            RegisterHandlers();
            RegisterLifecycleHandlers();
        }

        private void RegisterHandlers()
        {
            HubConnection.On<string, string, ChatMessage>(
                "SendMessageToChannel",
                (channelId, serverId, message) =>
                {
                    ChannelMessageReceived?.Invoke(channelId, serverId, message);
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

            HubConnection.On<string, ServerUpdateType, ServerInfoUpdate>(
                "ServerInfoUpdate",
                (serverId, updateType, update) =>
                {
                    ServerInfoUpdateReceived?.Invoke(serverId, updateType, update);
                });

            HubConnection.On<string, ServerUpdateType, ServerRole>(
                "ServerRoleUpdate",
                (serverId, updateType, role) =>
                {
                    ServerRoleUpdateReceived?.Invoke(serverId, updateType, role);
                });

            HubConnection.On<string, ServerRole, RoleMemberLinking>(
                "ServerRoleMembersUpdate",
                (serverId, role, linking) =>
                {
                    ServerRoleMembersUpdateReceived?.Invoke(serverId, role, linking);
                });
        }

        private void RegisterLifecycleHandlers()
        {
            HubConnection.Reconnecting += error =>
            {
                _logger.LogWarning(error, "SignalR reconnecting...");
                return Task.CompletedTask;
            };

            HubConnection.Reconnected += connectionId =>
            {
                _logger.LogInformation("SignalR reconnected. ConnectionId: {ConnectionId}", connectionId);
                return Task.CompletedTask;
            };

            HubConnection.Closed += async error =>
            {
                _logger.LogWarning(error, "SignalR connection closed.");
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    await ConnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restart SignalR connection after close.");
                }
            };
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            await _connectionLock.WaitAsync(cancellationToken);
            try
            {
                if (HubConnection.State != HubConnectionState.Connected)
                {
                    await HubConnection.StartAsync(cancellationToken);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Communications Hub.");
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (HubConnection.State == HubConnectionState.Connected)
                {
                    await HubConnection.StopAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Communications Hub.");
            }
        }

        public async Task JoinBroadcastChannelAsync(string groupName)
        {
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return;
            }
            await HubConnection.InvokeAsync("JoinBroadcastChannel", groupName);
        }

        public async Task LeaveBroadcastChannelAsync(string groupName)
        {
            if (HubConnection.State != HubConnectionState.Connected)
            {
                return;
            }
            await HubConnection.InvokeAsync("LeaveBroadcastChannel", groupName);
        }
    }
}