using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Models;
using CommunicationsApp.SharedKernel.Localization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CommunicationsApp.Infrastructure.Services
{
    public class UserService(
        IConfiguration configuration,
        HybridCache cache,
        IStringLocalizer<CommunicationsAppLoc> localizer,
        ILogger<UserService> logger) : IUserService
    {
        private SqlConnection GetConnection()
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            return new SqlConnection(connectionString);
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            var cachedUser = await cache.GetOrCreateAsync<ApplicationUser>(
                $"user_{userId}",
                factory: async entry =>
                {
                    return await GetUserFromDatabaseAsync(userId);
                }
            );

            return cachedUser;
        }

        public async Task<ApplicationUser> GetUserFromDatabaseAsync(string userId)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var user = await GetUserWithSettingsAsync(conn, userId);
            if (user is null)
            {
                return null!;
            }
            var (profiles, servers) = await GetServerProfilesAsync(conn, userId);
            var memberLookup = await GetServerMembersAsync(conn, profiles.Select(sp => sp.ServerId!));

            user.ServerProfiles = profiles;
            user.Servers = servers;

            foreach (var server in user.Servers)
            {
                if (memberLookup.TryGetValue(server.Id!, out var members))
                    server.Members = members;
            }

            return user;
        }

        public async Task<ApplicationUser> GetUserWithSettingsAsync(IDbConnection connection, string userId)
        {
            var sql = """
                SELECT 
                    u.*,
                    ac.Id AS AccountSettingsId, ac.*
                FROM AspNetUsers u
                LEFT JOIN AccountSettings ac ON ac.UserId = u.Id
                WHERE u.Id = @userId
                """;

            var userDictionary = new Dictionary<string, ApplicationUser>();

            await connection.QueryAsync<ApplicationUser, AccountSettings, ApplicationUser>(
                sql,
                (appUser, ac) =>
                {
                    if (!userDictionary.TryGetValue(appUser.Id, out var currentUser))
                    {
                        currentUser = appUser;
                        currentUser.ServerProfiles ??= [];
                        currentUser.Servers ??= [];
                        userDictionary.Add(currentUser.Id, currentUser);
                    }

                    if (ac != null && !string.IsNullOrWhiteSpace(ac.UserId) && ac.UserId == appUser.Id)
                    {
                        currentUser.AccountSettings = ac;
                    }

                    return currentUser;
                },
                new { userId },
                splitOn: "AccountSettingsId"
            );

            if (userDictionary.Count == 0)
            {
                logger.LogError("No user found with the given ID {Method}", nameof(GetUserWithSettingsAsync));
                return null!;
            }
            else
            {
                return userDictionary.Values.First();
            }
        }

        public async Task<(List<ServerProfile> Profiles, List<Server> Servers)> GetServerProfilesAsync(
            IDbConnection connection, string userId)
        {
            var getUserQuery = """
                SELECT 
                    sp.*,
                    usr.UserId AS UserServerRoleUserId, usr.RoleId AS UserServerRoleId, usr.ServerId AS UserServerRoleServerId, 
                    sr.Id AS ServerRoleId, sr.ServerId AS SR_ServerId, sr.*,
                    s.Id AS ServerId, s.*,
                    cc.Id AS ChannelClassId, cc.*,
                    c.Id AS ChannelId, c.*
                FROM ServerProfiles sp
                LEFT JOIN UserServerRoles usr ON usr.UserId = sp.UserId AND usr.ServerId = sp.ServerId
                LEFT JOIN ServerRoles sr ON sr.Id = usr.RoleId
                LEFT JOIN Servers s ON sp.ServerId = s.Id
                LEFT JOIN ChannelClasses cc ON cc.ServerId = s.Id
                LEFT JOIN Channels c ON c.ChannelClassId = cc.Id
                WHERE sp.UserId = @userId
                """;
            var profileDict = new Dictionary<string, ServerProfile>();
            var serverDict = new Dictionary<string, Server>();

            await connection.QueryAsync<ServerProfile, UserServerRole, ServerRole, Server, ChannelClass, Channel, ServerProfile>(
                getUserQuery,
                (sp, usr, sr, s, cc, c) =>
                {
                    if (!profileDict.TryGetValue(sp.Id, out var currentSp))
                    {
                        currentSp = sp;
                        currentSp.Roles ??= [];
                        profileDict.Add(sp.Id, currentSp);
                    }
                    if (sr != null && !string.IsNullOrWhiteSpace(sr.Id) && currentSp.Roles.All(x => x.Id != sr.Id))
                    {
                        currentSp.Roles.Add(sr);
                    }

                    if (s is not null && !string.IsNullOrWhiteSpace(s.Id))
                    {
                        if (!serverDict.TryGetValue(s.Id, out var currentServer))
                        {
                            s.ChannelClasses ??= [];
                            serverDict.Add(s.Id, s);
                            currentServer = s;
                        }

                        if (cc is not null && !string.IsNullOrWhiteSpace(cc.Id))
                        {
                            var classList = currentServer.ChannelClasses!;
                            var foundCc = classList.FirstOrDefault(x => x.Id == cc.Id);
                            if (foundCc == null)
                            {
                                cc.Channels ??= [];
                                classList.Add(cc);
                                foundCc = cc;
                            }

                            if (c is not null && !string.IsNullOrWhiteSpace(c.Id) &&
                                foundCc.Channels!.All(x => x.Id != c.Id))
                            {
                                foundCc.Channels.Add(c);
                            }
                        }
                    }

                    return currentSp;
                },
                new { userId },
                splitOn: "UserServerRoleUserId,ServerRoleId,ServerId,ChannelClassId,ChannelId"
            );

            var profiles = profileDict.Values.ToList();
            var servers = serverDict.Values.ToList();

            return (profiles, servers);
        }

        public async Task<Dictionary<string, List<ServerProfile>>> GetServerMembersAsync(IDbConnection connection,
            IEnumerable<string> serverIds)
        {
            var serverProfileQuery = """
                SELECT sp.*, sr.Id AS ServerRoleId, sr.*
                FROM ServerProfiles sp
                LEFT JOIN UserServerRoles usr ON usr.ServerId = sp.ServerId AND usr.UserId = sp.UserId
                LEFT JOIN ServerRoles sr ON sr.Id = usr.RoleId
                WHERE sp.ServerId IN @serverIds
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

                    if (role != null && !string.IsNullOrWhiteSpace(role.Id) && member.Roles.All(r => r.Id != role.Id))
                    {
                        member.Roles.Add(role);
                    }
                    return member;
                },
                new { serverIds },
                splitOn: "ServerRoleId"
            );
            return memberDictionary.Values
                .GroupBy(sp => sp.ServerId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task UpdateCacheAsync(ApplicationUser user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Id))
            {
                return;
            }
            await cache.SetAsync($"user_{user.Id}", user);
        }

        public async Task<AccountSettingsResult> CreateAccountSettingsAsync(AccountSettings settings)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.UserId))
            {
                return new AccountSettingsResult { Succeeded = false, ErrorMessage = "Error with the given settings." };
            }
            var query = """
                INSERT INTO AccountSettings (Id, UserId, PreferredLocale, DisplayServerMemberList, PreferredTheme)
                VALUES (@Id, @UserId, @PreferredLocale, @DisplayServerMemberList, @PreferredTheme)
                """;
            using var connection = GetConnection();
            var rowsAffected = await connection.ExecuteAsync(query, settings);

            return rowsAffected > 0
                ? new AccountSettingsResult { Succeeded = true }
                : new AccountSettingsResult { Succeeded = false, ErrorMessage = "There was an error adding settings." };
        }

        public async Task<AccountSettingsResult> GetAccountSettingsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new AccountSettingsResult { Succeeded = false, ErrorMessage = "User ID cannot be null or empty." };
            }
            var query = """
                SELECT * FROM AccountSettings WHERE UserId = @userId
                """;
            using var connection = GetConnection();
            var settings = await connection.QueryFirstOrDefaultAsync<AccountSettings>(query, new { userId });

            return settings == null
                ? new AccountSettingsResult { Succeeded = false, ErrorMessage = "No settings found for the given user." }
                : new AccountSettingsResult { Succeeded = true, Settings = settings };
        }
    }
}
