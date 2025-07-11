﻿using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Core.Models;
using CommunicationsApp.SharedKernel.Localization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace CommunicationsApp.Infrastructure.Services
{
    public class UserService(
        IConfiguration configuration,
        HybridCache cache,
        IStringLocalizer<CommunicationsAppLoc> localizer) : IUserService
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

        public async Task<ApplicationUser> GetUserFromDatabaseAsync(string userId, bool refreshCache = false)
        {
            var userDictionary = new Dictionary<string, ApplicationUser>();
            var getUserByIdQuery = """
                SELECT 
                    u.*,
                    ac.Id AS AccountSettingsId, ac.*
                FROM AspNetUsers u
                LEFT JOIN AccountSettings ac ON ac.UserId = u.Id
                WHERE u.Id = @userId
                """;
            using var connection = GetConnection();
            await connection.QueryAsync<ApplicationUser, AccountSettings, ApplicationUser>(
                getUserByIdQuery,
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
            await connection.QueryAsync<ServerProfile, UserServerRole, ServerRole, Server, ChannelClass, Channel, ApplicationUser>(
                getUserQuery,
                (sp, usr, sr, s, cc, c) =>
                {
                    userDictionary.TryGetValue(userId, out var currentUser);

                    if (sp != null && !string.IsNullOrWhiteSpace(sp.UserName))
                    {
                        var currentSP = currentUser.ServerProfiles.FirstOrDefault(x => x.Id == sp.Id);
                        if (currentSP == null)
                        {
                            currentSP = sp;
                            currentSP.Roles ??= [];
                            currentUser.ServerProfiles.Add(currentSP);
                        }
                        if (sr != null && !string.IsNullOrWhiteSpace(sr.Id) && currentSP.Roles.All(x => x.Id != sr.Id))
                        {
                            currentSP.Roles.Add(sr);
                        }
                    }

                    if (s != null && !string.IsNullOrWhiteSpace(s.Name))
                    {
                        var currentServer = currentUser.Servers.FirstOrDefault(x => x.Id == s.Id);
                        if (currentServer == null)
                        {
                            s.ChannelClasses ??= [];
                            currentUser.Servers.Add(s);
                            currentServer = s;
                        }
                        if (cc != null && !string.IsNullOrWhiteSpace(cc.Id))
                        {
                            var currentCC = currentServer.ChannelClasses.FirstOrDefault(x => x.Id == cc.Id);
                            if (currentCC == null)
                            {
                                cc.Channels ??= [];
                                currentServer.ChannelClasses.Add(cc);
                                currentCC = cc;
                            }
                            if (c != null && !string.IsNullOrWhiteSpace(c.Id))
                            {
                                if (currentCC.Channels.All(x => x.Id != c.Id))
                                {
                                    c.Messages ??= [];
                                    currentCC.Channels.Add(c);
                                }
                            }
                        }
                    }

                    return currentUser;
                },
                new { userId },
                splitOn: "UserServerRoleUserId,ServerRoleId,ServerId,ChannelClassId,ChannelId"
            );

            var userWithAllData = userDictionary.FirstOrDefault().Value;
            var serverIds = userWithAllData.Servers.Select(s => s.Id).ToList();
            foreach (var server in userWithAllData.Servers)
            {
                server.ChannelClasses = [.. server.ChannelClasses.OrderBy(cc => cc.OrderNumber)];
                foreach (var channelClass in server.ChannelClasses)
                {
                    channelClass.Channels = [.. channelClass.Channels.OrderBy(c => c.OrderNumber)];
                }
            }

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
            var groupedProfiles = memberDictionary.Values
                .GroupBy(sp => sp.ServerId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var server in userWithAllData.Servers)
            {
                server.Members ??= [];

                if (groupedProfiles.TryGetValue(server.Id!, out var profiles))
                {
                    server.Members.AddRange(profiles);
                }
            }

            return userWithAllData;
        }

        public async Task UpdateCacheAsync(ApplicationUser user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Id))
            {
                return;
            }
            await cache.SetAsync($"user_{user.Id}", user);
        }

        public async Task<dynamic> CreateAccountSettingsAsync(AccountSettings settings)
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

        public async Task<dynamic> GetAccountSettingsAsync(string userId)
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
