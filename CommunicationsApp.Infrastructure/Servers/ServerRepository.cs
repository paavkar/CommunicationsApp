using CommunicationsApp.Application.DTOs;
using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.CosmosDb;
using CommunicationsApp.SharedKernel.Localization;
using Dapper;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CommunicationsApp.Infrastructure.Services
{
    public class ServerRepository(
        IConfiguration configuration,
        HybridCache cache,
        ICosmosDbService cosmosDbService,
        IStringLocalizer<CommunicationsAppLoc> localizer,
        ILogger<ServerService> logger,
        AuthenticationStateProvider asp) : IServerRepository
    {
        private SqlConnection GetConnection()
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            return new SqlConnection(connectionString);
        }

        public async Task<int> InsertServerAsync(Server server)
        {
            const string sql = """
            INSERT INTO Servers
              (Id, Name, InvitationCode, CustomInvitationCode,
               Description, OwnerId, CreatedAt, IconUrl, BannerUrl, ServerType)
            VALUES
              (@Id, @Name, @InvitationCode, @CustomInvitationCode,
               @Description, @OwnerId, @CreatedAt, @IconUrl, @BannerUrl, @ServerType)
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, server);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error inserting server: {Method}", nameof(InsertServerAsync));
                return 0;
            }
        }

        public async Task<Server?> GetServerByIdAsync(string serverId)
        {
            const string sql = """
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
            return await LoadServerAggregateAsync(sql, new { serverId });
        }

        public async Task<Server?> GetServerByInvitationAsync(string invitationCode)
        {
            const string sql = """
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
            return await LoadServerAggregateAsync(sql, new { invitationCode });
        }

        public async Task<int> UpdateServerInfoAsync(string serverId, ServerInfoUpdate update)
        {
            const string sql = """
            UPDATE Servers
            SET Name = @Name,
                Description = @Description
            WHERE Id = @serverId
            """;
            using var conn = GetConnection();
            return await conn.ExecuteAsync(sql, new { update.Name, update.Description, serverId });
        }

        public async Task<int> InsertServerRoleAsync(ServerRole role)
        {
            const string sql = """
            INSERT INTO ServerRoles
              (Id, Name, ServerId, HexColour, Hierarchy, DisplaySeparately)
            VALUES
              (@Id, @Name, @ServerId, @HexColour, @Hierarchy, @DisplaySeparately)
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, role);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error inserting server role: {Method}", nameof(InsertServerRoleAsync));
                return 0;
            }
        }

        public async Task<int> UpdateServerRoleAsync(ServerRole role)
        {
            const string sql = """
            UPDATE ServerRoles
            SET Name = @Name, HexColour = @HexColour, Hierarchy = @Hierarchy, DisplaySeparately = @DisplaySeparately
            WHERE Id = @Id AND ServerId = @ServerId
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, role);

            }
            catch (Exception)
            {
                logger.LogError("Error updating server role: {RoleId}", role.Id);
                return 0;
            }
        }

        public async Task<int> UpdateServerRoleHierarchyAsync(ServerRole role)
        {
            const string sql = """
            UPDATE ServerRoles
            SET Hierarchy = @Hierarchy
            WHERE Id = @Id
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, role);
            }
            catch (Exception)
            {
                logger.LogError("Error updating server role hierarchy: {RoleId}", role.Id);
                return 0;
            }
        }

        public async Task UpdateServerPermissionAsync(ServerPermission permission)
        {
            const string sql = """
            UPDATE ServerPermissions
            SET PermissionType = @PermissionType
            WHERE Id = @Id
            """;
            using var conn = GetConnection();
            try
            {
                await conn.ExecuteAsync(sql, permission);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating server permission: {PermissionName}", permission.ToString());
            }
        }

        public async Task UpsertServerPermissionAsync(ServerPermission permission)
        {
            const string sql = """
            INSERT INTO ServerPermissions
              (Id, PermissionType, PermissionName)
            SELECT @Id, @PermissionType, @PermissionName
            WHERE NOT EXISTS (
              SELECT 1 FROM ServerPermissions WHERE PermissionName = @PermissionName
            )
            """;
            using var conn = GetConnection();
            try
            {
                await conn.ExecuteAsync(sql, permission);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding server permission: {PermissionName}", permission.ToString());
            }
        }

        public async Task<Server> GetServerRolePermissionsAsync(Server server)
        {
            var sql = """
                SELECT 
                    sp.*,
                    srp.*
                FROM ServerPermissions sp
                LEFT JOIN ServerRolePermissions srp ON srp.PermissionId = sp.Id
                WHERE srp.RoleId IN (SELECT Id FROM ServerRoles WHERE ServerId = @serverId)
                """;
            using var conn = GetConnection();
            try
            {
                await conn.QueryAsync<ServerPermission, ServerRolePermission, ServerPermission>(
                    sql,
                    (permission, rolePermission) =>
                    {
                        if (server.Roles != null)
                        {
                            var role = server.Roles.FirstOrDefault(r => r.Id == rolePermission.RoleId);
                            if (role != null && !role.Permissions.Any(p => p.Id == permission.Id))
                            {
                                role.Permissions.Add(permission);
                            }
                        }
                        return permission;
                    },
                    new { serverId = server.Id },
                    splitOn: "RoleId"
                );

                foreach (var role in server.Roles)
                {
                    foreach (var member in server.Members)
                    {
                        var memberRole = member.Roles.FirstOrDefault(r => r.Id == role.Id);
                        if (memberRole != null)
                        {
                            memberRole.Permissions = [.. role.Permissions];
                        }
                    }
                }

                return server;
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<IEnumerable<ServerPermission>> GetAllPermissionsAsync()
        {
            const string sql = "SELECT * FROM ServerPermissions";
            using var conn = GetConnection();
            try
            {
                return await conn.QueryAsync<ServerPermission>(sql);
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<int> UpsertServerRolePermissionsAsync(string roleId, IEnumerable<string> permissionIds)
        {
            const string sql = """
            INSERT INTO ServerRolePermissions (RoleId, PermissionId)
            SELECT @RoleId, @PermissionId
            WHERE NOT EXISTS (
              SELECT 1 FROM ServerRolePermissions
              WHERE RoleId = @RoleId AND PermissionId = @PermissionId
            )
            """;
            var count = 0;
            using var conn = GetConnection();
            foreach (var pid in permissionIds)
            {
                try
                {
                    count += await conn.ExecuteAsync(sql, new { RoleId = roleId, PermissionId = pid });
                }
                catch (Exception)
                {
                    logger.LogError("Error inserting server role permission: {RoleId}, {PermissionId}", roleId, pid);
                }
            }
            return count;
        }

        public async Task<int> DeleteServerRolePermissionsNotInAsync(string roleId, IEnumerable<string> permissionIds)
        {
            const string sql = """
            DELETE FROM ServerRolePermissions
            WHERE RoleId = @RoleId
              AND PermissionId NOT IN @PermissionIds
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, new { RoleId = roleId, PermissionIds = permissionIds });
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> InsertUserServerRoleAsync(string userId, string serverId, string roleId)
        {
            const string sql = """
            INSERT INTO UserServerRoles (UserId, ServerId, RoleId)
            SELECT @UserId, @ServerId, @RoleId
            WHERE NOT EXISTS (
              SELECT 1 FROM UserServerRoles
              WHERE UserId = @UserId
                AND ServerId = @ServerId
                AND RoleId = @RoleId)
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, new { userId, serverId, roleId });
            }
            catch (Exception)
            {
                logger.LogError("Error inserting user server role: {Method}", nameof(InsertUserServerRoleAsync));
                return 0;
            }
        }

        public async Task<int> DeleteUserServerRoleAsync(string userId, string serverId, string roleId)
        {
            const string sql = """
            DELETE FROM UserServerRoles
            WHERE UserId = @UserId
              AND ServerId = @ServerId
              AND RoleId = @RoleId
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, new { userId, serverId, roleId });
            }
            catch (Exception)
            {
                logger.LogError("Error deleting user server role: {Method}", nameof(DeleteUserServerRoleAsync));
                return 0;
            }
        }

        public async Task<int> InsertServerProfileAsync(ServerProfile profile)
        {
            const string sql = """
            INSERT INTO ServerProfiles
              (Id, UserId, UserName, ServerId,
               DisplayName, ProfilePictureUrl, BannerUrl,
               CreatedAt, JoinedAt, Status, Bio)
            VALUES
              (@Id, @UserId, @UserName, @ServerId,
               @DisplayName, @ProfilePictureUrl, @BannerUrl,
               @CreatedAt, @JoinedAt, @Status, @Bio)
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, profile);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error inserting server profile: {Method}", nameof(InsertServerProfileAsync));
                return 0;
            }
        }

        public async Task<int> DeleteServerProfileAsync(string serverId, string userId)
        {
            const string sql = """
            DELETE FROM ServerProfiles
            WHERE ServerId = @serverId
              AND UserId = @userId
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, new { serverId, userId });
            }
            catch (Exception)
            {
                logger.LogError("Error deleting server profile: {Method}", nameof(DeleteServerProfileAsync));
                return 0;
            }
        }

        public async Task<int> DeleteServerProfilesAsync(string serverId, IEnumerable<string> userIds)
        {
            const string sql = """
            DELETE FROM ServerProfiles
            WHERE ServerId = @serverId
              AND UserId IN @UserIds
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, new { serverId, userIds = userIds });
            }
            catch (Exception)
            {
                logger.LogError("Error deleting server profiles: {Method}", nameof(DeleteServerProfilesAsync));
                return 0;
            }
        }

        public async Task<int> InsertChannelClassAsync(ChannelClass channelClass)
        {
            const string sql = """
            INSERT INTO ChannelClasses
              (Id, Name, ServerId, IsPrivate, OrderNumber)
            VALUES
              (@Id, @Name, @ServerId, @IsPrivate, @OrderNumber)
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, channelClass);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error inserting channel class: {Method}", nameof(InsertChannelClassAsync));
                return 0;
            }
        }

        public async Task<int> InsertChannelAsync(Channel channel)
        {
            const string sql = """
            INSERT INTO Channels
              (Id, Name, ServerId, ChannelClassId,
               Description, IsPrivate, OrderNumber, CreatedAt)
            VALUES
              (@Id, @Name, @ServerId, @ChannelClassId,
               @Description, @IsPrivate, @OrderNumber, @CreatedAt)
            """;
            using var conn = GetConnection();
            try
            {
                return await conn.ExecuteAsync(sql, channel);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error inserting channel: {Method}", nameof(InsertChannelAsync));
                return 0;
            }
        }

        public async Task<Server?> LoadServerAggregateAsync(string sql, object queryParameters)
        {
            var serverDictionary = new Dictionary<string, Server>();

            using var conn = GetConnection();
            try
            {
                await conn.QueryAsync<Server, ServerRole, UserServerRole, ChannelClass, Channel, ServerProfile, Server>(
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
                var server = serverDictionary.Values.FirstOrDefault();

                server = await GetServerRolePermissionsAsync(server);

                server.ChannelClasses = [.. server.ChannelClasses.OrderBy(cc => cc.OrderNumber)];
                foreach (var channelClass in server.ChannelClasses)
                {
                    channelClass.Channels = [.. channelClass.Channels.OrderBy(c => c.OrderNumber)];
                }
                foreach (var member in server.Members)
                {
                    member.Roles = [.. member.Roles.OrderBy(r => r.Hierarchy)];
                }

                return server;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error loading server aggregate: {Method}", nameof(LoadServerAggregateAsync));
                return null!;
            }
        }

        public async Task<Server?> LoadServerProfilesAsync(Server server)
        {
            var sql = """
                SELECT sp.*, sr.Id AS ServerRoleId, sr.*
                FROM ServerProfiles sp
                LEFT JOIN UserServerRoles usr ON usr.ServerId = sp.ServerId AND usr.UserId = sp.UserId
                LEFT JOIN ServerRoles sr ON sr.Id = usr.RoleId
                WHERE sp.ServerId = @serverId
                """;
            var memberDictionary = new Dictionary<string, ServerProfile>();

            using var conn = GetConnection();
            try
            {
                await conn.QueryAsync<ServerProfile, ServerRole, ServerProfile>(
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
                new { serverId = server.Id },
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
            catch (Exception e)
            {
                logger.LogError(e, "Error loading server profiles: {Method}", nameof(LoadServerProfilesAsync));
                return null;
            }
        }
    }
}
