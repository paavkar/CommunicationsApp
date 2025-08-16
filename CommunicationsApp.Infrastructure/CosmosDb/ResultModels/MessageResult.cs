using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Infrastructure.CosmosDb.Models
{
    public class MessageResult
    {

        public bool Succeeded { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ChatMessage>? Messages { get; set; }
    }
}
