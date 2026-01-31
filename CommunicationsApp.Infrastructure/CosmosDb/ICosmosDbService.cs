using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Infrastructure.CosmosDb
{
    public interface ICosmosDbService
    {
        Task<MessageResult> SaveMessageAsync(ChatMessage message);
        Task<MessageResult> GetServerMessagesAsync(string serverId);
    }
}
