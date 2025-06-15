using CommunicationsApp.Interfaces;
using CommunicationsApp.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using CommunicationsApp.Data;

namespace CommunicationsApp.Services
{
    public class ServerService(IConfiguration configuration) : IServerService
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

                
                var serverProfile = new {
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
    }
}
