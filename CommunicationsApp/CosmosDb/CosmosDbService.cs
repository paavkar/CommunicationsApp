using CommunicationsApp.Models;
using Microsoft.Azure.Cosmos;

namespace CommunicationsApp.CosmosDb
{
    public class CosmosDbService(CosmosDbFactory cosmosDbFactory) : ICosmosDbService
    {
        private Container MessageContainer => cosmosDbFactory.CosmosClient.GetContainer(cosmosDbFactory.DatabaseName, "messages");
        
        public async Task<dynamic> GetServerMessagesAsync(string serverId)
        {
            if (string.IsNullOrWhiteSpace(serverId))
            {
                return new { Succeeded = false, ErrorMessage = "Server ID was null or whitespace." };
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
                return new { Succeeded = true, Messages = messages };
            }
            catch (Exception ex)
            {
                return new
                {
                    Succeeded = false,
                    ErrorMessage = $"An error occurred while retrieving messages: {ex.Message}"
                };
            }
        }

        public async Task<dynamic> SaveMessageAsync(ChatMessage message)
        {
            if (message == null)
            {
                return new { Succeeded = false, ErrorMessage = "Message was null." };
            }
            try
            {
                var addResponse = await MessageContainer.CreateItemAsync(message, new PartitionKey(message.PartitionKey));
                
                if (addResponse.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    return new
                    {
                        Succeeded = false,
                        ErrorMessage = $"Failed to save message. Status code: {addResponse.StatusCode}"
                    };
                }
                return new { Succeeded = true, Message = message };
            }
            catch (Exception)
            {
                return new
                {
                    Succeeded = false,
                    ErrorMessage = "An error occurred while saving the message."
                };
            }
        }
    }
}
