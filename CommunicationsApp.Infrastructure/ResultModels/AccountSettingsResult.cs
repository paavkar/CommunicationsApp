using CommunicationsApp.Core.Models;
using CommunicationsApp.Infrastructure.Services.ResultModels;

namespace CommunicationsApp.Infrastructure.Services
{
    public class AccountSettingsResult : ResultBaseModel
    {
        public AccountSettings? Settings { get; set; }
    }
}
