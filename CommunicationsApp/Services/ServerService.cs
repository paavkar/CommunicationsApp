using CommunicationsApp.Interfaces;
using CommunicationsApp.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using CommunicationsApp.Data;
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
            var insertServerQuery = """
                INSERT INTO Servers (Id, Name, InvitationCode, Description, OwnerId, CreatedAt, IconUrl, BannerUrl, ServerType)
                VALUES (@Id, @Name, @InvitationCode, @Description, @OwnerId, @CreatedAt, @IconUrl, @BannerUrl, @ServerType)
                """;
            using var connection = GetConnection();
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                var rowsAffected = await connection.ExecuteAsync(insertServerQuery, server, transaction);


                var serverProfile = new
                {
                    Id = Guid.CreateVersion7().ToString(),
                    UserId = user.Id,
                    user.UserName,
                    ServerId = server.Id,
                    user.DisplayName,
                    user.ProfilePictureUrl,
                    user.BannerUrl,
                    user.CreatedAt,
                    JoinedAt = DateTimeOffset.UtcNow,
                    user.Status,
                    user.Bio
                };

                var insertServerProfileQuery = """
                    INSERT INTO ServerProfiles (Id, UserId, UserName, ServerId, DisplayName, ProfilePictureUrl, BannerUrl, CreatedAt, JoinedAt, Status, Bio)
                    VALUES (@Id, @UserId, @UserName, @ServerId, @DisplayName, @ProfilePictureUrl, @BannerUrl, @CreatedAt, @JoinedAt, @Status, @Bio)
                    """;
                rowsAffected = await connection.ExecuteAsync(insertServerProfileQuery, serverProfile, transaction);

                ChannelClass channelClass = new ChannelClass
                {
                    Id = Guid.CreateVersion7().ToString(),
                    Name = "text channels",
                    ServerId = server.Id!,
                    OrderNumber = 1,
                    IsPrivate = false
                };

                var channelClassQuery = """
                    INSERT INTO ChannelClasses (Id, Name, ServerId, IsPrivate, OrderNumber)
                    VALUES (@Id, @Name, @ServerId, @IsPrivate, @OrderNumber)
                    """;
                rowsAffected = await connection.ExecuteAsync(channelClassQuery, channelClass, transaction);

                if (rowsAffected > 0)
                {
                    var defaultChannel = new Channel
                    {
                        Id = Guid.CreateVersion7().ToString(),
                        Name = "general",
                        ServerId = server.Id!,
                        ChannelClassId = channelClass.Id,
                        Description = "General discussion channel",
                        IsPrivate = false,
                        OrderNumber = 1
                    };
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

            return cachedServer;
        }

        public async Task<Server?> GetServerFromDatabaseAsync(string serverId, string userId)
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
                WHERE s.Id = @serverId
                AND sp.UserId = @userId
                """;
            using var connection = GetConnection();
            await connection.QueryAsync<Server, ServerRole, UserServerRole, ChannelClass, Channel, ServerProfile, Server>(
                getServerQuery,
                (server, role, userServerRole, channelClass, channel, member) =>
                {
                    if (!serverDictionary.TryGetValue(server.Id!, out var currentServer))
                    {
                        currentServer = server;

                        currentServer.Roles ??= [];
                        currentServer.ChannelClasses ??= [];
                        currentServer.Members ??= [];
                        serverDictionary.Add(currentServer.Id!, currentServer);
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

                    if (member != null && !string.IsNullOrEmpty(member.Id) &&
                        currentServer.Members.All(m => m.Id != member.Id))
                    {
                        member.Roles ??= [];
                        if (member.Id == userServerRole.UserId && userServerRole.RoleId != null)
                        {
                            var roleToAdd = currentServer.Roles.FirstOrDefault(r => r.Id == userServerRole.RoleId);
                            if (roleToAdd != null && !member.Roles.Any(r => r.Id == roleToAdd.Id))
                            {
                                member.Roles.Add(roleToAdd);
                            }
                        }
                        currentServer.Members.Add(member);
                    }
                    return currentServer;
                },
                new { serverId, userId },
                splitOn: "ServerRoleId,UserMemberRoleId,ChannelClassId,ChannelId,ServerProfileId"
            );
            var serverWithAllData = serverDictionary.Distinct().FirstOrDefault().Value;

            var serverProfileQuery = """
                SELECT sp.*, sr.Id AS ServerRoleId, sr.*
                FROM ServerProfiles sp
                LEFT JOIN UserServerRoles usr ON usr.ServerId = sp.ServerId AND usr.UserId = sp.UserId
                LEFT JOIN ServerRoles sr ON sr.Id = usr.RoleId
                WHERE sp.ServerId = @serverId
                """;

            var memberDictionary = new Dictionary<string, ServerProfile>();

            await connection.QueryAsync<ServerProfile, ServerRole, ServerProfile>(
                serverProfileQuery,
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
                new { serverId },
                splitOn: "ServerRoleId"
            );
            var profiles = memberDictionary.Distinct();
            foreach (var sp in profiles)
            {
                if (serverWithAllData != null && serverWithAllData.Members != null)
                {
                    var existingProfile = serverWithAllData.Members.FirstOrDefault(x => x.Id == sp.Value.Id);
                    if (existingProfile == null)
                    {
                        serverWithAllData.Members.Add(sp.Value);
                    }
                }
            }

            return serverWithAllData ?? null;
        }

        public async Task UpdateCacheAsync(string serverId, Server server)
        {
            if (server == null || string.IsNullOrEmpty(serverId))
            {
                return;
            }
            await cache.SetAsync($"server_{serverId}", server);
        }
    }
}
