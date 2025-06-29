﻿using CommunicationsApp.Data;
using CommunicationsApp.Interfaces;
using CommunicationsApp.Models;
using Dapper;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Hybrid;

namespace CommunicationsApp.Services
{
    public class ServerService(IConfiguration configuration, HybridCache cache) : IServerService
    {
        private SqlConnection GetConnection()
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            return new SqlConnection(connectionString);
        }

        public async Task<Server> CreateServerAsync(Server server, ApplicationUser user)
        {
            server.CreatedAt = DateTimeOffset.UtcNow;
            var insertServerQuery = """
                INSERT INTO Servers (Id, Name, InvitationCode, CustomInvitationCode, Description, OwnerId, CreatedAt, IconUrl, BannerUrl, ServerType)
                VALUES (@Id, @Name, @InvitationCode, @CustomInvitationCode, @Description, @OwnerId, @CreatedAt, @IconUrl, @BannerUrl, @ServerType)
                """;
            using var connection = GetConnection();
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                var rowsAffected = await connection.ExecuteAsync(insertServerQuery, server, transaction);

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

                ServerRole serverRole = new()
                {
                    Id = Guid.CreateVersion7().ToString(),
                    Name = "owner",
                    ServerId = server.Id,
                    HexColour = "",
                    Hierarchy = 1
                };

                var insertServerRoleQuery = """
                    INSERT INTO ServerRoles (Id, Name, ServerId, HexColour, Hierarchy)
                    VALUES (@Id, @Name, @ServerId, @HexColour, @Hierarchy)
                    """;
                rowsAffected = await connection.ExecuteAsync(insertServerRoleQuery, serverRole, transaction);

                UserServerRole userServerRole = new()
                {
                    UserId = user.Id,
                    ServerId = server.Id,
                    RoleId = serverRole.Id
                };

                var insertUserServerRoleQuery = """
                    INSERT INTO UserServerRoles (UserId, ServerId, RoleId)
                    VALUES (@UserId, @ServerId, @RoleId)
                    """;
                rowsAffected = await connection.ExecuteAsync(insertUserServerRoleQuery, userServerRole, transaction);
                serverProfile.Roles ??= [];
                serverProfile.Roles.Add(serverRole);

                server.Members ??= [];
                server.Members.Add(serverProfile);

                var insertServerProfileQuery = """
                    INSERT INTO ServerProfiles (Id, UserId, UserName, ServerId, DisplayName, ProfilePictureUrl, BannerUrl, CreatedAt, JoinedAt, Status, Bio)
                    VALUES (@Id, @UserId, @UserName, @ServerId, @DisplayName, @ProfilePictureUrl, @BannerUrl, @CreatedAt, @JoinedAt, @Status, @Bio)
                    """;
                rowsAffected = await connection.ExecuteAsync(insertServerProfileQuery, serverProfile, transaction);

                ChannelClass channelClass = new()
                {
                    Id = Guid.CreateVersion7().ToString(),
                    Name = "text channels",
                    ServerId = server.Id!,
                    OrderNumber = 1,
                    IsPrivate = false
                };

                server.ChannelClasses ??= [];
                server.ChannelClasses.Add(channelClass);

                var channelClassQuery = """
                    INSERT INTO ChannelClasses (Id, Name, ServerId, IsPrivate, OrderNumber)
                    VALUES (@Id, @Name, @ServerId, @IsPrivate, @OrderNumber)
                    """;
                rowsAffected = await connection.ExecuteAsync(channelClassQuery, channelClass, transaction);

                if (rowsAffected > 0)
                {
                    Channel defaultChannel = new()
                    {
                        Id = Guid.CreateVersion7().ToString(),
                        Name = "general",
                        ServerId = server.Id!,
                        ChannelClassId = channelClass.Id,
                        Description = "General discussion channel",
                        IsPrivate = false,
                        OrderNumber = 1
                    };
                    channelClass.Channels ??= [];
                    channelClass.Channels.Add(defaultChannel);

                    var defaultChannelQuery = """
                        INSERT INTO Channels (Id, Name, ServerId, ChannelClassId, Description, IsPrivate, OrderNumber, CreatedAt)
                        VALUES (@Id, @Name, @ServerId, @ChannelClassId, @Description, @IsPrivate, @OrderNumber, @CreatedAt)
                        """;
                    rowsAffected = await connection.ExecuteAsync(defaultChannelQuery, defaultChannel, transaction);
                }

                transaction.Commit();
                return server;
            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine($"Error: {e.Message}");
                return null!;
            }
        }

        public async Task<Server?> GetServerByIdAsync(string serverId, string userId)
        {
            var cachedServer = await cache.GetOrCreateAsync<Server>(
                $"server_{serverId}",
                factory: async entry =>
                {
                    return await GetServerFromDatabaseAsync(serverId, userId);
                }
            );
            if (cachedServer is null)
            {
                await cache.RemoveAsync($"server_{serverId}");
                return null;
            }
            else
            {
                return cachedServer.Members.All(m => m.UserId != userId) ? null : cachedServer;
            }
        }

        public async Task<Server?> GetServerFromDatabaseAsync(string serverId, string userId)
        {
            var getServerQuery = """
                SELECT
                    s.*,
                    sr.Id AS ServerRoleId, sr.*,
                    usr.RoleId AS UserMemberRoleId, usr.*,
                    cc.Id AS ChannelClassId, cc.*,
                    c.Id AS ChannelId, c.*,
                    sp.Id AS ServerProfileId, sp.*
                FROM Servers s
                LEFT JOIN ServerRoles sr ON sr.ServerId = s.Id
                LEFT JOIN UserServerRoles usr ON usr.ServerId = s.Id AND usr.RoleId = sr.Id
                LEFT JOIN ChannelClasses cc ON cc.ServerId = s.Id
                LEFT JOIN Channels c ON c.ChannelClassId = cc.Id
                LEFT JOIN ServerProfiles sp ON sp.ServerId = s.Id
                WHERE s.Id = @serverId
                AND sp.UserId = @userId
                """;
            var server = await GetServerFromDatabaseAsync(getServerQuery, new { serverId, userId });
            if (server is null)
            {
                return null;
            }

            var serverProfileQuery = """
                SELECT sp.*, sr.Id AS ServerRoleId, sr.*
                FROM ServerProfiles sp
                LEFT JOIN UserServerRoles usr ON usr.ServerId = sp.ServerId AND usr.UserId = sp.UserId
                LEFT JOIN ServerRoles sr ON sr.Id = usr.RoleId
                WHERE sp.ServerId = @serverId
                """;

            server = await GetServerWithMembersAsync(serverProfileQuery, new { serverId = server.Id }, server);

            return server ?? null;
        }

        public async Task<dynamic> JoinServerByInvitationCode(string code, ApplicationUser user)
        {
            var serverDictionary = new Dictionary<string, Server>();
            var getServerQuery = """
                SELECT
                    s.*,
                    sr.Id AS ServerRoleId, sr.*,
                    usr.RoleId AS UserMemberRoleId, usr.*,
                    cc.Id AS ChannelClassId, cc.*,
                    c.Id AS ChannelId, c.*,
                    sp.Id AS ServerProfileId, sp.*
                FROM Servers s
                LEFT JOIN ServerRoles sr ON sr.ServerId = s.Id
                LEFT JOIN UserServerRoles usr ON usr.ServerId = s.Id AND usr.RoleId = sr.Id
                LEFT JOIN ChannelClasses cc ON cc.ServerId = s.Id
                LEFT JOIN Channels c ON c.ChannelClassId = cc.Id
                LEFT JOIN ServerProfiles sp ON sp.ServerId = s.Id
                WHERE s.InvitationCode = @code
                OR s.CustomInvitationCode = @code
                """;
            var server = await GetServerFromDatabaseAsync(getServerQuery, new { code });
            
            if (server is null)
            {
                return new { Succeeded = false, ErrorMessage = "There is no server associated with given invitation." };
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

            var insertServerProfileQuery = """
                INSERT INTO ServerProfiles (Id, UserId, UserName, ServerId, DisplayName, ProfilePictureUrl, BannerUrl, CreatedAt, JoinedAt, Status, Bio)
                VALUES (@Id, @UserId, @UserName, @ServerId, @DisplayName, @ProfilePictureUrl, @BannerUrl, @CreatedAt, @JoinedAt, @Status, @Bio)
                """;
            using var connection = GetConnection();
            var rowsAffected = await connection.ExecuteAsync(insertServerProfileQuery, serverProfile);
            if (rowsAffected <= 0)
            {
                return new { Succeeded = false, ErrorMessage = "Failed to join server." };
            }

            var serverProfileQuery = """
                SELECT sp.*, sr.Id AS ServerRoleId, sr.*
                FROM ServerProfiles sp
                LEFT JOIN UserServerRoles usr ON usr.ServerId = sp.ServerId AND usr.UserId = sp.UserId
                LEFT JOIN ServerRoles sr ON sr.Id = usr.RoleId
                WHERE sp.ServerId = @serverId
                """;

            server = await GetServerWithMembersAsync(serverProfileQuery, new { serverId = server.Id }, server);

            await UpdateCacheAsync(server.Id, server);

            return new { Succeeded = true, Server = server, ServerProfile = serverProfile };
        }

        public async Task<Server?> GetServerFromDatabaseAsync(string sql, object queryParameters)
        {
            var serverDictionary = new Dictionary<string, Server>();
            using var connection = GetConnection();
            await connection.QueryAsync<Server, ServerRole, UserServerRole, ChannelClass, Channel, ServerProfile, Server>(
                sql,
                (server, role, userServerRole, channelClass, channel, member) =>
                {
                    if (!serverDictionary.TryGetValue(server.Id!, out var currentServer))
                    {
                        currentServer = server;

                        currentServer.Roles ??= [];
                        currentServer.ChannelClasses ??= [];
                        currentServer.Members ??= [];
                    }

                    if (role != null && !string.IsNullOrEmpty(role.Id) && currentServer.Roles.All(r => r.Id != role.Id))
                    {
                        currentServer.Roles.Add(role);
                    }

                    if (channelClass != null && !string.IsNullOrEmpty(channelClass.Id))
                    {
                        var existingChannelClass = currentServer.ChannelClasses.FirstOrDefault(cc => cc.Id == channelClass.Id);
                        if (existingChannelClass == null)
                        {
                            existingChannelClass = channelClass;
                            existingChannelClass.Channels ??= [];
                            currentServer.ChannelClasses.Add(existingChannelClass);
                        }
                        if (channel != null && !string.IsNullOrEmpty(channel.Id) &&
                            existingChannelClass.Channels.All(ch => ch.Id != channel.Id))
                        {
                            channel.Messages ??= [];
                            existingChannelClass.Channels.Add(channel);
                        }
                    }

                    if (member != null && !string.IsNullOrEmpty(member.Id))
                    {
                        member.Roles ??= [];
                        member = currentServer.Members.FirstOrDefault(m => m.Id == member.Id) ?? member;
                        if (userServerRole.RoleId != null)
                        {
                            var roleToAdd = currentServer.Roles.FirstOrDefault(r => r.Id == userServerRole.RoleId);
                            if (roleToAdd != null && !member.Roles.Any(r => r.Id == roleToAdd.Id))
                            {
                                member.Roles.Add(roleToAdd);
                            }
                        }
                        if (currentServer.Members.All(m => m.Id != member.Id))
                        {
                            currentServer.Members.Add(member);
                        }
                    }

                    if (!serverDictionary.ContainsKey(currentServer.Id))
                    {
                        serverDictionary.Add(currentServer.Id!, currentServer);
                    }
                    return currentServer;
                },
                queryParameters,
                splitOn: "ServerRoleId,UserMemberRoleId,ChannelClassId,ChannelId,ServerProfileId"
            );
            var serverWithAllData = serverDictionary.FirstOrDefault().Value;

            serverWithAllData.ChannelClasses = [.. serverWithAllData.ChannelClasses.OrderBy(cc => cc.OrderNumber)];
            foreach (var channelClass in serverWithAllData.ChannelClasses)
            {
                channelClass.Channels = [.. channelClass.Channels.OrderBy(c => c.OrderNumber)];
            }
            foreach (var member in serverWithAllData.Members)
            {
                member.Roles = [.. member.Roles.OrderBy(r => r.Hierarchy)];
            }

            return serverWithAllData ?? null;
        }

        public async Task<Server> GetServerWithMembersAsync(string sql, object queryParameters, Server server)
        {
            var memberDictionary = new Dictionary<string, ServerProfile>();

            using var connection = GetConnection();
            await connection.QueryAsync<ServerProfile, ServerRole, ServerProfile>(
                sql,
                (serverProfile, role) =>
                {
                    if (!memberDictionary.TryGetValue(serverProfile.Id!, out var member))
                    {
                        member = serverProfile;

                        member.Roles ??= [];
                        memberDictionary.Add(member.Id!, member);
                    }
                    if (role != null && !string.IsNullOrEmpty(role.Id) && member.Roles.All(r => r.Id != role.Id))
                    {
                        member.Roles.Add(role);
                    }
                    return member;
                },
                queryParameters,
                splitOn: "ServerRoleId"
            );
            foreach (var sp in memberDictionary)
            {
                var member = sp.Value;
                member.Roles = [.. member.Roles.OrderBy(r => r.Hierarchy)];
                if (server != null && server.Members != null)
                {
                    var existingProfile = server.Members.FirstOrDefault(x => x.Id == member.Id);
                    if (existingProfile == null)
                    {
                        server.Members.Add(member);
                    }
                }
            }

            return server;
        }

        public async Task UpdateCacheAsync(string serverId, Server server)
        {
            if (server == null || string.IsNullOrEmpty(serverId))
            {
                return;
            }
            await cache.SetAsync($"server_{serverId}", server);
        }

        public async Task<dynamic> LeaveServerAsync(string serverId, string userId)
        {
            var deleteServerProfileQuery = """
                DELETE FROM ServerProfiles
                WHERE ServerId = @serverId AND UserId = @userId
                """;
            using var connection = GetConnection();
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                var rowsAffected = await connection.ExecuteAsync(deleteServerProfileQuery, new { serverId, userId }, transaction);
                if (rowsAffected > 0)
                {
                    transaction.Commit();
                    return new { Succeeded = true };
                }
                else
                {
                    transaction.Rollback();
                    return new { Succeeded = false, ErrorMessage = "Failed to leave server." };
                }
            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine($"Error: {e.Message}");
                return new { Succeeded = false, ErrorMessage = e.Message };
            }
        }
    }
}
