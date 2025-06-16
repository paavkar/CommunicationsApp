using CommunicationsApp.Data;
using CommunicationsApp.Interfaces;
using CommunicationsApp.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Hybrid;

namespace CommunicationsApp.Services
{
    public class UserService(IConfiguration configuration, HybridCache cache) : IUserService
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
            var getUserQuery = """
                SELECT 
                    u.*, 
                    sp.Id AS ServerProfileId, sp.*,
                    usr.UserId AS UserServerRoleUserId, usr.RoleId AS UserServerRoleId, usr.ServerId AS UserServerRoleServerId, 
                    sr.Id AS ServerRoleId, sr.ServerId AS SR_ServerId, sr.*,
                    s.Id AS ServerId, s.*,
                    cc.Id AS ChannelClassId, cc.*,
                    c.Id AS ChannelId, c.*
                FROM AspNetUsers u
                LEFT JOIN ServerProfiles sp ON sp.UserId = u.Id
                LEFT JOIN UserServerRoles usr ON usr.UserId = u.Id AND usr.ServerId = sp.ServerId
                LEFT JOIN ServerRoles sr ON sr.Id = usr.RoleId
                LEFT JOIN Servers s ON sp.ServerId = s.Id
                LEFT JOIN ChannelClasses cc ON cc.ServerId = s.Id
                LEFT JOIN Channels c ON c.ChannelClassId = cc.Id
                WHERE u.Id = @userId
                """;
            using var connection = GetConnection();
            var result = await connection.QueryAsync<ApplicationUser, ServerProfile, UserServerRole, ServerRole, Server, ChannelClass, Channel, ApplicationUser>(
                getUserQuery,
                (appUser, sp, usr, sr, s, cc, c) =>
                {
                    if (!userDictionary.TryGetValue(appUser.Id, out var currentUser))
                    {
                        currentUser = appUser;
                        currentUser.ServerProfiles ??= [];
                        currentUser.Servers ??= [];
                        userDictionary.Add(currentUser.Id, currentUser);
                    }

                    if (sp != null && !string.IsNullOrEmpty(sp.UserName))
                    {
                        var currentSP = currentUser.ServerProfiles.FirstOrDefault(x => x.Id == sp.Id);
                        if (currentSP == null)
                        {
                            currentSP = sp;
                            currentSP.Roles ??= [];
                            currentUser.ServerProfiles.Add(currentSP);
                        }
                        if (sr != null && !string.IsNullOrEmpty(sr.Id) && currentSP.Roles.All(x => x.Id != sr.Id))
                        {
                            currentSP.Roles.Add(sr);
                        }
                    }

                    if (s != null && !string.IsNullOrEmpty(s.Name))
                    {
                        var currentServer = currentUser.Servers.FirstOrDefault(x => x.Id == s.Id);
                        if (currentServer == null)
                        {
                            s.ChannelClasses ??= [];
                            currentUser.Servers.Add(s);
                            currentServer = s;
                        }
                        if (cc != null && !string.IsNullOrEmpty(cc.Id))
                        {
                            var currentCC = currentServer.ChannelClasses.FirstOrDefault(x => x.Id == cc.Id);
                            if (currentCC == null)
                            {
                                cc.Channels ??= [];
                                currentServer.ChannelClasses.Add(cc);
                                currentCC = cc;
                            }
                            if (c != null && !string.IsNullOrEmpty(c.Id))
                            {
                                if (currentCC.Channels.All(x => x.Id != c.Id))
                                {
                                    currentCC.Channels.Add(c);
                                }
                            }
                        }
                    }

                    return currentUser;
                },
                new { userId },
                splitOn: "ServerProfileId,UserServerRoleUserId,ServerRoleId,ServerId,ChannelClassId,ChannelId"
            );
            var userWithAllData = userDictionary.Distinct().FirstOrDefault().Value;

            if (refreshCache)
            {
                await cache.SetAsync(
                key: $"user_{userId}",
                value: userWithAllData);
            }

            return userWithAllData;
        }
    }
}
