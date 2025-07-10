using CommunicationsApp.CosmosDb;
using CommunicationsApp.Data;
using CommunicationsApp.Interfaces;
using CommunicationsApp.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Hybrid;
using static CommunicationsApp.Models.Enums;

namespace CommunicationsApp.Services
{
    public class ServerService(
        IConfiguration configuration,
        HybridCache cache,
        ICosmosDbService cosmosDbService) : IServerService
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
                    Name = "@everyone",
                    ServerId = server.Id,
                    HexColour = "",
                    Hierarchy = 1
                };

                var insertServerRoleQuery = """
                    INSERT INTO ServerRoles (Id, Name, ServerId, HexColour, Hierarchy, DisplaySeparately)
                    VALUES (@Id, @Name, @ServerId, @HexColour, @Hierarchy, @DisplaySeparately)
                    """;
                rowsAffected = await connection.ExecuteAsync(insertServerRoleQuery, serverRole, transaction);

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

                var insertRolePermissionsQuery = """
                    INSERT INTO ServerRolePermissions (RoleId, PermissionId)
                    VALUES (@RoleId, @PermissionId)
                    """;

                foreach (var permission in defaultPermissions)
                {
                    rowsAffected = await connection.ExecuteAsync(insertRolePermissionsQuery,
                        new ServerRolePermission() { RoleId = serverRole .Id, PermissionId = permission.Id},
                        transaction);
                }

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

        public async Task<Server?> GetServerByIdAsync(string serverId)
        {
            var cachedServer = await cache.GetOrCreateAsync<Server>(
                $"server_{serverId}",
                factory: async entry =>
                {
                    return await GetServerFromDatabaseAsync(serverId);
                }
            );
            return cachedServer;
        }

        public async Task<Server?> GetServerFromDatabaseAsync(string serverId)
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
                """;
            var server = await GetServerFromDatabaseAsync(getServerQuery, new { serverId });
            if (server is not null)
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

        public async Task<dynamic> GetServerByInvitationAsync(string invitationCode)
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
                WHERE s.InvitationCode = @invitationCode OR s.CustomInvitationCode = @invitationCode
                """;
            var server = await GetServerFromDatabaseAsync(getServerQuery, new { invitationCode });
            if (server is null)
            {
                return new { Succeeded = false, ErrorMessage = "There is no server associated with given invitation." };
            }
            return new { Succeeded = true, Server = server };
        }

        public async Task<dynamic> JoinServerAsync(Server server, ServerProfile profile)
        {
            var insertServerProfileQuery = """
                INSERT INTO ServerProfiles (Id, UserId, UserName, ServerId, DisplayName, ProfilePictureUrl, BannerUrl, CreatedAt, JoinedAt, Status, Bio)
                VALUES (@Id, @UserId, @UserName, @ServerId, @DisplayName, @ProfilePictureUrl, @BannerUrl, @CreatedAt, @JoinedAt, @Status, @Bio)
                """;
            using var connection = GetConnection();
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                var rowsAffected = await connection.ExecuteAsync(insertServerProfileQuery, profile, transaction);

                if (rowsAffected <= 0)
                {
                    transaction.Rollback();
                    return new { Succeeded = false, ErrorMessage = "Failed to join server." };
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
            }

            var serverProfileQuery = """
                SELECT sp.*, sr.Id AS ServerRoleId, sr.*
                FROM ServerProfiles sp
                LEFT JOIN UserServerRoles usr ON usr.ServerId = sp.ServerId AND usr.UserId = sp.UserId
                LEFT JOIN ServerRoles sr ON sr.Id = usr.RoleId
                WHERE sp.ServerId = @serverId
                """;

            server = await GetServerWithMembersAsync(serverProfileQuery, new { serverId = server.Id }, server);

            await GetMessagesAsync(server, server.Id!);
            await UpdateCacheAsync(server.Id, server);

            return new { Succeeded = true, Server = server };
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

                    if (role != null && !string.IsNullOrWhiteSpace(role.Id) && currentServer.Roles.All(r => r.Id != role.Id))
                    {
                        role.Permissions ??= [];
                        currentServer.Roles.Add(role);
                    }

                    if (channelClass != null && !string.IsNullOrWhiteSpace(channelClass.Id))
                    {
                        var existingChannelClass = currentServer.ChannelClasses.FirstOrDefault(cc => cc.Id == channelClass.Id);
                        if (existingChannelClass == null)
                        {
                            existingChannelClass = channelClass;
                            existingChannelClass.Channels ??= [];
                            currentServer.ChannelClasses.Add(existingChannelClass);
                        }
                        if (channel != null && !string.IsNullOrWhiteSpace(channel.Id) &&
                            existingChannelClass.Channels.All(ch => ch.Id != channel.Id))
                        {
                            channel.Messages ??= [];
                            existingChannelClass.Channels.Add(channel);
                        }
                    }

                    if (member != null && !string.IsNullOrWhiteSpace(member.Id))
                    {
                        member.Roles ??= [];
                        member = currentServer.Members.FirstOrDefault(m => m.Id == member.Id) ?? member;

                        if (role.Name == "@everyone" && !member.Roles.Any(r => r.Name == "@everyone"))
                        {
                            member.Roles.Add(role);
                        }

                        if (userServerRole.RoleId != null && userServerRole.UserId == member.UserId)
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

            if (serverWithAllData == null)
            {
                return null;
            }

            var getServerPermissionsQuery = """
                SELECT 
                    sp.*,
                    srp.*
                FROM ServerPermissions sp
                LEFT JOIN ServerRolePermissions srp ON srp.PermissionId = sp.Id
                WHERE srp.RoleId IN (SELECT Id FROM ServerRoles WHERE ServerId = @serverId)
                """;

            await connection.QueryAsync<ServerPermission, ServerRolePermission, ServerPermission>(
                getServerPermissionsQuery,
                (permission, rolePermission) =>
                {
                    if (serverWithAllData.Roles != null)
                    {
                        var role = serverWithAllData.Roles.FirstOrDefault(r => r.Id == rolePermission.RoleId);
                        if (role != null && !role.Permissions.Any(p => p.Id == permission.Id))
                        {
                            role.Permissions.Add(permission);
                        }
                    }
                    return permission;
                },
                new { serverId = serverWithAllData.Id },
                splitOn: "RoleId"
            );

            foreach (var role in serverWithAllData.Roles)
            {
                foreach (var member in serverWithAllData.Members)
                {
                    var memberRole = member.Roles.FirstOrDefault(r => r.Id == role.Id);
                    if (memberRole != null)
                    {
                        memberRole.Permissions = [.. role.Permissions];
                    }
                }
            }

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
                    if (role != null)
                    {
                        role.Permissions ??= [];
                        if (!member.Roles.Any(r => r.Name == "@everyone"))
                        {
                            var everyoneRole = server.Roles.FirstOrDefault(r => r.Name == "@everyone");
                            member.Roles.Add(everyoneRole);
                        }
                        if (!string.IsNullOrWhiteSpace(role.Id) && member.Roles.All(r => r.Id != role.Id))
                        {
                            role.Permissions = server.Roles.FirstOrDefault(r => r.Id == role.Id).Permissions;
                            member.Roles.Add(role);
                        }
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
            if (server == null || string.IsNullOrWhiteSpace(serverId))
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
                    var server = await GetServerFromDatabaseAsync(serverId);
                    server.Members.RemoveAll(m => m.UserId == userId);
                    await UpdateCacheAsync(serverId, server);
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

        public async Task<dynamic> AddChannelClassAsync(ChannelClass channelClass)
        {
            var server = await GetServerByIdAsync(channelClass.ServerId);
            server.ChannelClasses.Add(channelClass);
            var insertChannelClassQuery = """
                INSERT INTO ChannelClasses (Id, Name, ServerId, IsPrivate, OrderNumber)
                VALUES (@Id, @Name, @ServerId, @IsPrivate, @OrderNumber)
                """;
            using var connection = GetConnection();
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                var rowsAffected = await connection.ExecuteAsync(insertChannelClassQuery, channelClass, transaction);
                if (rowsAffected > 0)
                {
                    transaction.Commit();
                    await UpdateCacheAsync(server.Id!, server);
                    return new { Succeeded = true, ChannelClass = channelClass };
                }
                else
                {
                    transaction.Rollback();
                    return new { Succeeded = false, ErrorMessage = "Failed to add channel class." };
                }
            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine($"Error: {e.Message}");
                return new { Succeeded = false, ErrorMessage = e.Message };
            }
        }

        public async Task<dynamic> AddChannelAsync(string channelClassId, Channel channel)
        {
            var server = await GetServerByIdAsync(channel.ServerId);
            var channelClass = server.ChannelClasses.FirstOrDefault(cc => cc.Id == channelClassId);
            if (channelClass == null)
            {
                return new { Succeeded = false, ErrorMessage = "Channel class not found." };
            }
            channelClass.Channels.Add(channel);
            var insertChannelQuery = """
                INSERT INTO Channels (Id, Name, ServerId, ChannelClassId, Description, IsPrivate, OrderNumber, CreatedAt)
                VALUES (@Id, @Name, @ServerId, @ChannelClassId, @Description, @IsPrivate, @OrderNumber, @CreatedAt)
                """;
            using var connection = GetConnection();
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                var rowsAffected = await connection.ExecuteAsync(insertChannelQuery, channel, transaction);
                if (rowsAffected > 0)
                {
                    transaction.Commit();
                    await UpdateCacheAsync(server.Id!, server);
                    return new { Succeeded = true, Channel = channel };
                }
                else
                {
                    transaction.Rollback();
                    return new { Succeeded = false, ErrorMessage = "Failed to add channel." };
                }
            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine($"Error: {e.Message}");
                return new { Succeeded = false, ErrorMessage = e.Message };
            }
        }

        public async Task<dynamic> AddServerPermissionsAsync()
        {
            foreach (Enums.ServerPermissionType permission in Enum.GetValues<Enums.ServerPermissionType>())
            {
                var insertServerPermissionQuery = """
                    INSERT INTO ServerPermissions (Id, PermissionType, PermissionName)
                    SELECT @Id, @PermissionType, @PermissionName
                    WHERE NOT EXISTS (
                        SELECT 1 FROM ServerPermissions WHERE PermissionName = @PermissionName
                    )
                    """;
                using var connection = GetConnection();
                connection.Open();
                using SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    var rowsAffected = await connection.ExecuteAsync(insertServerPermissionQuery, new
                    {
                        Id = Guid.CreateVersion7().ToString(),
                        PermissionType = permission,
                        PermissionName = permission.ToString()
                    }, transaction);
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Error: {e.Message}");
                    return new { Succeeded = false, ErrorMessage = e.Message };
                }
            }
            return new { Succeeded = true, Message = "Server permissions added successfully." };
        }

        public async Task<List<ServerPermission>> GetServerPermissionsAsync()
        {
            var getServerPermissionsQuery = """
                SELECT * FROM ServerPermissions
                """;
            using var connection = GetConnection();
            var serverPermissions = await connection.QueryAsync<ServerPermission>(getServerPermissionsQuery);
            return serverPermissions.ToList();
        }
    }
}
