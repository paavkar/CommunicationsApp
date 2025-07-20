using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationsApp.Core.Models
{
    public class Channel
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ServerId { get; set; } = string.Empty;
        public string ChannelClassId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPrivate { get; set; } = false;
        public int OrderNumber { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        [NotMapped]
        public List<ChatMessage> Messages { get; set; } = [];

        public override string ToString()
        {
            return $"Channel [Id={Id ?? "null"}, Name={Name}, ServerId={ServerId}, " +
                   $"ChannelClassId={ChannelClassId}, Description={Description}, IsPrivate={IsPrivate}, " +
                   $"OrderNumber={OrderNumber}, CreatedAt={CreatedAt}]";
        }
    }
}
