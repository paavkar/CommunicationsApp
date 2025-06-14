namespace CommunicationsApp.Models
{
    public class ChannelClass
    {
        public string Id { get; set; } = Guid.CreateVersion7().ToString();
        public string ServerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsPrivate { get; set; } = false;

        // Navigation properties
        public List<Channel> Channels { get; set; } = [];
    }
}
