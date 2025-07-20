using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.Services.ResultModels;

namespace CommunicationsApp.Infrastructure.Services
{
    public class ChannelResult : ResultBaseModel
    {
        public Channel? Channel { get; set; }
    }
}
