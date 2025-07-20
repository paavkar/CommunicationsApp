using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.Services.ResultModels;

namespace CommunicationsApp.Infrastructure.Services
{
    public class ServerResult : ResultBaseModel
    {
        public Server? Server { get; set; }
    }
}
