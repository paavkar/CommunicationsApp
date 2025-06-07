namespace CommunicationsApp.Models
{
    public class ServerRole
    {
        public string Id { get; set; } = Guid.CreateVersion7().ToString();
        public string ServerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
