using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.ResultModels
{
    public class MessageResult
    {
        public bool Succeeded { get; set; }
        public bool SignalRSucceeded { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ChatMessage>? Messages { get; set; }
    }
}
