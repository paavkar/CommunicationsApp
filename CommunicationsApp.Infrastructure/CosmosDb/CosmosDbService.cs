using CommunicationsApp.Application.Notifications;
using CommunicationsApp.Application.ResultModels;
using CommunicationsApp.Core.Models;
using Microsoft.Azure.Cosmos;

namespace CommunicationsApp.Infrastructure.CosmosDb
{
    public class CosmosDbService(
        CosmosDbFactory cosmosDbFactory,
        ICommunicationsNotificationService cns) : ICosmosDbService
    {
        private Container MessageContainer =>
            cosmosDbFactory.CosmosClient.GetContainer(cosmosDbFactory.DatabaseName, "messages");

        public async Task<MessageResult> GetServerMessagesAsync(string serverId)
        {
            if (string.IsNullOrWhiteSpace(serverId))
            {
                return new MessageResult { Succeeded = false, ErrorMessage = "Server ID was null or whitespace." };
            }
            try
            {
                QueryDefinition query = new QueryDefinition("SELECT * FROM messages m WHERE CONTAINS(m.partitionKey, @serverId)")
                    .WithParameter("@serverId", serverId);
                List<ChatMessage> messages = [];
                using (FeedIterator<ChatMessage> feedIterator = MessageContainer.GetItemQueryIterator<ChatMessage>(query))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        FeedResponse<ChatMessage> response = await feedIterator.ReadNextAsync();
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
                ItemResponse<ChatMessage> addResponse = await MessageContainer.CreateItemAsync(message,
                    new PartitionKey(message.PartitionKey));

                if (addResponse.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    return new MessageResult
                    {
                        Succeeded = false,
                        ErrorMessage = $"Failed to save message. Status code: {addResponse.StatusCode}"
                    };
                }

                MessageResult result = await cns.SendMessageAsync(
                    message.Channel.ServerId, message.Channel.Id, message);
                result.Succeeded = true;

                return result;
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
