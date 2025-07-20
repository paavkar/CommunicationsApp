using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.Services.ResultModels;

namespace CommunicationsApp.Infrastructure.Services
{
    public class ChannelClassResult : ResultBaseModel
    {
        public ChannelClass? ChannelClass { get; set; }
    }
}
