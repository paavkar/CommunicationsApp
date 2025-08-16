using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.CosmosDb.Models;

namespace CommunicationsApp.Infrastructure.CosmosDb
{
    public interface ICosmosDbService
    {
        Task<MessageResult> SaveMessageAsync(ChatMessage message);
        Task<MessageResult> GetServerMessagesAsync(string serverId);
    }
}
