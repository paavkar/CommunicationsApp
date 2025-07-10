namespace CommunicationsApp.Core.Models
{
    public class ChannelClass
    {
        public string? Id { get; set; }
        public string ServerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsPrivate { get; set; } = false;
        public int OrderNumber { get; set; }

        // Navigation properties
        public List<Channel> Channels { get; set; } = [];

        public override string ToString()
        {
            return $"ChannelClass [Id={Id ?? "null"}, ServerId={ServerId}, Name={Name}, IsPrivate={IsPrivate}, " +
                $"OrderNumber={OrderNumber}]";
        }
    }
}
