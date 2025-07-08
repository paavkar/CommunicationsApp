using CommunicationsApp.Models;

namespace CommunicationsApp.CosmosDb
{
    public interface ICosmosDbService
    {
        Task<dynamic> SaveMessageAsync(ChatMessage message);
        Task<dynamic> GetServerMessagesAsync(string serverId);
    }
}
