using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.CosmosDb.Models;
using Microsoft.Azure.Cosmos;

namespace CommunicationsApp.Infrastructure.CosmosDb
{
    public class CosmosDbService(CosmosDbFactory cosmosDbFactory) : ICosmosDbService
    {
        private Container MessageContainer => cosmosDbFactory.CosmosClient.GetContainer(cosmosDbFactory.DatabaseName, "messages");

        public async Task<MessageResult> GetServerMessagesAsync(string serverId)
        {
            if (string.IsNullOrWhiteSpace(serverId))
            {
                return new MessageResult { Succeeded = false, ErrorMessage = "Server ID was null or whitespace." };
            }
            try
            {
                var query = new QueryDefinition("SELECT * FROM messages m WHERE CONTAINS(m.partitionKey, @serverId)")
                    .WithParameter("@serverId", serverId);
                var messages = new List<ChatMessage>();
                using (var feedIterator = MessageContainer.GetItemQueryIterator<ChatMessage>(query))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        var response = await feedIterator.ReadNextAsync();
                        messages.AddRange(response);
                    }
                }
                messages = [.. messages.OrderBy(m => m.SentAt)];
                return new MessageResult { Succeeded = true, Messages = messages };
            }
            catch (Exception ex)
            {
                return new MessageResult
                {
                    Succeeded = false,
                    ErrorMessage = $"An error occurred while retrieving messages: {ex.Message}"
                };
            }
        }

        public async Task<MessageResult> SaveMessageAsync(ChatMessage message)
        {
            if (message == null)
            {
                return new MessageResult { Succeeded = false, ErrorMessage = "Message was null." };
            }
            try
            {
                var addResponse = await MessageContainer.CreateItemAsync(message, new PartitionKey(message.PartitionKey));

                return addResponse.StatusCode != System.Net.HttpStatusCode.Created
                    ? new MessageResult
                    {
                        Succeeded = false,
                        ErrorMessage = $"Failed to save message. Status code: {addResponse.StatusCode}"
                    }
                    : new MessageResult { Succeeded = true };
            }
            catch (Exception)
            {
                return new MessageResult
                {
                    Succeeded = false,
                    ErrorMessage = "An error occurred while saving the message."
                };
            }
        }
    }
}
