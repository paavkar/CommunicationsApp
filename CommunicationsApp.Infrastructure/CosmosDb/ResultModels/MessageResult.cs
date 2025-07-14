using CommunicationsApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationsApp.Infrastructure.CosmosDb.Models
{
    public class MessageResult
    {

        public bool? Succeeded { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ChatMessage>? Messages { get; set; }
    }
}
