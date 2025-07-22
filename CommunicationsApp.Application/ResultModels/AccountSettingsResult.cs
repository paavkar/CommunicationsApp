using CommunicationsApp.Core.Models;

namespace CommunicationsApp.Application.ResultModels
{
    public class AccountSettingsResult : ResultBaseModel
    {
        public AccountSettings? Settings { get; set; }
    }
}
