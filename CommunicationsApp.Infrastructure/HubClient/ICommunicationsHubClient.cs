using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;
using static CommunicationsApp.Core.Models.Enums;

namespace CommunicationsApp.Infrastructure.HubClient
{
    public interface ICommunicationsHubClient
    {
        public HubConnection HubConnection { get; }
        public event Action<string, string, ChatMessage>? ChannelMessageReceived;
        public event Action<string, ServerUpdateType, ServerProfile>? MemberUpdateReceived;
        public event Action<string, ServerUpdateType, ChannelClass>? ChannelClassUpdateReceived;
        public event Action<string, ServerUpdateType, Channel>? ChannelUpdateReceived;
        public event Action<string, ServerUpdateType, ServerInfoUpdate>? ServerInfoUpdateReceived;
        public event Action<string, ServerUpdateType, ServerRole>? ServerRoleUpdateReceived;
        public event Action<string, ServerRole, RoleMemberLinking>? ServerRoleMembersUpdateReceived;
        public event Action<string, string>? DataReady;

        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        Task JoinBroadcastChannelAsync(string channelId);
        Task LeaveBroadcastChannelAsync(string channelId);
    }
}
