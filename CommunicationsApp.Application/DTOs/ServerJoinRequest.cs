using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.DTOs
{
    public class ServerJoinRequest
    {
        public Server Server { get; set; }
        public ServerProfile Profile { get; set; }
    }
}
