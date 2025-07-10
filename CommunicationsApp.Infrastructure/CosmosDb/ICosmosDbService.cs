using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Infrastructure.CosmosDb
{
    public interface ICosmosDbService
    {
        Task<dynamic> SaveMessageAsync(ChatMessage message);
        Task<dynamic> GetServerMessagesAsync(string serverId);
    }
}
