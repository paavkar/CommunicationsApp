using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.CosmosDb;
using CommunicationsApp.SharedKernel.Localization;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Transactions;
using static CommunicationsApp.Core.Models.Enums;

namespace CommunicationsApp.Infrastructure.Services
{
    public class ServerService(
        IConfiguration configuration,
        HybridCache cache,
        ICosmosDbService cosmosDbService,
        IStringLocalizer<CommunicationsAppLoc> localizer,
        ILogger<ServerService> logger,
        IServerRepository serverRepository) : IServerService
    {
        public async Task<Server> CreateServerAsync(Server server, ApplicationUser user)
        {
            server.CreatedAt = DateTimeOffset.UtcNow;

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var rowsAffected = await serverRepository.InsertServerAsync(server);
            if (rowsAffected == 0)
            {
                return null!;
            }
            ServerProfile serverProfile = new()
            {
                Id = Guid.CreateVersion7().ToString(),
                UserId = user.Id,
                UserName = user.UserName,
                ServerId = server.Id,
                DisplayName = user.DisplayName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                BannerUrl = user.BannerUrl,
                CreatedAt = user.CreatedAt,
                JoinedAt = DateTimeOffset.UtcNow,
                Status = user.Status,
                Bio = user.Bio
            };
            rowsAffected = await serverRepository.InsertServerProfileAsync(serverProfile);
            if (rowsAffected == 0)
            {
                return null!;
            }
            ServerRole serverRole = new()
            {
                Id = Guid.CreateVersion7().ToString(),
                Name = "@everyone",
                ServerId = server.Id,
                HexColour = "",
                Hierarchy = 1
            };
            rowsAffected = await serverRepository.InsertServerRoleAsync(serverRole);
            if (rowsAffected == 0)
            {
                return null!;
            }
            var serverPermissions = await GetServerPermissionsAsync();
            List<ServerPermission> defaultPermissions = [];

            foreach (var permission in serverPermissions)
            {
                switch (permission.PermissionType)
                {
                    case ServerPermissionType.DisplayChannels:
                        defaultPermissions.Add(permission);
                        break;
                    case ServerPermissionType.ChangeNickname:
                        defaultPermissions.Add(permission);
                        break;
                    case ServerPermissionType.SendMessages:
                        defaultPermissions.Add(permission);
                        break;
                    case ServerPermissionType.SendMessagesToThreads:
                        defaultPermissions.Add(permission);
                        break;
                    case ServerPermissionType.CreatePublicThreads:
                        defaultPermissions.Add(permission);
                        break;
                    case ServerPermissionType.CreatePrivateThreads:
                        defaultPermissions.Add(permission);
                        break;
                    case ServerPermissionType.EmbedLinks:
                        defaultPermissions.Add(permission);
                        break;
                    case ServerPermissionType.AttachFiles:
                        defaultPermissions.Add(permission);
                        break;
                    case ServerPermissionType.AddReactions:
                        defaultPermissions.Add(permission);
                        break;
                    case ServerPermissionType.ReadMessageHistory:
                        defaultPermissions.Add(permission);
                        break;
                    default:
                        break;
                }
            }

            foreach (var permission in defaultPermissions)
            {
                rowsAffected = await serverRepository.UpsertServerRolePermissionsAsync(serverRole.Id, [permission.Id]);
            }
            serverProfile.Roles ??= [];
            serverProfile.Roles.Add(serverRole);

            server.Members ??= [];
            server.Members.Add(serverProfile);
            ChannelClass channelClass = new()
            {
                Id = Guid.CreateVersion7().ToString(),
                Name = localizer["TextChannels"],
                ServerId = server.Id!,
                OrderNumber = 1,
                IsPrivate = false
            };

            server.ChannelClasses ??= [];
            server.ChannelClasses.Add(channelClass);

            rowsAffected = await serverRepository.InsertChannelClassAsync(channelClass);
            if (rowsAffected > 0)
            {
                Channel defaultChannel = new()
                {
                    Id = Guid.CreateVersion7().ToString(),
                    Name = localizer["General"].ToString().ToLower(),
                    ServerId = server.Id!,
                    ChannelClassId = channelClass.Id,
                    Description = localizer["GeneralDescription"],
                    IsPrivate = false,
                    OrderNumber = 1
                };
                channelClass.Channels ??= [];
                channelClass.Channels.Add(defaultChannel);

                rowsAffected = await serverRepository.InsertChannelAsync(defaultChannel);
                if (rowsAffected == 0)
                {
                    return null!;
                }

                scope.Complete();
                return server;
            }

            return null!;
        }

        public async Task<Server?> GetServerByIdAsync(string serverId, string userId = "")
        {
            var cachedServer = await cache.GetOrCreateAsync<Server>(
                $"server_{serverId}",
                factory: async entry =>
                {
                    return await GetServerFromDatabaseAsync(serverId);
                }
            );
            if (cachedServer is null)
                return null!;
            return string.IsNullOrWhiteSpace(userId)
                ? cachedServer
                : cachedServer.Members.Any(m => m.UserId == userId)
                    ? cachedServer
                    : null;
        }

        public async Task<Server?> GetServerFromDatabaseAsync(string serverId)
        {
            var server = await serverRepository.GetServerByIdAsync(serverId);
            if (server != null)
            {
                server.ChannelClasses = [.. server.ChannelClasses.OrderBy(cc => cc.OrderNumber)];
                foreach (var channelClass in server.ChannelClasses)
                {
                    channelClass.Channels = [.. channelClass.Channels.OrderBy(c => c.OrderNumber)];
                }

                await GetMessagesAsync(server, serverId!);
            }

            return server ?? null;
        }

        public async Task<ServerResult> GetServerByInvitationAsync(string invitationCode)
        {
            var server = await serverRepository.GetServerByInvitationAsync(invitationCode);
            return server is null
                ? new ServerResult { Succeeded = false, ErrorMessage = "There is no server associated with given invitation." }
                : new ServerResult { Succeeded = true, Server = server };
        }

        public async Task<ServerResult> JoinServerAsync(Server server, ServerProfile profile)
        {
            var rowsAffected = await serverRepository.InsertServerProfileAsync(profile);
            if (rowsAffected <= 0)
            {
                return new ServerResult { Succeeded = false, ErrorMessage = localizer["ServerJoinError"] };
            }

            server = await serverRepository.LoadServerProfilesAsync(server);
            if (server is null)
            {
                return new ServerResult { Succeeded = false, ErrorMessage = localizer["FetchServerError"] };
            }

            await GetMessagesAsync(server, server.Id!);
            await UpdateCacheAsync(server.Id, server);

            return new ServerResult { Succeeded = true, Server = server };
        }

        public async Task GetMessagesAsync(Server server, string serverId)
        {
            var messagesResponse = await cosmosDbService.GetServerMessagesAsync(server.Id);
            if (messagesResponse.Succeeded)
            {
                var serverMessages = messagesResponse.Messages as List<ChatMessage>;
                foreach (var message in serverMessages)
                {
                    var channel = server.ChannelClasses
                        .SelectMany(cc => cc.Channels)
                        .FirstOrDefault(c => c.Id == message.Channel.Id);
                    if (channel != null)
                    {
                        channel.Messages ??= [];
                        channel.Messages.Add(message);
                    }
                }
            }
        }

        public async Task UpdateCacheAsync(string serverId, Server server)
        {
            if (server == null || string.IsNullOrWhiteSpace(serverId))
            {
                return;
            }
            await cache.SetAsync($"server_{serverId}", server);
        }

        public async Task<ServerResult> LeaveServerAsync(string serverId, string userId)
        {
            var rowsAffected = await serverRepository.DeleteServerProfileAsync(serverId, userId);
            if (rowsAffected > 0)
            {
                var server = await GetServerFromDatabaseAsync(serverId);
                server.Members.RemoveAll(m => m.UserId == userId);
                await UpdateCacheAsync(serverId, server);
                return new ServerResult { Succeeded = true };
            }
            else
            {
                return new ServerResult { Succeeded = false, ErrorMessage = "Failed to leave server." };
            }
        }

        public async Task<ServerResult> KickMembersAsync(string serverId, List<string> userIds)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var rowsAffected = await serverRepository.DeleteServerProfilesAsync(serverId, userIds);
            if (rowsAffected > 0)
            {
                scope.Complete();
                var server = await GetServerFromDatabaseAsync(serverId);
                server.Members.RemoveAll(m => userIds.Contains(m.UserId));
                await UpdateCacheAsync(serverId, server);
                return new ServerResult { Succeeded = true };
            }
            else
            {
                return new ServerResult { Succeeded = false, ErrorMessage = "Failed to leave server." };
            }
        }

        public async Task<ChannelClassResult> AddChannelClassAsync(ChannelClass channelClass)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var rowsAffected = await serverRepository.InsertChannelClassAsync(channelClass);
            if (rowsAffected > 0)
            {
                scope.Complete();
                var server = await GetServerByIdAsync(channelClass.ServerId);
                server.ChannelClasses.Add(channelClass);
                await UpdateCacheAsync(server.Id!, server);
                return new ChannelClassResult { Succeeded = true, ChannelClass = channelClass };
            }
            else
            {
                return new ChannelClassResult { Succeeded = false, ErrorMessage = "Failed to add channel class." };
            }
        }

        public async Task<ChannelResult> AddChannelAsync(string channelClassId, Channel channel)
        {
            var server = await GetServerByIdAsync(channel.ServerId);
            var channelClass = server.ChannelClasses.FirstOrDefault(cc => cc.Id == channelClassId);
            if (channelClass == null)
            {
                return new ChannelResult { Succeeded = false, ErrorMessage = "Channel class not found." };
            }
            channelClass.Channels.Add(channel);
            var rowsAffected = await serverRepository.InsertChannelAsync(channel);
            if (rowsAffected > 0)
            {
                await UpdateCacheAsync(server.Id!, server);
                return new ChannelResult { Succeeded = true, Channel = channel };
            }
            else
            {
                return new ChannelResult { Succeeded = false, ErrorMessage = "Failed to add channel." };
            }
        }

        public async Task<ServerPermissionResult> AddServerPermissionsAsync()
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            foreach (Enums.ServerPermissionType permission in Enum.GetValues<Enums.ServerPermissionType>())
            {
                ServerPermission p = new()
                {
                    Id = Guid.CreateVersion7().ToString(),
                    PermissionType = permission,
                    PermissionName = permission.ToString()
                };
                await serverRepository.UpdateServerPermissionAsync(p);
                await serverRepository.UpsertServerPermissionAsync(p);
            }
            scope.Complete();
            return new ServerPermissionResult { Succeeded = true };
        }

        public async Task<List<ServerPermission>> GetServerPermissionsAsync()
        {
            var serverPermissions = await serverRepository.GetAllPermissionsAsync();
            return [.. serverPermissions];
        }

        public async Task<ResultBaseModel> UpdateServerNameDescriptionAsync(string serverId, ServerInfoUpdate update)
        {
            var rowsAffected = await serverRepository.UpdateServerInfoAsync(serverId, update);
            if (rowsAffected > 0)
            {
                var server = await GetServerByIdAsync(serverId);
                if (server != null)
                {
                    server.Name = update.Name;
                    server.Description = update.Description;
                    await UpdateCacheAsync(serverId, server);
                }
                return new ResultBaseModel { Succeeded = true };
            }
            else
            {
                return new ResultBaseModel
                {
                    Succeeded = false,
                    ErrorMessage = localizer["ServerInfoUpdateError"]
                };
            }
        }

        public async Task<ResultBaseModel> UpdateRoleAsync(string serverId, ServerRole role, RoleMemberLinking linking)
        {
            var permissionIds = role.Permissions.Select(p => p.Id);
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                await serverRepository.UpdateServerRoleAsync(role);
                await serverRepository.UpsertServerRolePermissionsAsync(role.Id, permissionIds);
                await serverRepository.DeleteServerRolePermissionsNotInAsync(role.Id, permissionIds);

                foreach (var member in linking.NewMembers)
                {
                    await serverRepository.InsertUserServerRoleAsync(member.UserId, serverId, role.Id);
                }

                foreach (var member in linking.RemovedMembers)
                {
                    await serverRepository.DeleteUserServerRoleAsync(member.UserId, serverId, role.Id);
                }
                scope.Complete();

                var server = await GetServerByIdAsync(serverId);
                if (server != null)
                {
                    var existingRole = server.Roles.FirstOrDefault(r => r.Id == role.Id);
                    if (existingRole != null)
                    {
                        existingRole.Name = role.Name;
                        existingRole.HexColour = role.HexColour;
                        existingRole.Hierarchy = role.Hierarchy;
                        existingRole.DisplaySeparately = role.DisplaySeparately;
                        existingRole.Permissions = [.. role.Permissions];
                    }
                    var userServerRoles = server.Members
                        .SelectMany(m => m.Roles)
                        .Where(r => r.Id == role.Id)
                        .ToList();
                    foreach (var userServerRole in userServerRoles)
                    {
                        userServerRole.Name = role.Name;
                        userServerRole.HexColour = role.HexColour;
                        userServerRole.Hierarchy = role.Hierarchy;
                        userServerRole.DisplaySeparately = role.DisplaySeparately;
                        userServerRole.Permissions = [.. role.Permissions];
                    }

                    var addedMemberIds = linking.NewMembers.Select(m => m.UserId).ToList();
                    var removedMemberIds = linking.RemovedMembers.Select(m => m.UserId).ToList();
                    var membersToUpdate = server.Members.Where(m => addedMemberIds.Contains(m.UserId)).ToList();
                    membersToUpdate.AddRange(server.Members.Where(m => removedMemberIds.Contains(m.UserId)));
                    foreach (var member in membersToUpdate)
                    {
                        if (addedMemberIds.Contains(member.UserId) && member.Roles.All(r => r.Id != role.Id))
                        {
                            member.Roles.Add(role);
                        }
                        if (removedMemberIds.Contains(member.UserId))
                        {
                            member.Roles.RemoveAll(r => r.Id == role.Id);
                        }
                        member.Roles = [.. member.Roles.OrderBy(r => r.Hierarchy)];
                    }

                    await UpdateCacheAsync(serverId, server);
                }
                return new ResultBaseModel { Succeeded = true };
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error updating the role in {Method}", nameof(UpdateRoleAsync));
                return new ResultBaseModel { Succeeded = false, ErrorMessage = e.Message };
            }
        }

        public async Task<ResultBaseModel> AddRoleAsync(string serverId, ServerRole role)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var rowsAffected = await serverRepository.InsertServerRoleAsync(role);

            if (rowsAffected > 0)
            {
                var server = await GetServerByIdAsync(serverId);
                if (server != null)
                {
                    server.Roles.Add(role);
                    var everyoneRole = server.Roles.FirstOrDefault(r => r.Name == "@everyone");
                    everyoneRole.Hierarchy = server.Roles.Count;

                    rowsAffected = await serverRepository.UpdateServerRoleHierarchyAsync(role);
                    if (rowsAffected == 0)
                    {
                        return new ResultBaseModel { Succeeded = false, ErrorMessage = localizer["AddRoleError"] };
                    }
                    scope.Complete();
                    await UpdateCacheAsync(serverId, server);
                }
                return new ResultBaseModel { Succeeded = true };
            }
            else
            {
                return new ResultBaseModel { Succeeded = false, ErrorMessage = localizer["AddRoleError"] };
            }
        }
    }
}
